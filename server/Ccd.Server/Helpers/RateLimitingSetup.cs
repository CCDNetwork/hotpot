using System;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ccd.Server.Helpers;

public static class RateLimitingSetup
{
    public const string AuthPolicyName = "auth-strict";
    public const string LoginInitPolicyName = "login-init";

    public static IServiceCollection AddCcdRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetClientIp(context),
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = StaticConfiguration.RateLimitGlobalPermitPerMinute,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                AutoReplenishment = true
                            }
                    )
            );

            options.AddPolicy(
                AuthPolicyName,
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetClientIp(context),
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = StaticConfiguration.RateLimitAuthPermitPerMinute,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                AutoReplenishment = true
                            }
                    )
            );

            options.AddPolicy(
                LoginInitPolicyName,
                context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        GetClientIp(context),
                        _ =>
                            new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = StaticConfiguration.RateLimitLoginInitPermitPerMinute,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 6,
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                AutoReplenishment = true
                            }
                    )
            );

            options.OnRejected = async (context, token) =>
            {
                var httpContext = context.HttpContext;
                var logger = httpContext
                    .RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RateLimiting");
                logger.LogWarning(
                    "Rate limit exceeded for {Ip} on {Path}",
                    GetClientIp(httpContext),
                    httpContext.Request.Path.ToString()
                );

                httpContext.Response.Headers.RetryAfter =
                    context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? ((int)retryAfter.TotalSeconds).ToString()
                        : "60";

                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsync(
                    "{\"errorMessage\":\"Too many requests. Please try again later.\"}",
                    token
                );
            };
        });

        return services;
    }

    private static string GetClientIp(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
