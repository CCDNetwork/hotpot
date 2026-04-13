using System;
using System.Linq;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ccd.Server.Helpers;

/// <summary>
/// Hangfire recurring job: sweeps temporary files older than the configured lifetime
/// and deletes them (DB + disk). Registered via RecurringJob.AddOrUpdate in Startup.
/// </summary>
public class TemporaryFileCleanupJob
{
    public const string JobId = "temporary-file-cleanup";

    private readonly CcdContext _context;
    private readonly IStorageService _storage;
    private readonly ILogger<TemporaryFileCleanupJob> _logger;

    public TemporaryFileCleanupJob(
        CcdContext context,
        IStorageService storage
    )
    {
        _context = context;
        _storage = storage;
    }

    private static readonly TimeSpan TemporaryFileLifetime = TimeSpan.FromMinutes(60);

    public async Task Run()
    {
        Console.WriteLine("Running TemporaryFileCleanupJob at " + DateTime.UtcNow);
        var cutoff = DateTime.UtcNow - TemporaryFileLifetime;

        var staleFiles = await _context
            .Files.Where(f => f.IsTemporary && f.CreatedAt < cutoff)
            .ToListAsync();

        if (staleFiles.Count == 0)
        {
            Console.WriteLine("No stale files found for cleanup");
            return;
        }

        Console.WriteLine($"Found {staleFiles.Count} stale files to clean up");

        foreach (var file in staleFiles)
        {
            try
            {
                await RemoveFileReferences(file.Id);
                await _storage.DeleteFile(file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete stale file {Id}", file.Id);
            }
        }

        Console.WriteLine("TemporaryFileCleanupJob completed at " + DateTime.UtcNow);
    }

    /// <summary>
    /// Nulls out the file_id FK on any Booking / BookingLog / BeneficaryDeduplication
    /// rows that point at the given file. The rows themselves are preserved; only the
    /// reference is cleared so the file row can be removed without violating the FK.
    /// </summary>
    private async Task RemoveFileReferences(Guid fileId)
    {
        var bookings = await _context.Bookings.Where(b => b.FileId == fileId).ToListAsync();
        foreach (var b in bookings)
            b.FileId = null;

        var bookingLogs = await _context.BookingLogs.Where(bl => bl.FileId == fileId).ToListAsync();
        foreach (var bl in bookingLogs)
            bl.FileId = null;

        var dedupRows = await _context
            .BeneficaryDeduplications.Where(d => d.FileId == fileId)
            .ToListAsync();
        foreach (var d in dedupRows)
            d.FileId = null;

        if (bookings.Count + bookingLogs.Count + dedupRows.Count > 0)
            await _context.SaveChangesAsync();
    }
}
