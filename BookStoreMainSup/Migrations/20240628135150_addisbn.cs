using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStoreMainSup.Migrations
{
    public partial class addisbn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "isbn",
                table: "Books",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isbn",
                table: "Books");
        }
    }
}
