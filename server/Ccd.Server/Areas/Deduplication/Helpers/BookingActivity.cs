namespace Ccd.Server.Deduplication;

public class BookingActivity
{
    public const string Previous = "previous";
    public const string Current = "current";
    public const string Upcoming = "upcoming";
    public const string Released = "released";


    public static bool IsValidActivity(string modelActivity)
    {
        return modelActivity is Previous or Current or Upcoming or Released;
    }
}