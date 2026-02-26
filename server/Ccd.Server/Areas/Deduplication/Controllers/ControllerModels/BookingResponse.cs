
using System;
using System.Text.Json.Serialization;
using Ccd.Server.Helpers;

namespace Ccd.Server.Deduplication;

public class BookingResponse
{
    public Guid Id { get; set; }
    [QuickSearchable] public string HouseholdId { get; set; }
    [QuickSearchable] public string SpouseId { get; set; }
    public string Currency { get; set; }
    public decimal Amount { get; set; }
    public int Rounds { get; set; }
    public string Modality { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    [JsonIgnore] public Guid OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}