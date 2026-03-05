using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdgeFront.Builder.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionPresentersAndCoordinators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionCoordinators",
                columns: table => new
                {
                    SessionCoordinatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntraUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionCoordinators", x => x.SessionCoordinatorId);
                    table.ForeignKey(
                        name: "FK_SessionCoordinators_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionPresenters",
                columns: table => new
                {
                    SessionPresenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntraUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPresenters", x => x.SessionPresenterId);
                    table.ForeignKey(
                        name: "FK_SessionPresenters_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionCoordinators_SessionId",
                table: "SessionCoordinators",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionCoordinators_SessionId_EntraUserId",
                table: "SessionCoordinators",
                columns: new[] { "SessionId", "EntraUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionPresenters_SessionId",
                table: "SessionPresenters",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPresenters_SessionId_EntraUserId",
                table: "SessionPresenters",
                columns: new[] { "SessionId", "EntraUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionCoordinators");

            migrationBuilder.DropTable(
                name: "SessionPresenters");
        }
    }
}
