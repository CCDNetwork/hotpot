using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ccd.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddConflictEventsAndDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "dashboard_display_currency",
                table: "settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_prebooking",
                table: "booking_log",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "submission_id",
                table: "booking_log",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "booking_conflict_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requesting_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocking_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_key = table.Column<string>(type: "text", nullable: true),
                    overlap_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    overlap_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    blocking_booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    proposed_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    proposed_currency = table.Column<string>(type: "text", nullable: true),
                    proposed_modality = table.Column<string>(type: "text", nullable: true),
                    proposed_rounds = table.Column<int>(type: "integer", nullable: false),
                    first_detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    detection_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_conflict_event", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_conflict_event_booking_blocking_booking_id",
                        column: x => x.blocking_booking_id,
                        principalTable: "booking",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_booking_conflict_event_organization_blocking_organization_id",
                        column: x => x.blocking_organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_booking_conflict_event_organization_requesting_organization~",
                        column: x => x.requesting_organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exchange_rate",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: true),
                    base_currency = table.Column<string>(type: "text", nullable: true),
                    rate = table.Column<decimal>(type: "numeric(24,12)", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "text", nullable: true),
                    fetched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchange_rate", x => x.id);
                });

            migrationBuilder.UpdateData(
                table: "settings",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "dashboard_display_currency", "funding_sources" },
                values: new object[] { "USD", new List<string> { "BHA", "Other" } });

            migrationBuilder.CreateIndex(
                name: "ix_booking_log_is_prebooking_created_at",
                table: "booking_log",
                columns: new[] { "is_prebooking", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_booking_log_submission_id",
                table: "booking_log",
                column: "submission_id");

            migrationBuilder.CreateIndex(
                name: "idx_conflict_event_fingerprint",
                table: "booking_conflict_event",
                columns: new[] { "requesting_organization_id", "blocking_organization_id", "subject_key", "overlap_start_date", "overlap_end_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_conflict_event_blocking_booking_id",
                table: "booking_conflict_event",
                column: "blocking_booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_conflict_event_blocking_organization_id",
                table: "booking_conflict_event",
                column: "blocking_organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_conflict_event_first_detected_at",
                table: "booking_conflict_event",
                column: "first_detected_at");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_rate_currency_year_month_source",
                table: "exchange_rate",
                columns: new[] { "currency", "year", "month", "source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_conflict_event");

            migrationBuilder.DropTable(
                name: "exchange_rate");

            migrationBuilder.DropIndex(
                name: "ix_booking_log_is_prebooking_created_at",
                table: "booking_log");

            migrationBuilder.DropIndex(
                name: "ix_booking_log_submission_id",
                table: "booking_log");

            migrationBuilder.DropColumn(
                name: "dashboard_display_currency",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "is_prebooking",
                table: "booking_log");

            migrationBuilder.DropColumn(
                name: "submission_id",
                table: "booking_log");

            migrationBuilder.UpdateData(
                table: "settings",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "funding_sources",
                value: new List<string> { "BHA", "Other" });
        }
    }
}
