using System;
using System.Collections.Generic;
using Ccd.Server.Helpers;

namespace Ccd.Server.Organizations;

public class OrganizationResponse
{
    public Guid Id { get; set; }

    [QuickSearchable]
    public string Name { get; set; }
    public bool IsMpcaActive { get; set; }
    public bool IsWashActive { get; set; }
    public bool IsShelterActive { get; set; }
    public bool IsFoodAssistanceActive { get; set; }
    public bool IsLivelihoodsActive { get; set; }
    public bool IsProtectionActive { get; set; }
    public bool IsIncentivePaymentsActive { get; set; }
    public List<Activity> Activities { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
