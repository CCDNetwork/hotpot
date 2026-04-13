using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ccd.Server.Migrations
{
    /// <inheritdoc />
    public partial class MakeBookingFileIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_beneficary_deduplication_file_file_id",
                table: "beneficary_deduplication");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_file_file_id",
                table: "booking");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_log_file_file_id",
                table: "booking_log");

            migrationBuilder.AlterColumn<Guid>(
                name: "file_id",
                table: "booking_log",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "file_id",
                table: "booking",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "file_id",
                table: "beneficary_deduplication",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "fk_beneficary_deduplication_file_file_id",
                table: "beneficary_deduplication",
                column: "file_id",
                principalTable: "file",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_booking_file_file_id",
                table: "booking",
                column: "file_id",
                principalTable: "file",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_booking_log_file_file_id",
                table: "booking_log",
                column: "file_id",
                principalTable: "file",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_beneficary_deduplication_file_file_id",
                table: "beneficary_deduplication");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_file_file_id",
                table: "booking");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_log_file_file_id",
                table: "booking_log");

            migrationBuilder.AlterColumn<Guid>(
                name: "file_id",
                table: "booking_log",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "file_id",
                table: "booking",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "file_id",
                table: "beneficary_deduplication",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "settings",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "funding_sources",
                value: new List<string> { "BHA", "Other" });

            migrationBuilder.AddForeignKey(
                name: "fk_beneficary_deduplication_file_file_id",
                table: "beneficary_deduplication",
                column: "file_id",
                principalTable: "file",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_file_file_id",
                table: "booking",
                column: "file_id",
                principalTable: "file",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_log_file_file_id",
                table: "booking_log",
                column: "file_id",
                principalTable: "file",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
