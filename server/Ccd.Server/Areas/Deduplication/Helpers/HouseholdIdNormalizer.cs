namespace Ccd.Server.Deduplication;

public static class HouseholdIdNormalizer
{
    /// <summary>
    /// Canonicalise a household ID for matching. Any change here must apply
    /// to BOTH IdEncryptor and ConflictEventHasher — they must agree on the
    /// same normalised form or the reporting HMAC will not align with the
    /// booking comparison.
    /// </summary>
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return value.Trim();
    }
}
