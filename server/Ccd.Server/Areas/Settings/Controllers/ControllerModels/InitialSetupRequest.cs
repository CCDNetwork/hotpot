using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ccd.Server.Settings;

public class InitialSetupRequest
{
    // Deployment Settings (required)
    [Required]
    public string DeploymentName { get; set; }

    [Required]
    public string DeploymentCountry { get; set; }

    [Required]
    public string AdminLevel1Name { get; set; }

    [Required]
    public string AdminLevel2Name { get; set; }

    [Required]
    public string AdminLevel3Name { get; set; }

    [Required]
    public string AdminLevel4Name { get; set; }

    public string MetabaseUrl { get; set; }

    public List<string> FundingSources { get; set; }

    // Organization (required)
    [Required, MinLength(3), MaxLength(100)]
    public string OrganizationName { get; set; }

    public bool IsMpcaActive { get; set; }
    public bool IsWashActive { get; set; }
    public bool IsShelterActive { get; set; }
    public bool IsFoodAssistanceActive { get; set; }
    public bool IsLivelihoodsActive { get; set; }
    public bool IsProtectionActive { get; set; }

    // User (required)
    [Required, MinLength(2), MaxLength(30)]
    public string FirstName { get; set; }

    [Required, MinLength(2), MaxLength(30)]
    public string LastName { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }

    [Required, MinLength(8), MaxLength(30)]
    public string Password { get; set; }

    public List<string> Permissions { get; set; }
}
