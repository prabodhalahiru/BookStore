using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStoreMainSup.Migrations
{
    public partial class rmvdIsLoggedIn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLoggedIn",
                table: "Users");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLoggedIn",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
