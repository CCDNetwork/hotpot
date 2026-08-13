using System;
using System.Collections.Generic;

namespace Ccd.Server.Settings;

public class SettingsResponse
{
    public Guid Id { get; set; }

    public string DeploymentCountry { get; set; }
    public string DeploymentName { get; set; }
    public string AdminLevel1Name { get; set; }
    public string AdminLevel2Name { get; set; }
    public string AdminLevel3Name { get; set; }
    public string AdminLevel4Name { get; set; }
    public string MetabaseUrl { get; set; }
    public List<string> FundingSources { get; set; }
}