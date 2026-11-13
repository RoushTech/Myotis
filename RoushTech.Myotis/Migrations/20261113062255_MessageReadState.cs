#nullable disable

namespace RoushTech.Myotis.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class MessageReadState : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "To",
            table: "Messages",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "Subject",
            table: "Messages",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "From",
            table: "Messages",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AddColumn<bool>(
            name: "IsRead",
            table: "Messages",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsRead",
            table: "Messages");

        migrationBuilder.AlterColumn<string>(
            name: "To",
            table: "Messages",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "Subject",
            table: "Messages",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "From",
            table: "Messages",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");
    }
}
