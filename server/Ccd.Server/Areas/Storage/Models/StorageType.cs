using System;

namespace Ccd.Server.Storage;

public class StorageType
{
    public static readonly StorageType Assets = new StorageType {Id = 1, Name = "Assets", Directory = "assets"};
    public static readonly StorageType AzureBlob = new StorageType {Id = 2, Name = "AzureBlob", Directory = "azure"};

    public int Id { get; set; }
    public string Name { get; set; }
    public string Directory { get; set; }

    public static StorageType GetById(int id)
    {
        if (Assets.Id == id) return Assets;
        if (AzureBlob.Id == id) return AzureBlob;

        throw new ArgumentException($"Unknown StorageType id: {id}", nameof(id));
    }
}
