using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ccd.Server.Migrations
{
    /// <inheritdoc />
    public partial class DropDashboardDisplayCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dashboard_display_currency",
                table: "settings");

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
            migrationBuilder.AddColumn<string>(
                name: "dashboard_display_currency",
                table: "settings",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "settings",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "dashboard_display_currency", "funding_sources" },
                values: new object[] { "USD", new List<string> { "BHA", "Other" } });
        }
    }
}
