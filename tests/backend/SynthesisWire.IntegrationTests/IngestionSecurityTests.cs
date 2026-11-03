using System.Net;
using System.Net.Http.Headers;
using System.Text;
using SynthesisWire.Infrastructure.Ingestion;

namespace SynthesisWire.IntegrationTests;

public sealed class IngestionSecurityTests
{
    [Theory]
    [InlineData("http://example.org/")]
    [InlineData("https://127.0.0.1/")]
    [InlineData("https://[::1]/")]
    [InlineData("https://example.org:444/")]
    [InlineData("https://example.org.evil.invalid/")]
    [InlineData("https://example.org/?key=not-a-credential")]
    [InlineData("https://example.org/#fragment")]
    [InlineData("https://user@example.org/")]
    [InlineData("file:///etc/hosts")]
    public void Rejects_unsafe_or_unapproved_URLs(string url)
        => Assert.Throws<InvalidOperationException>(() => new SafeSourceFetcher(["example.org"]).ValidateUrl(url));

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("10.0.0.1")]
    [InlineData("100.64.0.1")]
    [InlineData("127.1.2.3")]
    [InlineData("169.254.169.254")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("192.0.2.1")]
    [InlineData("198.18.0.1")]
    [InlineData("198.51.100.1")]
    [InlineData("203.0.113.1")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("2001:db8::1")]
    [InlineData("2002:7f00:1::")]
    public void Rejects_nonpublic_and_transition_addresses(string address)
        => Assert.False(SafeSourceFetcher.IsPublicAddress(IPAddress.Parse(address)));

    [Fact]
    public async Task Revalidates_redirect_before_sending_any_request_to_the_new_destination()
    {
        var sends = 0;
        var fetcher = new SafeSourceFetcher(["example.org"])
        {
            TestSend = (_, _) =>
            {
                sends++;
                var response = new HttpResponseMessage(HttpStatusCode.Found);
                response.Headers.Location = new Uri("http://169.254.169.254/");
                return Task.FromResult(response);
            }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => fetcher.FetchAsync("https://example.org/", TestContext.Current.CancellationToken));
        Assert.Equal(1, sends);
    }

    [Fact]
    public async Task Bounds_redirects_body_size_content_type_encoding_and_total_time()
    {
        var redirects = new SafeSourceFetcher(["example.org"])
        {
            TestSend = (_, _) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.Found);
                response.Headers.Location = new Uri("/again", UriKind.Relative);
                return Task.FromResult(response);
            }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => redirects.FetchAsync("https://example.org/", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => SafeSourceFetcher.ReadBoundedAsync(new MemoryStream(new byte[SafeSourceFetcher.MaximumBytes + 1]), null, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => SafeSourceFetcher.ReadBoundedAsync(new MemoryStream([1]), SafeSourceFetcher.MaximumBytes + 1, TestContext.Current.CancellationToken));
        foreach (var media in new[] { "application/pdf", "application/octet-stream" })
        {
            var fetcher = ResponseFetcher(media, "Synthetic source");
            await Assert.ThrowsAsync<InvalidOperationException>(() => fetcher.FetchAsync("https://example.org/", TestContext.Current.CancellationToken));
        }
        var compressed = new SafeSourceFetcher(["example.org"])
        {
            TestSend = (_, _) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Synthetic source", Encoding.UTF8, "text/plain") };
                response.Content.Headers.ContentEncoding.Add("gzip");
                return Task.FromResult(response);
            }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => compressed.FetchAsync("https://example.org/", TestContext.Current.CancellationToken));
        var slow = new SafeSourceFetcher(["example.org"])
        {
            Deadline = TimeSpan.FromMilliseconds(30),
            TestSend = async (_, token) => { await Task.Delay(Timeout.Infinite, token); return new HttpResponseMessage(); }
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => slow.FetchAsync("https://example.org/", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Captures_untrusted_markup_as_bytes_without_executing_or_generating_facts()
    {
        const string html = "<script>throw 'untrusted';</script><p>Synthetic source only.</p>";
        var result = await ResponseFetcher("text/html", html).FetchAsync("https://example.org/", TestContext.Current.CancellationToken);
        Assert.Equal(html, result.Text);
        Assert.Equal(64, result.ContentHash.Length);
        Assert.Equal(result.ContentHash, new SourceCapture(result.RequestedUrl, result.FinalUrl, result.MediaType, result.Body, result.FetchedAt).ContentHash);
        Assert.False(result.IsSynthetic); // Real fetcher never fabricates a synthetic/public research record.
        Assert.Throws<InvalidOperationException>(() => new SafeSourceFetcher([]).ValidateUrl("https://example.org/"));
    }

    private static SafeSourceFetcher ResponseFetcher(string media, string text) => new(["example.org"])
    {
        TestSend = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, media) })
    };

    [Fact]
    public async Task Optional_real_public_capture_smoke_never_publishes_content()
    {
        Assert.SkipWhen(Environment.GetEnvironmentVariable("SYNTHESISWIRE_PUBLIC_CAPTURE_SMOKE") != "true", "Explicit optional public-network smoke; disabled in deterministic CI.");
        var capture = await new SafeSourceFetcher(["example.org"]).FetchAsync("https://example.org/", TestContext.Current.CancellationToken);
        Assert.InRange(capture.Body.Length, 1, SafeSourceFetcher.MaximumBytes);
        Assert.Equal("https://example.org/", capture.FinalUrl);
        Assert.Equal(64, capture.ContentHash.Length);
        TestContext.Current.TestOutputHelper?.WriteLine($"Captured public example.org only: {capture.Body.Length} bytes, SHA-256 {capture.ContentHash}; no database or publication used.");
    }
}
