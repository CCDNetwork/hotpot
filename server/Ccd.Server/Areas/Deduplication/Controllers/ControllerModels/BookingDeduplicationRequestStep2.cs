using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Ccd.Server.Deduplication;

public class BookingDeduplicationRequestStep2
{
    [Required] public Guid FileId { get; set; }
}
