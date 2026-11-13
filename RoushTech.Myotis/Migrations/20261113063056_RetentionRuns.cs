#nullable disable

namespace RoushTech.Myotis.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class RetentionRuns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RetentionRuns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                DeletedCount = table.Column<int>(type: "INTEGER", nullable: false),
                Error = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RetentionRuns", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RetentionRuns");
    }
}
