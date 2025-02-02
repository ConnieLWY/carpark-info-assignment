using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandshakesByDC_BEAssignment.Migrations
{
    public partial class InitialModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Carparks",
                columns: table => new
                {
                    CarparkNo = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    XCoord = table.Column<float>(type: "REAL", nullable: false),
                    YCoord = table.Column<float>(type: "REAL", nullable: false),
                    CarParkType = table.Column<string>(type: "TEXT", nullable: false),
                    TypeOfParkingSystem = table.Column<string>(type: "TEXT", nullable: false),
                    FreeParking = table.Column<string>(type: "TEXT", nullable: false),
                    NightParking = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShortTermParking = table.Column<string>(type: "TEXT", nullable: false),
                    CarParkBasement = table.Column<string>(type: "TEXT", nullable: false),
                    GantryHeight = table.Column<float>(type: "REAL", nullable: false),
                    CarParkDecks = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carparks", x => x.CarparkNo);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserFavorites",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CarparkNo = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavorites", x => new { x.UserId, x.CarparkNo });
                    table.ForeignKey(
                        name: "FK_UserFavorites_Carparks_CarparkNo",
                        column: x => x.CarparkNo,
                        principalTable: "Carparks",
                        principalColumn: "CarparkNo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_CarparkNo",
                table: "UserFavorites",
                column: "CarparkNo");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_UserId",
                table: "UserFavorites",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserFavorites");

            migrationBuilder.DropTable(
                name: "Carparks");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
