using System;
using System.Linq;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;
using IO = System.IO;

namespace Ccd.Server.Storage;

[ApiController]
public class StorageController : ControllerBaseExtended
{
    private const int CACHE_AGE_SECONDS = 60 * 60 * 24 * 30; // 30 days

    private readonly CcdContext _context;
    private readonly IStorageService _storageService;

    public StorageController(CcdContext context, IStorageService storageService)
    {
        _storageService = storageService;
        _context = context;
    }

    [HttpGet("storage/{fileName}")]
    // [PermissionLevel(UserRole.User)]
    public async Task<IActionResult> Get(string fileName, [FromQuery] string size)
    {
        // check if fileName is actually the file ID
        var isValidParsedId = Guid.TryParse(fileName, out var parsedId);

        var file = isValidParsedId
            ? await _storageService.GetFileById(parsedId)
            : await _storageService.GetFileByFileName(fileName);

        var bytes = await _storageService.GetFileBytes(file);

        if (!string.IsNullOrEmpty(size))
        {
            var width = 0;
            var height = 0;

            try
            {
                width = Convert.ToInt32(size.Split("x")[0]);
                height = Convert.ToInt32(size.Split("x")[1]);
            }
            catch
            {
                throw new BadRequestException("size parameter needs to be in [width]x[height] format");
            }

            try
            {
                using var image = SixLabors.ImageSharp.Image.Load(bytes);

                if (image.Width > width || image.Height > height)
                {
                    image.Mutate(x => x.Resize(width, 0));

                    if (image.Height > height)
                    {
                        image.Mutate(x => x.Resize(0, height));
                    }
                }

                await using var ms = new IO.MemoryStream();
                await image.SaveAsync(ms, image.DetectEncoder(file.FileName));

                bytes = ms.ToArray();
            }
            catch
            {
                throw new BadRequestException("error resizing image");
            }
        }

        Response.Headers["Cache-Control"] = $"public,max-age={CACHE_AGE_SECONDS}";

        var contentType = _storageService.ResolveContentType(fileName);

        return new FileContentResult(bytes, contentType);
    }

    [HttpPost("/api/v1/storage/files")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<FileResponse>> Post([FromForm] FileRequest model)
    {
        var storedFile = await _storageService.SaveFileApi(StorageType.GetById(model.StorageTypeId), model.File,
            this.UserId, model.File.FileName);

        return Ok(storedFile);
    }
}
