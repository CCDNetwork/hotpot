using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ccd.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowedEmailDomainsAndChangeLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "change_log_entry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "text", nullable: true),
                    old_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_name = table.Column<string>(type: "text", nullable: true),
                    subject_email = table.Column<string>(type: "text", nullable: true),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_by_email = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_change_log_entry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_email_domain",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_email_domain", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_email_domain_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_change_log_entry_created_at",
                table: "change_log_entry",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_change_log_entry_organization_id_entity_type_created_at",
                table: "change_log_entry",
                columns: new[] { "organization_id", "entity_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_email_domain_domain",
                table: "organization_email_domain",
                column: "domain");

            migrationBuilder.CreateIndex(
                name: "ix_organization_email_domain_organization_id_domain",
                table: "organization_email_domain",
                columns: new[] { "organization_id", "domain" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "change_log_entry");

            migrationBuilder.DropTable(
                name: "organization_email_domain");
        }
    }
}
