using System;
using Ccd.Server.Referrals;

namespace Ccd.Server.Deduplication;

public class BookingExportResponse
{
    [ImpexName("householdId;en")] public string HouseholdId { get; set; }
    [ImpexName("spouseId;en")] public string SpouseId { get; set; }
    [ImpexName("currency;en")] public string Currency { get; set; }
    [ImpexName("amount;en")] public decimal Amount { get; set; }
    [ImpexName("rounds;en")] public int Rounds { get; set; }
    [ImpexName("modality;en")] public string Modality { get; set; }
    [ImpexName("startDate;en")] public DateTime? StartDate { get; set; }
    [ImpexName("endDate;en")] public DateTime? EndDate { get; set; }
}
