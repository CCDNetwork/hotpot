using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Ccd.Server.Storage;

public interface IStorageService
{
    Task<File> SaveFile(StorageType storageType, IFormFile file, Guid ownerId, string name, bool isTemporary = false);
    Task<File> SaveFile(StorageType storageType, MemoryStream ms, Guid ownerId, string name, bool isTemporary = false);
    Task<FileResponse> SaveFileApi(StorageType storageType, IFormFile file, Guid ownerId, string name, bool isTemporary = false);
    Task DeleteFile(File file);
    Task<File> GetFileById(Guid id);
    Task<File> GetFileByFileName(string fileName);
    Task<byte[]> GetFileBytes(File file);
    Stream GetFileStream(File file);
    Task<FileShortResponse> GetFileApiById(Guid? id);
    Task<List<FileShortResponse>> GetFilesApiById(List<Guid> ids);
    Task<List<FileResponse>> GetFilesApiByIdLongResponse(List<Guid> ids);
    Task<FileResponse> GetFileApiByIdLongResponse(Guid id);
    string ResolveContentType(string fileName);
}
