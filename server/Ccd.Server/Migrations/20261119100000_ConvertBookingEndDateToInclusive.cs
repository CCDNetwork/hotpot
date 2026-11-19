using Ccd.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ccd.Server.Migrations
{
    /// <summary>
    /// CO-02 Request 2 — End Date becomes INCLUSIVE (last covered day) in storage.
    ///
    /// Shifts every stored end date by -1 day (Down: +1 day) in:
    ///   booking.end_date, booking_log.end_date, booking_conflict_event.overlap_end_date.
    /// Released bookings (end_date NULL) are untouched.
    ///
    /// Safety:
    ///  - EF runs the whole migration in ONE transaction; any RAISE EXCEPTION
    ///    below rolls everything back and the app does not start, so the data
    ///    is never left half-converted.
    ///  - Pre-check: no active booking may already have end_date &lt;= start_date
    ///    (that would mean the data is not in the exclusive convention).
    ///  - Post-check per table: same number of dated rows, every row moved by
    ///    exactly one day (sum of dates changed by exactly the row count), and
    ///    no booking ends before it starts.
    ///  - The conflict-event fingerprint unique index is dropped and recreated
    ///    around the update so the row-by-row shift can't hit a transient
    ///    duplicate.
    ///
    /// The app must be fully stopped while this runs (Recreate deployment, no
    /// old instance serving uploads), and a DB backup taken beforehand.
    /// </summary>
    [DbContext(typeof(CcdContext))]
    [Migration("20261119100000_ConvertBookingEndDateToInclusive")]
    public partial class ConvertBookingEndDateToInclusive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ShiftSql(-1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ShiftSql(+1));
        }

        private static string ShiftSql(int days)
        {
            // Up (-1): data must currently be exclusive → end > start.
            // Down (+1): data must currently be inclusive → end >= start.
            var preCheck = days < 0 ? "end_date <= start_date" : "end_date < start_date";
            var direction = days < 0 ? "exclusive -> inclusive" : "inclusive -> exclusive";

            return $@"
SET LOCAL timezone = 'UTC';

DO $migration$
DECLARE
    v_days            int := {days};
    v_bad             bigint;
    v_updated         bigint;

    b_count_before    bigint;  b_sum_before  numeric;
    b_count_after     bigint;  b_sum_after   numeric;
    l_count_before    bigint;  l_sum_before  numeric;
    l_count_after     bigint;  l_sum_after   numeric;
    e_count_before    bigint;  e_sum_before  numeric;
    e_count_after     bigint;  e_sum_after   numeric;
BEGIN
    RAISE NOTICE 'ConvertBookingEndDateToInclusive: {direction} (% day)', v_days;

    -- ---------------------------------------------------------- pre-check
    SELECT count(*) INTO v_bad
    FROM booking
    WHERE end_date IS NOT NULL AND (start_date IS NULL OR {preCheck});
    IF v_bad > 0 THEN
        RAISE EXCEPTION 'Aborted: % booking row(s) have an end_date incompatible with the {direction} shift (or a NULL start_date). No data was changed.', v_bad;
    END IF;

    -- ---------------------------------------------------------- baseline
    SELECT count(end_date), coalesce(sum(extract(epoch FROM end_date)), 0)
      INTO b_count_before, b_sum_before FROM booking;
    SELECT count(end_date), coalesce(sum(extract(epoch FROM end_date)), 0)
      INTO l_count_before, l_sum_before FROM booking_log;
    SELECT count(overlap_end_date), coalesce(sum(overlap_end_date - DATE '2000-01-01'), 0)
      INTO e_count_before, e_sum_before FROM booking_conflict_event;

    RAISE NOTICE 'Before: booking=% booking_log=% booking_conflict_event=% dated rows',
        b_count_before, l_count_before, e_count_before;

    -- ---------------------------------------------------------- shift
    UPDATE booking SET end_date = end_date + make_interval(days => v_days)
     WHERE end_date IS NOT NULL;
    GET DIAGNOSTICS v_updated = ROW_COUNT;
    IF v_updated <> b_count_before THEN
        RAISE EXCEPTION 'Aborted: booking updated % rows, expected %.', v_updated, b_count_before;
    END IF;

    UPDATE booking_log SET end_date = end_date + make_interval(days => v_days)
     WHERE end_date IS NOT NULL;
    GET DIAGNOSTICS v_updated = ROW_COUNT;
    IF v_updated <> l_count_before THEN
        RAISE EXCEPTION 'Aborted: booking_log updated % rows, expected %.', v_updated, l_count_before;
    END IF;

    DROP INDEX IF EXISTS idx_conflict_event_fingerprint;
    UPDATE booking_conflict_event SET overlap_end_date = overlap_end_date + v_days;
    GET DIAGNOSTICS v_updated = ROW_COUNT;
    IF v_updated <> e_count_before THEN
        RAISE EXCEPTION 'Aborted: booking_conflict_event updated % rows, expected %.', v_updated, e_count_before;
    END IF;
    CREATE UNIQUE INDEX idx_conflict_event_fingerprint ON booking_conflict_event
        (requesting_organization_id, blocking_organization_id, subject_key, overlap_start_date, overlap_end_date);

    -- ---------------------------------------------------------- verify
    SELECT count(end_date), coalesce(sum(extract(epoch FROM end_date)), 0)
      INTO b_count_after, b_sum_after FROM booking;
    SELECT count(end_date), coalesce(sum(extract(epoch FROM end_date)), 0)
      INTO l_count_after, l_sum_after FROM booking_log;
    SELECT count(overlap_end_date), coalesce(sum(overlap_end_date - DATE '2000-01-01'), 0)
      INTO e_count_after, e_sum_after FROM booking_conflict_event;

    IF b_count_after <> b_count_before
       OR b_sum_after - b_sum_before <> b_count_before * v_days * 86400 THEN
        RAISE EXCEPTION 'Aborted: booking verification failed (rows % -> %, epoch delta %).',
            b_count_before, b_count_after, b_sum_after - b_sum_before;
    END IF;

    IF l_count_after <> l_count_before
       OR l_sum_after - l_sum_before <> l_count_before * v_days * 86400 THEN
        RAISE EXCEPTION 'Aborted: booking_log verification failed (rows % -> %, epoch delta %).',
            l_count_before, l_count_after, l_sum_after - l_sum_before;
    END IF;

    IF e_count_after <> e_count_before
       OR e_sum_after - e_sum_before <> e_count_before * v_days THEN
        RAISE EXCEPTION 'Aborted: booking_conflict_event verification failed (rows % -> %, day delta %).',
            e_count_before, e_count_after, e_sum_after - e_sum_before;
    END IF;

    SELECT count(*) INTO v_bad
    FROM booking WHERE end_date IS NOT NULL AND end_date < start_date;
    IF v_bad > 0 THEN
        RAISE EXCEPTION 'Aborted: % booking row(s) would end before they start.', v_bad;
    END IF;

    RAISE NOTICE 'ConvertBookingEndDateToInclusive: OK — booking=%, booking_log=%, booking_conflict_event=% rows shifted by % day.',
        b_count_before, l_count_before, e_count_before, v_days;
END
$migration$;
";
        }
    }
}
