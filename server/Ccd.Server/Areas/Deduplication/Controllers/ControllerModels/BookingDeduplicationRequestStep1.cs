using System;
using System.ComponentModel.DataAnnotations;
using Ccd.Server.Storage;
using Microsoft.AspNetCore.Http;

namespace Ccd.Server.Deduplication;

public class BookingDeduplicationRequestStep1
{
    [Required] public IFormFile File { get; set; }
    public int StorageTypeId { get; set; } = StorageType.Assets.Id;
}
