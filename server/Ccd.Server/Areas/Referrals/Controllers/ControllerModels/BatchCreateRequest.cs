using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Ccd.Server.Storage;
using Microsoft.AspNetCore.Http;

namespace Ccd.Server.Referrals;

public class BatchCreateRequest
{
    [Required] public Guid OrganizationReferredToId { get; set; }
    [Required] public string ServiceCategory { get; set; }
    [Required] public string BatchType { get; set; }

    public List<Guid> SubactivitiesIds { get; set; }

    [Required] public IFormFile File { get; set; }

    public int StorageTypeId { get; set; } = StorageType.Assets.Id;
}