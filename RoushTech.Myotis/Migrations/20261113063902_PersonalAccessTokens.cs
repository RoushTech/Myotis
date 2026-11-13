#nullable disable

namespace RoushTech.Myotis.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class PersonalAccessTokens : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PersonalAccessTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                TokenHash = table.Column<byte[]>(type: "BLOB", nullable: false),
                Prefix = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonalAccessTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonalAccessTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PersonalAccessTokens_TokenHash",
            table: "PersonalAccessTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PersonalAccessTokens_UserId",
            table: "PersonalAccessTokens",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PersonalAccessTokens");
    }
}
