using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ccd.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "user",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "status",
                value: "Active");

            migrationBuilder.Sql(
                @"ALTER TABLE ""user"" ADD CONSTRAINT ""CK_user_status"" CHECK (""status"" IN ('Pending', 'Active', 'Disabled'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE ""user"" DROP CONSTRAINT IF EXISTS ""CK_user_status""");

            migrationBuilder.DropColumn(
                name: "status",
                table: "user");
        }
    }
}
