using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _4Bet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAvatarDataUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarDataUrl",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarDataUrl",
                table: "Users");
        }
    }
}
