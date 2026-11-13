#nullable disable

namespace RoushTech.Myotis.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class MailboxGrants : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "LastSignInAt",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "MailboxGrants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                MailboxId = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MailboxGrants", x => x.Id);
                table.ForeignKey(
                    name: "FK_MailboxGrants_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MailboxGrants_Mailboxes_MailboxId",
                    column: x => x.MailboxId,
                    principalTable: "Mailboxes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MailboxGrants_MailboxId",
            table: "MailboxGrants",
            column: "MailboxId");

        migrationBuilder.CreateIndex(
            name: "IX_MailboxGrants_UserId_MailboxId",
            table: "MailboxGrants",
            columns: new[] { "UserId", "MailboxId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MailboxGrants");

        migrationBuilder.DropColumn(
            name: "LastSignInAt",
            table: "AspNetUsers");
    }
}
