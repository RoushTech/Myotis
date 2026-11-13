#nullable disable

namespace RoushTech.Myotis.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class Mailboxes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Mailboxes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Mailboxes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SmtpCredentials",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                MailboxId = table.Column<Guid>(type: "TEXT", nullable: false),
                Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                PasswordHash = table.Column<byte[]>(type: "BLOB", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SmtpCredentials", x => x.Id);
                table.ForeignKey(
                    name: "FK_SmtpCredentials_Mailboxes_MailboxId",
                    column: x => x.MailboxId,
                    principalTable: "Mailboxes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Mailboxes_Name",
            table: "Mailboxes",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SmtpCredentials_MailboxId",
            table: "SmtpCredentials",
            column: "MailboxId");

        migrationBuilder.CreateIndex(
            name: "IX_SmtpCredentials_Username",
            table: "SmtpCredentials",
            column: "Username",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SmtpCredentials");

        migrationBuilder.DropTable(
            name: "Mailboxes");
    }
}
