using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _4Bet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamIdentityCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamIdentities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ProviderTeamId = table.Column<int>(type: "integer", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: false),
                    TeamNameNormalized = table.Column<string>(type: "text", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamIdentities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamIdentities_Provider_ProviderTeamId",
                table: "TeamIdentities",
                columns: new[] { "Provider", "ProviderTeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamIdentities_TeamNameNormalized",
                table: "TeamIdentities",
                column: "TeamNameNormalized");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamIdentities");
        }
    }
}
