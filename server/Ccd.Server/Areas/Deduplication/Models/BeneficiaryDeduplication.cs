using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Ccd.Server.Beneficiaries;
using Ccd.Server.Deduplication;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;
using Ccd.Server.Storage;
using Ccd.Server.Users;

namespace Ccd.Server.Deduplication;

public class BeneficaryDeduplication
{
    public Guid Id { get; set; } = IdProvider.NewId();
    public string FirstName { get; set; }
    public string FamilyName { get; set; }
    public string Gender { get; set; }
    public string DateOfBirth { get; set; }
    public string CommunityId { get; set; }
    public string HhId { get; set; }
    public string MobilePhoneId { get; set; }
    public string AdminLevel1 { get; set; }
    public string AdminLevel2 { get; set; }
    public string AdminLevel3 { get; set; }
    public string AdminLevel4 { get; set; }
    public string GovIdType { get; set; }
    public string GovIdNumber { get; set; }
    public string OtherIdType { get; set; }
    public string OtherIdNumber { get; set; }
    public string AssistanceDetails { get; set; }
    public string Activity { get; set; }
    public string Currency { get; set; }
    public string CurrencyAmount { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public string Frequency { get; set; }
    public bool IsOrganizationDuplicate { get; set; }
    public bool IsSystemDuplicate { get; set; }
    [Column(TypeName = "jsonb")] public List<string> MatchedFields { get; set; }
    [Column(TypeName = "jsonb")] public List<Guid> DuplicateOfIds { get; set; }
    [ForeignKey("User")] public Guid UploadedById { get; set; }
    public User UploadedBy { get; set; }
    public bool MarkedForImport { get; set; }
    public Guid? FileId { get; set; }
    public File File { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}