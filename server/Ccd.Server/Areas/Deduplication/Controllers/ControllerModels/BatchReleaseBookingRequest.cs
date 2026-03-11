using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Ccd.Server.Deduplication;

public class BatchReleaseBookingRequest
{
    [Required] public IFormFile File { get; set; }
}
