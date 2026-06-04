using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RureSubPostsWriter.Migrations
{
    /// <inheritdoc />
    public partial class Initial4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Posts",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<string[]>(
                name: "MediaFilePaths",
                table: "Posts",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaFilePaths",
                table: "Posts");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Posts",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);
        }
    }
}
