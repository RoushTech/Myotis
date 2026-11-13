#nullable disable

namespace RoushTech.Myotis.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class InstanceSettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InstanceSettings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                Value = table.Column<string>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InstanceSettings", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_InstanceSettings_Key",
            table: "InstanceSettings",
            column: "Key",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "InstanceSettings");
    }
}
