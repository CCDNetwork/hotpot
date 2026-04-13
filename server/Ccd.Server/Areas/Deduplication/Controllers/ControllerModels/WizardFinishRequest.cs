using System;
using System.ComponentModel.DataAnnotations;

namespace Ccd.Server.Deduplication;

public class WizardFinishRequest
{
    [Required] public Guid FileId { get; set; }
}
