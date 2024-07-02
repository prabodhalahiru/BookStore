using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStoreMainSup.Migrations
{
    public partial class isbnChanged : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ISBN",
                table: "Books",
                newName: "isbn");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isbn",
                table: "Books",
                newName: "ISBN");
        }
    }
}
