namespace AiResearch.Domain;

public enum EditorialStatus { Draft, InReview, Approved, Published }
public enum ResearchItemType { ResearchPaper, ModelRelease, ResearchNote }
public enum EvidenceStrength { Limited, Moderate, Strong, Synthetic }
public enum ClaimType { AuthorClaim, IndependentFinding, SyntheticIllustration }
public enum Confidence { Low, Medium, High }
public enum EvidenceKind { Paper, VendorReport, IndependentReplication, SyntheticFixture }

public sealed class EditorialRuleException(string message) : InvalidOperationException(message);

public static class SupportedLocales
{
    public const string English = "en";
    public const string BrazilianPortuguese = "pt-BR";

    public static bool TryNormalize(string? value, out string locale)
    {
        locale = value?.ToLowerInvariant() switch
        {
            null => English,
            "en" => English,
            "pt-br" => BrazilianPortuguese,
            _ => string.Empty
        };
        return locale.Length > 0;
    }

    public static string Normalize(string value) => TryNormalize(value, out var locale)
        ? locale
        : throw new EditorialRuleException("Supported locales are en and pt-BR.");
}

internal static class Guard
{
    public static string Text(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new EditorialRuleException($"{name} is required.");
        return value.Trim();
    }

    public static DateTimeOffset Utc(DateTimeOffset value, string name)
    {
        if (value == default) throw new EditorialRuleException($"{name} is required.");
        return value.ToUniversalTime();
    }
}
