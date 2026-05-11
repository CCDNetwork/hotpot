using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Ccd.Server.Helpers;

/// <summary>
/// This middleware is extracting project, user and api key data from the request so it can
/// be used later in the pipeline
/// </summary>
public class PermissionLevelMiddleware
{
    private readonly RequestDelegate _next;

    public PermissionLevelMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CcdContext ccdContext)
    {
        var isUserAuthenticated = false;
        var role = "";

        if (
            context.Request.Headers.ContainsKey("organization-id")
            && !string.IsNullOrEmpty(context.Request.Headers["organization-id"].ToString())
        )
        {
            try
            {
                context.Items["OrganizationId"] = Guid.Parse(
                    context.Request.Headers["organization-id"].ToString()
                );
            }
            catch
            {
                context.Response.StatusCode = 401;
                var writer = new StreamWriter(context.Response.Body);
                await writer.WriteAsync("Invalid organization id");
                await writer.FlushAsync();
                await context.Response.CompleteAsync();
                return;
            }
        }

        // try to find the user role JWT
        if (
            context.User != null
            && context.User.Identity.IsAuthenticated
            && context.User.Identity.AuthenticationType != "ApiKey"
        )
        {
            isUserAuthenticated = true;
            role = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "";
        }

        if (isUserAuthenticated)
        {
            var key = "UserId";

            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userIdGuid))
            {
                context.Items[key] = userIdGuid;
            }

            context.Items["OrganizationRoles"] = role;
        }

        // Call the next delegate/middleware in the pipeline
        await _next(context);
    }
}

public static class PermissionLevelMiddlewareExtensions
{
    public static IApplicationBuilder UsePermissionLevel(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PermissionLevelMiddleware>();
    }
}
