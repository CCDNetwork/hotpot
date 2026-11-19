using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ccd.Server.Organizations;

public class OrganizationAddRequest
{
    [Required, MinLength(3), MaxLength(100)] public string Name { get; set; }
    [Required] public bool IsMpcaActive { get; set; }
    [Required] public bool IsWashActive { get; set; }
    [Required] public bool IsShelterActive { get; set; }
    [Required] public bool IsFoodAssistanceActive { get; set; }
    [Required] public bool IsLivelihoodsActive { get; set; }
    [Required] public bool IsProtectionActive { get; set; }
    [Required] public bool IsIncentivePaymentsActive { get; set; }
    public List<ActivityAddRequest> Activities { get; set; }

    /// <summary>Optional allowed email domains, created together with the organisation.</summary>
    public List<string> EmailDomains { get; set; }
}