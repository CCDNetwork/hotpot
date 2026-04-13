using System;
using System.ComponentModel.DataAnnotations;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Ccd.Server.Users;

namespace Ccd.Server.Storage;

public class File : UserChangeTracked
{
    public Guid Id { get; set; } = IdProvider.NewId();

    [Required] public Guid OwnerId { get; set; }
    public User Owner { get; set; }

    public int StorageTypeId { get; set; }
    public string Name { get; set; }
    public string FileName { get; set; }
    public long Size { get; set; }
    public bool IsTemporary { get; set; }
}
