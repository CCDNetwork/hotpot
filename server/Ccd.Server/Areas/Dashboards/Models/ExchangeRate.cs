using System;
using System.ComponentModel.DataAnnotations.Schema;
using Ccd.Server.Helpers;

namespace Ccd.Server.Dashboards;

public class ExchangeRate
{
    public Guid Id { get; set; } = IdProvider.NewId();

    public string Currency { get; set; }
    public string BaseCurrency { get; set; }

    // 1 unit of Currency = Rate units of BaseCurrency. InforEuro publishes the
    // inverse (units per EUR), so high-denomination currencies need many decimal
    // places after inversion — hence the wide precision.
    [Column(TypeName = "numeric(24,12)")] public decimal Rate { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }
    public string Source { get; set; }

    public DateTime FetchedAt { get; set; }
}
