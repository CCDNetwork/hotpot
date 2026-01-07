public class UserPermission
{
    public const string Referral = "referral";
    public const string Deduplication = "deduplication";
    public const string Booking = "booking";

    public static bool IsValidPermission(string permission)
    {
        return permission is Referral or Deduplication or Booking;
    }
}