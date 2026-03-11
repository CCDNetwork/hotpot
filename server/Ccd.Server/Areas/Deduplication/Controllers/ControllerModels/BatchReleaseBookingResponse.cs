namespace Ccd.Server.Deduplication;

public class BatchReleaseBookingResponse
{
    public int Total { get; set; }
    public int Released { get; set; }
    public int Skipped { get; set; }
}
