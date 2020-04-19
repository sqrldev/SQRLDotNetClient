using Microsoft.EntityFrameworkCore.Migrations;

namespace SQRLDotNetClientUI.Migrations
{
    public partial class AddStartMinimized : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StartMinimized",
                table: "UserData",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartMinimized",
                table: "UserData");
        }
    }
}
