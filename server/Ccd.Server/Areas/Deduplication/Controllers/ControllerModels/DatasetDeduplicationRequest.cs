using System;
using System.ComponentModel.DataAnnotations;
using Ccd.Server.Storage;
using Microsoft.AspNetCore.Http;

namespace Ccd.Server.Deduplication;

public class DatasetDeduplicationRequest
{
    [Required] public IFormFile File { get; set; }
    [Required] public Guid TemplateId { get; set; }
    public int StorageTypeId { get; set; } = StorageType.Assets.Id;
}
