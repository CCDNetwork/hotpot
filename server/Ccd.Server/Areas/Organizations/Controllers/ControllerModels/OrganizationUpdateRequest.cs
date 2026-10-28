using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ccd.Server.Organizations;

public class OrganizationUpdateRequest
{
    [Required, MinLength(3), MaxLength(100)] public string Name { get; set; }
    [Required] public bool IsMpcaActive { get; set; }
    [Required] public bool IsWashActive { get; set; }
    [Required] public bool IsShelterActive { get; set; }
    [Required] public bool IsFoodAssistanceActive { get; set; }
    [Required] public bool IsLivelihoodsActive { get; set; }
    [Required] public bool IsProtectionActive { get; set; }
    [Required] public bool IsIncentivePaymentsActive { get; set; }
    public List<ActivityUpdateRequest> Activities { get; set; }
}
