using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Ccd.Server.Helpers;
using IO = System.IO;

namespace Ccd.Server.Storage;

public class AzureBlobStorageEngine : IStorageEngine
{
    private readonly Lazy<BlobContainerClient> _container = new(() =>
    {
        var connectionString = StaticConfiguration.AzureStorageConnectionString;
        var containerName = StaticConfiguration.AzureBlobContainerName;

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(containerName))
            throw new InvalidOperationException("Azure Blob storage is not configured");

        return new BlobContainerClient(connectionString, containerName);
    });

    public async Task<byte[]> GetFileAsync(File file)
    {
        var blob = _container.Value.GetBlobClient(file.FileName);

        if (!await blob.ExistsAsync())
            throw new NotFoundException("File not found");

        var response = await blob.DownloadContentAsync();
        return response.Value.Content.ToArray();
    }

    public IO.Stream GetFileStream(File file)
    {
        var blob = _container.Value.GetBlobClient(file.FileName);

        if (!blob.Exists())
            throw new NotFoundException("File not found");

        return blob.OpenRead();
    }

    public async Task<File> SaveFileAsync(Guid ownerId, StorageType storageType, IO.MemoryStream stream, string name)
    {
        var internalFileName = Guid.NewGuid().ToString() + '-' + name.Replace(" ", "_");

        stream.Position = 0;
        var blob = _container.Value.GetBlobClient(internalFileName);
        await blob.UploadAsync(stream, overwrite: false);

        return new File
        {
            OwnerId = ownerId,
            StorageTypeId = storageType.Id,
            Name = name,
            FileName = internalFileName,
            Size = stream.Length
        };
    }

    public void DeleteFile(File file)
    {
        var blob = _container.Value.GetBlobClient(file.FileName);
        blob.DeleteIfExists();
    }
}
