using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _4Bet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParcingSportEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SportEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HomeTeam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AwayTeam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SportKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HomeWinOdds = table.Column<double>(type: "double precision", nullable: false),
                    DrawOdds = table.Column<double>(type: "double precision", nullable: false),
                    AwayWinOdds = table.Column<double>(type: "double precision", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SportEvents_ExternalId",
                table: "SportEvents",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SportEvents");
        }
    }
}
