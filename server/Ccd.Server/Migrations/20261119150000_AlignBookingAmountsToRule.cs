using Ccd.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ccd.Server.Migrations
{
    /// <summary>
    /// Aligns existing MPCA amounts with the CWG-agreed rule (BookingAmountRule):
    /// amount = 1250 ILS × rounds, currency ILS. The amount is the agreed entitlement, so ALL
    /// bookings are corrected (current, upcoming, previous and released), plus
    /// the proposed amount of blocked duplicate attempts (booking_conflict_event)
    /// so the dashboard uses one scale. booking_log (upload history) is NOT changed.
    ///
    /// The rule is written out here (1250 × rounds) on purpose: this migration
    /// records the rule as it was when it ran, even if BookingAmountRule changes later.
    ///
    /// Safety:
    ///  - EF runs the whole migration in ONE transaction; any RAISE EXCEPTION
    ///    rolls everything back and the app does not start.
    ///  - Currency: ILS only (CWG). A row in any other currency (or none) is
    ///    converted: currency becomes ILS and the amount 1250 × rounds. Its original
    ///    currency and amount are kept in the backup table like every other change.
    ///  - Pre-check: every row must be MPCA with rounds 1–3. Otherwise nothing is
    ///    changed and the error names the problem, so the data can be reviewed.
    ///  - The original amount and currency of every changed row are copied to
    ///    booking_amount_backup / booking_conflict_event_amount_backup first.
    ///  - Post-check: the number of updated rows equals the number backed up,
    ///    and no row breaks the rule afterwards.
    ///  - Down restores the original values from the backup tables exactly.
    ///
    /// Run with the app fully stopped (no old instance accepting uploads) and a
    /// database backup taken beforehand.
    /// </summary>
    [DbContext(typeof(CcdContext))]
    [Migration("20261119150000_AlignBookingAmountsToRule")]
    public partial class AlignBookingAmountsToRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DownSql);
        }

        private const string UpSql = """
            DO $$
            DECLARE
                b_not_ils     bigint;
                b_not_mpca    bigint;
                b_bad_rounds  bigint;
                e_not_ils     bigint;
                e_not_mpca    bigint;
                e_bad_rounds  bigint;
                b_backed_up   bigint;
                b_updated     bigint;
                e_backed_up   bigint;
                e_updated     bigint;
                b_remaining   bigint;
                e_remaining   bigint;
            BEGIN
                -- ---------------------------------------------------------- pre-check
                SELECT
                    count(*) FILTER (WHERE upper(btrim(coalesce(currency, ''))) <> 'ILS'),
                    count(*) FILTER (WHERE upper(btrim(coalesce(modality, ''))) <> 'MPCA'),
                    count(*) FILTER (WHERE rounds NOT BETWEEN 1 AND 3)
                INTO b_not_ils, b_not_mpca, b_bad_rounds
                FROM booking;

                SELECT
                    count(*) FILTER (WHERE upper(btrim(coalesce(proposed_currency, ''))) <> 'ILS'),
                    count(*) FILTER (WHERE upper(btrim(coalesce(proposed_modality, ''))) <> 'MPCA'),
                    count(*) FILTER (WHERE proposed_rounds NOT BETWEEN 1 AND 3)
                INTO e_not_ils, e_not_mpca, e_bad_rounds
                FROM booking_conflict_event;

                -- Non-ILS rows are converted to ILS below (CWG: ILS only); only an
                -- unexpected modality or number of rounds stops the migration.
                IF b_not_mpca + b_bad_rounds + e_not_mpca + e_bad_rounds > 0 THEN
                    RAISE EXCEPTION
                        'Aborted: data does not fit the amount rule, nothing was changed. '
                        'booking: % not MPCA, % with rounds outside 1-3; '
                        'booking_conflict_event: % not MPCA, % with rounds outside 1-3. '
                        'Review these rows before running the migration again.',
                        b_not_mpca, b_bad_rounds, e_not_mpca, e_bad_rounds;
                END IF;

                -- ---------------------------------------------------------- backup
                CREATE TABLE booking_amount_backup (
                    id           uuid PRIMARY KEY,
                    amount       numeric NOT NULL,
                    currency     text,
                    backed_up_at timestamptz NOT NULL DEFAULT now()
                );
                INSERT INTO booking_amount_backup (id, amount, currency)
                SELECT id, amount, currency
                FROM booking
                WHERE amount <> 1250 * rounds OR currency IS DISTINCT FROM 'ILS';
                GET DIAGNOSTICS b_backed_up = ROW_COUNT;

                CREATE TABLE booking_conflict_event_amount_backup (
                    id                uuid PRIMARY KEY,
                    proposed_amount   numeric NOT NULL,
                    proposed_currency text,
                    backed_up_at      timestamptz NOT NULL DEFAULT now()
                );
                INSERT INTO booking_conflict_event_amount_backup (id, proposed_amount, proposed_currency)
                SELECT id, proposed_amount, proposed_currency
                FROM booking_conflict_event
                WHERE proposed_amount <> 1250 * proposed_rounds OR proposed_currency IS DISTINCT FROM 'ILS';
                GET DIAGNOSTICS e_backed_up = ROW_COUNT;

                -- ---------------------------------------------------------- update
                UPDATE booking b
                SET amount = 1250 * b.rounds, currency = 'ILS'
                FROM booking_amount_backup bk
                WHERE b.id = bk.id;
                GET DIAGNOSTICS b_updated = ROW_COUNT;

                UPDATE booking_conflict_event e
                SET proposed_amount = 1250 * e.proposed_rounds, proposed_currency = 'ILS'
                FROM booking_conflict_event_amount_backup bk
                WHERE e.id = bk.id;
                GET DIAGNOSTICS e_updated = ROW_COUNT;

                -- ---------------------------------------------------------- post-check
                SELECT count(*) INTO b_remaining FROM booking
                WHERE amount <> 1250 * rounds OR currency IS DISTINCT FROM 'ILS';
                SELECT count(*) INTO e_remaining FROM booking_conflict_event
                WHERE proposed_amount <> 1250 * proposed_rounds OR proposed_currency IS DISTINCT FROM 'ILS';

                IF b_updated <> b_backed_up OR e_updated <> e_backed_up OR b_remaining > 0 OR e_remaining > 0 THEN
                    RAISE EXCEPTION
                        'Aborted: post-check failed, nothing was changed. booking: % backed up, % updated, % still off-rule; '
                        'booking_conflict_event: % backed up, % updated, % still off-rule.',
                        b_backed_up, b_updated, b_remaining, e_backed_up, e_updated, e_remaining;
                END IF;

                RAISE NOTICE 'AlignBookingAmountsToRule: % booking(s) and % conflict event(s) corrected '
                    '(of which converted to ILS from another currency: % booking(s), % conflict event(s)).',
                    b_updated, e_updated, b_not_ils, e_not_ils;
            END $$;
            """;

        private const string DownSql = """
            UPDATE booking b
            SET amount = bk.amount, currency = bk.currency
            FROM booking_amount_backup bk
            WHERE b.id = bk.id;

            UPDATE booking_conflict_event e
            SET proposed_amount = bk.proposed_amount, proposed_currency = bk.proposed_currency
            FROM booking_conflict_event_amount_backup bk
            WHERE e.id = bk.id;

            DROP TABLE booking_amount_backup;
            DROP TABLE booking_conflict_event_amount_backup;
            """;
    }
}
