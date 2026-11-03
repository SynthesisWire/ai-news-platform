using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SynthesisWire.Infrastructure.Ingestion;

public sealed record SourceCapture(string RequestedUrl, string FinalUrl, string MediaType, byte[] Body, DateTimeOffset FetchedAt, bool IsSynthetic = false)
{
    public string ContentHash => Convert.ToHexStringLower(SHA256.HashData(Body));
    public string Text => new UTF8Encoding(false, true).GetString(Body);
    public string MaterialHash => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join(" ", Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))));
}

public interface ISourceFetcher
{
    Task<SourceCapture> FetchAsync(string url, CancellationToken cancellationToken = default);
}

// No ambient proxy, cookies, credentials, automatic redirects or decompression. DNS is checked
// inside the actual socket callback and the socket connects to that checked address, not a second lookup.
public sealed class SafeSourceFetcher(IEnumerable<string> allowedHosts) : ISourceFetcher
{
    public const int MaximumBytes = 524288;
    internal Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? TestSend { get; init; }
    internal TimeSpan Deadline { get; init; } = TimeSpan.FromSeconds(20);
    private readonly HashSet<string> _allowedHosts = new(allowedHosts.Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0), StringComparer.Ordinal);

    public Uri ValidateUrl(string value)
    {
        if (value.Length > 2048 || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || uri.Port != 443 || uri.UserInfo.Length > 0
            || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.HostNameType != UriHostNameType.Dns
            || !_allowedHosts.Contains(uri.IdnHost.ToLowerInvariant()))
            throw new InvalidOperationException("Only query-free HTTPS URLs on explicitly allowed DNS hosts and port 443 are permitted.");
        return uri;
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) return IsPublicAddress(address.MapToIPv4());
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return (bytes[0] & 0xe0) == 0x20 && !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] < 2)
                && !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8)
                && !(bytes[0] == 0x20 && bytes[1] == 0x02);
        return bytes[0] is not (0 or 10 or 127) && bytes[0] < 224
            && !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127)
            && !(bytes[0] == 169 && bytes[1] == 254)
            && !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            && !(bytes[0] == 192 && (bytes[1] == 168 || bytes[1] == 0 || (bytes[1] == 88 && bytes[2] == 99)))
            && !(bytes[0] == 198 && (bytes[1] is 18 or 19 || (bytes[1] == 51 && bytes[2] == 100)))
            && !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113);
    }

    public async Task<SourceCapture> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        var requested = ValidateUrl(url);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(Deadline);
        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None,
            MaxResponseHeadersLength = 16,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            MaxConnectionsPerServer = 1,
            ConnectCallback = async (context, token) =>
            {
                if (context.DnsEndPoint.Port != 443 || !_allowedHosts.Contains(context.DnsEndPoint.Host.ToLowerInvariant()))
                    throw new InvalidOperationException("Connection destination is not allowed.");
                var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, token);
                if (addresses.Length == 0 || addresses.Any(address => !IsPublicAddress(address)))
                    throw new InvalidOperationException("DNS contains a non-public address.");
                // Deliberately IPv4-only transport for this local increment; IPv6-only sources fail closed.
                var address = addresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)
                    ?? throw new InvalidOperationException("A public IPv4 destination is required.");
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(address, 443), token);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch { socket.Dispose(); throw; }
            }
        };
        using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        var current = requested;
        for (var redirects = 0; ; redirects++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, current) { Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionExact };
            request.Headers.UserAgent.ParseAdd("SynthesisWire-LocalReview/0.1");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            using var response = TestSend is null
                ? await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token)
                : await TestSend(request, deadline.Token);
            if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                if (redirects >= 3 || response.Headers.Location is null) throw new InvalidOperationException("Redirect limit or invalid redirect.");
                current = ValidateUrl(new Uri(current, response.Headers.Location).AbsoluteUri);
                continue;
            }
            response.EnsureSuccessStatusCode();
            var type = response.Content.Headers.ContentType;
            if (type?.MediaType is not ("text/plain" or "text/html")
                || (type.CharSet is not null && !type.CharSet.Trim('"').Equals("utf-8", StringComparison.OrdinalIgnoreCase))
                || response.Content.Headers.ContentEncoding.Count > 0)
                throw new InvalidOperationException("Only uncompressed UTF-8 text/plain or text/html sources are supported.");
            var body = await ReadBoundedAsync(await response.Content.ReadAsStreamAsync(deadline.Token), response.Content.Headers.ContentLength, deadline.Token);
            var capture = new SourceCapture(requested.AbsoluteUri, current.AbsoluteUri, type.MediaType!, body, DateTimeOffset.UtcNow);
            _ = capture.Text; // Reject malformed UTF-8 before persistence; never parse/execute downloaded markup.
            return capture;
        }
    }

    public static async Task<byte[]> ReadBoundedAsync(Stream input, long? length, CancellationToken cancellationToken)
    {
        await using (input)
        {
            if (length > MaximumBytes) throw new InvalidOperationException("Source exceeds capture size limit.");
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (output.Length + count > MaximumBytes) throw new InvalidOperationException("Source exceeds capture size limit.");
                output.Write(buffer, 0, count);
            }
            if (output.Length == 0) throw new InvalidOperationException("Source is empty.");
            return output.ToArray();
        }
    }
}
