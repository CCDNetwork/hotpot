using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Ccd.Server.Data;
using Ccd.Server.Helpers;

namespace Ccd.Server.Settings;

public class Settings : UserChangeTracked
{
    public static readonly Settings DEFAULT_SETTINGS = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        DeploymentCountry = "Country",
        DeploymentName = "CCD Data Portal",
        AdminLevel1Name = "AdminLevel1",
        AdminLevel2Name = "AdminLevel2",
        AdminLevel3Name = "AdminLevel3",
        AdminLevel4Name = "AdminLevel4",
        MetabaseUrl = "https://default.metabase.url",
        FundingSources = new List<string> { "BHA", "Other" }
    };

    public Guid Id { get; set; } = IdProvider.NewId();

    public string DeploymentCountry { get; set; }
    public string DeploymentName { get; set; }
    public string AdminLevel1Name { get; set; }
    public string AdminLevel2Name { get; set; }
    public string AdminLevel3Name { get; set; }
    public string AdminLevel4Name { get; set; }
    public string MetabaseUrl { get; set; }
    [Column(TypeName = "jsonb")] public List<string> FundingSources { get; set; }
}