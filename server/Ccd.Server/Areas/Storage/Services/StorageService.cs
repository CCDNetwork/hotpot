using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ccd.Server.Storage;

public class StorageService : IStorageService
{
    private readonly CcdContext _context;
    private readonly IServiceProvider _serviceProvider;

    public StorageService(CcdContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    private IStorageEngine GetEngine(int storageTypeId) =>
        _serviceProvider.GetRequiredKeyedService<IStorageEngine>(storageTypeId);


    public async Task<File> SaveFile(StorageType storageType, IFormFile file, Guid ownerId, string name, bool isTemporary = false)
    {
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        return await SaveFile(storageType, ms, ownerId, name, isTemporary);
    }

    public async Task<FileResponse> SaveFileApi(StorageType storageType, IFormFile file, Guid ownerId, string name, bool isTemporary = false)
    {
        var storedFile = await SaveFile(storageType, file, ownerId, name, isTemporary);

        if (storedFile == null)
            throw new BadRequestException();

        return new FileResponse
        {
            Id = storedFile.Id,
            StorageTypeId = storedFile.StorageTypeId,
            Name = storedFile.Name,
            Url = StaticConfiguration.StorageUrl + "/" + storedFile.FileName
        };
    }

    public async Task<File> SaveFile(StorageType storageType, MemoryStream ms, Guid ownerId, string name, bool isTemporary = false)
    {
        var savedFile = await GetEngine(storageType.Id).SaveFileAsync(ownerId, storageType, ms, name);
        savedFile.IsTemporary = isTemporary;

        savedFile = _context.Files.Add(savedFile).Entity;
        await _context.SaveChangesAsync();

        return savedFile;
    }

    public async Task DeleteFile(File file)
    {
        if (file is null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        _context.Files.Remove(file);
        await _context.SaveChangesAsync();

        try
        {
            GetEngine(file.StorageTypeId).DeleteFile(file);
        }
        catch (FileNotFoundException)
        {
            // Disk file already gone — DB cleanup already succeeded, treat as success.
        }
    }

    public async Task<byte[]> GetFileBytes(File file)
    {
        return await GetEngine(file.StorageTypeId).GetFileAsync(file);
    }

    public Stream GetFileStream(File file)
    {
        return GetEngine(file.StorageTypeId).GetFileStream(file);
    }

    public async Task<File> GetFileByFileName(string fileName)
    {
        return await _context.Files.FirstOrDefaultAsync(e => e.FileName == fileName) ??
               throw new NotFoundException($"File {fileName} not found");
    }

    public async Task<File> GetFileById(Guid id)
    {
        return await _context.Files.FirstOrDefaultAsync(e => e.Id == id) ??
               throw new NotFoundException($"File with id {id} not found");
    }

    public async Task<FileShortResponse> GetFileApiById(Guid? id)
    {
        var file = await _context.Files.FirstOrDefaultAsync(e => e.Id == id);
        if (file == null) return null;

        return new FileShortResponse { Id = file.Id, Url = StaticConfiguration.StorageUrl + "/" + file.FileName, Name = file.Name };
    }

    public async Task<List<FileShortResponse>> GetFilesApiById(List<Guid> ids)
    {
        var list = new List<FileShortResponse>();

        if (ids == null) return list;

        foreach (var id in ids)
        {
            var file = await GetFileApiById(id);
            if (file != null) list.Add(file);
        }

        return list;
    }

    public string ResolveContentType(string fileName)
    {
        string extension = Path.GetExtension(fileName)?.ToLowerInvariant();

        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".png":
                return "image/png";
            case ".gif":
                return "image/gif";
            case ".bmp":
                return "image/bmp";
            case ".webp":
                return "image/webp";
            default:
                return "application/octet-stream";
        }
    }

    public async Task<FileResponse> UpdateAltApi(Guid id)
    {
        var file = await GetFileById(id);
        var savedFile = _context.Files.Update(file).Entity;
        await _context.SaveChangesAsync();

        return new FileResponse
        {
            Id = savedFile.Id,
            StorageTypeId = savedFile.StorageTypeId,
            Name = savedFile.Name,
            Url = StaticConfiguration.StorageUrl + "/" + savedFile.FileName
        };
    }

    public async Task<FileResponse> GetFileApiByIdLongResponse(Guid id)
    {
        var file = await _context.Files.FirstOrDefaultAsync(e => e.Id == id);
        if (file == null) return null;

        return new FileResponse
        {
            Id = file.Id,
            Url = StaticConfiguration.StorageUrl + "/" + file.FileName,
            Name = file.Name,
            StorageTypeId = file.StorageTypeId
        };
    }

    public async Task<List<FileResponse>> GetFilesApiByIdLongResponse(List<Guid> ids)
    {
        var list = new List<FileResponse>();

        if (ids == null) return list;

        foreach (var id in ids)
        {
            list.Add(await GetFileApiByIdLongResponse(id));
        }

        return list;
    }
}
