using System;
using System.Collections.Generic;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;

namespace Ccd.Server.Users;

public class UserResponse
{
    public Guid Id { get; set; }

    [QuickSearchable] public string Email { get; set; }
    [QuickSearchable] public string FirstName { get; set; }
    [QuickSearchable] public string LastName { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Role { get; set; }
    public string Language { get; set; } = "en";
    public List<OrganizationResponse> Organizations { get; set; }
    public List<string> Permissions { get; set; }

    public bool IsSuperAdmin => Id == User.SYSTEM_USER.Id;

}
