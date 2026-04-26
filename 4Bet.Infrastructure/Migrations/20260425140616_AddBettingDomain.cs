using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _4Bet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBettingDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stake = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CombinedOdds = table.Column<double>(type: "double precision", nullable: false),
                    PotentialPayout = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SettledPayout = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BetLegs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SportEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Selection = table.Column<int>(type: "integer", nullable: false),
                    LockedOdds = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BetLegs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BetLegs_Bets_BetId",
                        column: x => x.BetId,
                        principalTable: "Bets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BetLegs_SportEvents_SportEventId",
                        column: x => x.SportEventId,
                        principalTable: "SportEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BetLegs_BetId_SportEventId",
                table: "BetLegs",
                columns: new[] { "BetId", "SportEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BetLegs_SportEventId",
                table: "BetLegs",
                column: "SportEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Bets_CreatedAt",
                table: "Bets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Bets_UserId_Status",
                table: "Bets",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BetLegs");

            migrationBuilder.DropTable(
                name: "Bets");
        }
    }
}
