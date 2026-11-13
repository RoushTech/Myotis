#nullable disable

namespace RoushTech.Myotis.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class Messages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Messages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                MailboxId = table.Column<Guid>(type: "TEXT", nullable: false),
                ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                EnvelopeFrom = table.Column<string>(type: "TEXT", nullable: false),
                EnvelopeTo = table.Column<string>(type: "TEXT", nullable: false),
                Subject = table.Column<string>(type: "TEXT", nullable: false),
                From = table.Column<string>(type: "TEXT", nullable: false),
                To = table.Column<string>(type: "TEXT", nullable: false),
                Cc = table.Column<string>(type: "TEXT", nullable: false),
                SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                MessageIdHeader = table.Column<string>(type: "TEXT", nullable: true),
                Size = table.Column<int>(type: "INTEGER", nullable: false),
                AttachmentCount = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Messages", x => x.Id);
                table.ForeignKey(
                    name: "FK_Messages_Mailboxes_MailboxId",
                    column: x => x.MailboxId,
                    principalTable: "Mailboxes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MessageContents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                MessageId = table.Column<Guid>(type: "TEXT", nullable: false),
                Raw = table.Column<byte[]>(type: "BLOB", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MessageContents", x => x.Id);
                table.ForeignKey(
                    name: "FK_MessageContents_Messages_MessageId",
                    column: x => x.MessageId,
                    principalTable: "Messages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MessageContents_MessageId",
            table: "MessageContents",
            column: "MessageId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Messages_MailboxId_ReceivedAt",
            table: "Messages",
            columns: new[] { "MailboxId", "ReceivedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MessageContents");

        migrationBuilder.DropTable(
            name: "Messages");
    }
}
