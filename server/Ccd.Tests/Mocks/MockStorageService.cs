using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Ccd.Server.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using File = Ccd.Server.Storage.File;

namespace Ccd.Tests.Mocks;

public class MockStorageService : IStorageService
{
    private readonly CcdContext _context;
    private static Dictionary<Guid, byte[]> FileBytes = new();

    public MockStorageService(CcdContext context)
    {
        _context = context;
    }

    public async Task<File> SaveFile(StorageType storageType, IFormFile file, Guid ownerId, string name, bool isTemporary = false)
    {
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        return await SaveFile(storageType, ms, ownerId, name, isTemporary);
    }

    public async Task<File> SaveFile(StorageType storageType, MemoryStream ms, Guid ownerId, string name, bool isTemporary = false)
    {
        var newFile = new File
        {
            OwnerId = ownerId,
            StorageTypeId = storageType.Id,
            Name = name,
            FileName = Guid.NewGuid().ToString() + '-' + name.Replace(" ", "_"),
            Size = 1000,
            IsTemporary = isTemporary
        };

        newFile = _context.Files.Add(newFile).Entity;
        await _context.SaveChangesAsync();

        FileBytes.Add(newFile.Id, ms.ToArray());

        return newFile;
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

    public Task DeleteFile(File file)
    {
        FileBytes.Remove(file.Id);
        _context.Files.Remove(file);
        return _context.SaveChangesAsync();
    }

    public Task<byte[]> GetFileBytes(File file)
    {
        if (FileBytes.ContainsKey(file.Id))
            return Task.FromResult(FileBytes[file.Id]);

        return Task.FromResult(Array.Empty<byte>());
    }

    public Stream GetFileStream(File file)
    {
        if (FileBytes.ContainsKey(file.Id))
            return new MemoryStream(FileBytes[file.Id]);

        return new MemoryStream();
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

        return new FileShortResponse { Id = file.Id, Url = StaticConfiguration.StorageUrl + "/" + file.FileName };
    }

    public async Task<List<FileShortResponse>> GetFilesApiById(List<Guid> ids)
    {
        var list = new List<FileShortResponse>();

        if (ids == null) return list;

        foreach (var id in ids)
        {
            list.Add(await GetFileApiById(id));
        }

        return list;
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
            Url = StaticConfiguration.StorageUrl + "/" + savedFile.FileName,
        };
    }
}
