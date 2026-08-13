using System;
using System.Linq;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ccd.Server.Deduplication;

public class ConflictEventService
{
    private readonly CcdContext _context;

    public ConflictEventService(CcdContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Insert a booking_conflict_event for the fingerprint
    /// (requesting_org, blocking_org, subject_key, overlap_period), or bump
    /// detection_count and the "latest proposed" values when the fingerprint
    /// already exists. SaveChanges is left to the caller — the write is part of
    /// the wizard's larger transaction.
    /// </summary>
    public async Task RecordOrIncrementAsync(Guid requestingOrganizationId, ConflictDetectionContext ctx)
    {
        var subjectKey = ConflictEventHasher.ComputeSubjectKey(ctx.MatchedIdPlaintext);
        var overlapStart = DateOnly.FromDateTime(ctx.ProposedStart);
        var overlapEnd = DateOnly.FromDateTime(ctx.ProposedEnd);

        // Check pending inserts first so two rows in the same upload with the same
        // fingerprint increment instead of colliding on the unique index at commit.
        var existing = _context.BookingConflictEvents.Local.FirstOrDefault(e =>
            e.RequestingOrganizationId == requestingOrganizationId
            && e.BlockingOrganizationId == ctx.BlockingOrganizationId
            && e.SubjectKey == subjectKey
            && e.OverlapStartDate == overlapStart
            && e.OverlapEndDate == overlapEnd)
            ?? await _context.BookingConflictEvents.FirstOrDefaultAsync(e =>
                e.RequestingOrganizationId == requestingOrganizationId
                && e.BlockingOrganizationId == ctx.BlockingOrganizationId
                && e.SubjectKey == subjectKey
                && e.OverlapStartDate == overlapStart
                && e.OverlapEndDate == overlapEnd);

        if (existing != null)
        {
            // Re-detection: increment counter, keep the latest proposed values
            // (partner's most recent intent — DigiCap Q2b).
            existing.LastDetectedAt = DateTime.UtcNow;
            existing.DetectionCount += 1;
            existing.ProposedAmount = ctx.ProposedAmount;
            existing.ProposedCurrency = ctx.ProposedCurrency;
            existing.ProposedModality = ctx.ProposedModality;
            existing.ProposedRounds = ctx.ProposedRounds;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.BookingConflictEvents.Add(new BookingConflictEvent
            {
                Id = IdProvider.NewId(),
                RequestingOrganizationId = requestingOrganizationId,
                BlockingOrganizationId = ctx.BlockingOrganizationId,
                SubjectKey = subjectKey,
                OverlapStartDate = overlapStart,
                OverlapEndDate = overlapEnd,
                BlockingBookingId = ctx.BlockingBookingId,
                ProposedAmount = ctx.ProposedAmount,
                ProposedCurrency = ctx.ProposedCurrency,
                ProposedModality = ctx.ProposedModality,
                ProposedRounds = ctx.ProposedRounds,
                FirstDetectedAt = DateTime.UtcNow,
                LastDetectedAt = DateTime.UtcNow,
                DetectionCount = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
}
