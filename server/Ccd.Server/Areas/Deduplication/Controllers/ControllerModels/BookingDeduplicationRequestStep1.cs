using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Ccd.Server.Deduplication;

public class BookingDeduplicationRequestStep1
{
    [Required] public IFormFile File { get; set; }
}
