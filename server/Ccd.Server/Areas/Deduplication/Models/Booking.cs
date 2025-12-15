using System;
using System.ComponentModel.DataAnnotations.Schema;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;
using Ccd.Server.Storage;
using Ccd.Server.Users;

namespace Ccd.Server.Deduplication;

public class Booking
{
    public Guid Id { get; set; } = IdProvider.NewId();
    public string HouseholdId { get; set; }
    public string SpouseId { get; set; }
    public string Currency { get; set; }
    public decimal Amount { get; set; }
    public int Frequency { get; set; }
    public string Modality { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    [ForeignKey("User")] public Guid UploadedById { get; set; }
    public User UploadedBy { get; set; }
    public Guid FileId { get; set; }
    public File File { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}