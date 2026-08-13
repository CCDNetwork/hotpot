using System;
using System.Security.Cryptography;
using System.Text;
using Ccd.Server.Helpers;

namespace Ccd.Server.Deduplication;

public static class ConflictEventHasher
{
    /// <summary>
    /// HMAC-SHA-256 of the normalised plaintext household ID, keyed with the
    /// deployment-scoped CONFLICT_EVENT_HMAC_KEY. Independent of IdEncryptor so
    /// a future change to storage-side encryption (e.g. a random IV) does not
    /// break historical subject_key values.
    /// </summary>
    public static string ComputeSubjectKey(string householdId)
    {
        if (string.IsNullOrWhiteSpace(householdId))
            throw new ArgumentException("Household ID required", nameof(householdId));

        var normalized = HouseholdIdNormalizer.Normalize(householdId);
        var secret = StaticConfiguration.ConflictEventHmacKey
            ?? throw new InvalidOperationException("CONFLICT_EVENT_HMAC_KEY is not configured.");

        var keyBytes = Convert.FromBase64String(secret);
        var dataBytes = Encoding.UTF8.GetBytes(normalized);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash); // 64-char uppercase hex; column is text
    }
}
