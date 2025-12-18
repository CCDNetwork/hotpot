
using System;

namespace Ccd.Server.Deduplication;

public class BookingResponse
{
    public Guid Id { get; set; }
    public string HouseholdId { get; set; }
    public string SpouseId { get; set; }
    public string Currency { get; set; }
    public decimal Amount { get; set; }
    public int Frequency { get; set; }
    public string Modality { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}