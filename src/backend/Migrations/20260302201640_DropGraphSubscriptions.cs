using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdgeFront.Builder.Migrations
{
    /// <inheritdoc />
    public partial class DropGraphSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GraphSubscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GraphSubscriptions",
                columns: table => new
                {
                    GraphSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientStateHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpirationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphSubscriptions", x => x.GraphSubscriptionId);
                    table.ForeignKey(
                        name: "FK_GraphSubscriptions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GraphSubscriptions_ExpirationDateTime",
                table: "GraphSubscriptions",
                column: "ExpirationDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_GraphSubscriptions_SessionId",
                table: "GraphSubscriptions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphSubscriptions_SessionId_SubscriptionId",
                table: "GraphSubscriptions",
                columns: new[] { "SessionId", "SubscriptionId" },
                unique: true);
        }
    }
}
