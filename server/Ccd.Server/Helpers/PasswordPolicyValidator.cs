using System.Text.RegularExpressions;

namespace Ccd.Server.Helpers;

public static class PasswordPolicyValidator
{
    private const int MinimumLength = 8;
    private static readonly Regex HasUppercase = new("[A-Z]", RegexOptions.Compiled);
    private static readonly Regex HasLowercase = new("[a-z]", RegexOptions.Compiled);
    private static readonly Regex HasDigit = new("[0-9]", RegexOptions.Compiled);
    private static readonly Regex HasSpecial = new("[^A-Za-z0-9]", RegexOptions.Compiled);

    public const string ErrorMessage =
        "Password must be at least 8 characters and contain an uppercase letter, a lowercase letter, a number, and a special character.";

    public static void Validate(string password)
    {
        if (
            string.IsNullOrEmpty(password)
            || password.Length < MinimumLength
            || !HasUppercase.IsMatch(password)
            || !HasLowercase.IsMatch(password)
            || !HasDigit.IsMatch(password)
            || !HasSpecial.IsMatch(password)
        )
        {
            throw new BadRequestException(ErrorMessage);
        }
    }
}
