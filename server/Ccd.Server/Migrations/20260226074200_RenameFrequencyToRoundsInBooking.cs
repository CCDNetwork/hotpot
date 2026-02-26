using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ccd.Server.Migrations
{
    /// <inheritdoc />
    public partial class RenameFrequencyToRoundsInBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "frequency",
                table: "booking_log",
                newName: "rounds");

            migrationBuilder.RenameColumn(
                name: "frequency",
                table: "booking",
                newName: "rounds");

            migrationBuilder.UpdateData(
                table: "settings",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "funding_sources",
                value: new List<string> { "BHA", "Other" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "rounds",
                table: "booking_log",
                newName: "frequency");

            migrationBuilder.RenameColumn(
                name: "rounds",
                table: "booking",
                newName: "frequency");

            migrationBuilder.UpdateData(
                table: "settings",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "funding_sources",
                value: new List<string> { "BHA", "Other" });
        }
    }
}
