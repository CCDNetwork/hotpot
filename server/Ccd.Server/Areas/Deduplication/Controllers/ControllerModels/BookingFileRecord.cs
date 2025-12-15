using System;

namespace Ccd.Server.Deduplication;

public class BookingFileRecord
{
    public string HeadOfHouseHoldId { get; set; }
    public string SpouseId { get; set; }
    public string Modality { get; set; }
    public string Amount { get; set; }
    public string Currency { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public string Frequency { get; set; }
}