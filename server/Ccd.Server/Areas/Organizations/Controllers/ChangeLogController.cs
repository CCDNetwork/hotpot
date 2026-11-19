using System;
using System.Linq;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ccd.Server.Organizations;

/// <summary>
/// Platform-wide admin change log: organisations, allowed email domains, users
/// and roles. Readable by Admins, User Admins and Organisation Admins
/// (PermissionLevel Admin covers all three).
/// </summary>
[ApiController]
[Route("/api/v1/change-log")]
public class ChangeLogController : ControllerBaseExtended
{
    private readonly CcdContext _context;

    public ChangeLogController(CcdContext context)
    {
        _context = context;
    }

    [HttpGet]
    [PermissionLevel(UserRole.Admin)]
    public async Task<ActionResult<PagedApiResponse<ChangeLogEntryResponse>>> GetAll(
        [FromQuery] string entityType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.ChangeLogEntries.AsNoTracking()
            .Where(e => string.IsNullOrEmpty(entityType) || e.EntityType == entityType);

        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ChangeLogEntryResponse
            {
                Id = e.Id,
                EntityType = e.EntityType,
                OrganizationName = e.OrganizationName,
                SubjectEmail = e.SubjectEmail,
                Action = e.Action,
                OldValue = e.OldValue,
                NewValue = e.NewValue,
                PerformedByEmail = e.PerformedByEmail,
                CreatedAt = e.CreatedAt,
            })
            .ToListAsync();

        return Ok(new PagedApiResponse<ChangeLogEntryResponse>
        {
            Data = data,
            Meta = new PagedApiResponseMeta(page, pageSize, total, "createdAt", "desc"),
        });
    }
}
