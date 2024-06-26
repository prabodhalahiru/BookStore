using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStoreMainSup.Migrations
{
    public partial class AddDiscountToBooks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Discount",
                table: "Books",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Books");
        }
    }
}
