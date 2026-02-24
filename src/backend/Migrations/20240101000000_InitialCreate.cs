using System;
using Microsoft.EntityFrameworkCore.Migrations;
using EdgeFront.Builder.Infrastructure.Data;

#nullable disable

namespace EdgeFront.Builder.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(AppDbContext))]
    [Migration("20240101000000_InitialCreate")]
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    SeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.SeriesId);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TeamsWebinarId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DriftStatus = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "None"),
                    ReconcileStatus = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Synced"),
                    LastSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_Sessions_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "SeriesId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeriesMetrics",
                columns: table => new
                {
                    SeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalRegistrations = table.Column<int>(type: "int", nullable: false),
                    TotalAttendees = table.Column<int>(type: "int", nullable: false),
                    UniqueRegistrantAccountDomains = table.Column<int>(type: "int", nullable: false),
                    UniqueAccountsInfluenced = table.Column<int>(type: "int", nullable: false),
                    WarmAccounts = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesMetrics", x => x.SeriesId);
                    table.ForeignKey(
                        name: "FK_SeriesMetrics_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "SeriesId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GraphSubscriptions",
                columns: table => new
                {
                    GraphSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscriptionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientStateHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpirationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "NormalizedAttendances",
                columns: table => new
                {
                    AttendanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmailDomain = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Attended = table.Column<bool>(type: "bit", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    DurationPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FirstJoinAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLeaveAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormalizedAttendances", x => x.AttendanceId);
                    table.ForeignKey(
                        name: "FK_NormalizedAttendances_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NormalizedRegistrations",
                columns: table => new
                {
                    RegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmailDomain = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormalizedRegistrations", x => x.RegistrationId);
                    table.ForeignKey(
                        name: "FK_NormalizedRegistrations_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionMetrics",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalRegistrations = table.Column<int>(type: "int", nullable: false),
                    TotalAttendees = table.Column<int>(type: "int", nullable: false),
                    UniqueRegistrantAccountDomains = table.Column<int>(type: "int", nullable: false),
                    UniqueAttendeeAccountDomains = table.Column<int>(type: "int", nullable: false),
                    WarmAccountsTriggered = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionMetrics", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_SessionMetrics_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Indexes on Series
            migrationBuilder.CreateIndex(
                name: "IX_Series_OwnerUserId_Title",
                table: "Series",
                columns: new[] { "OwnerUserId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Series_OwnerUserId_CreatedAt",
                table: "Series",
                columns: new[] { "OwnerUserId", "CreatedAt" });

            // Indexes on Sessions
            migrationBuilder.CreateIndex(
                name: "IX_Sessions_SeriesId_StartsAt",
                table: "Sessions",
                columns: new[] { "SeriesId", "StartsAt" });

            // Indexes on NormalizedRegistrations
            migrationBuilder.CreateIndex(
                name: "IX_NormalizedRegistrations_OwnerUserId_SessionId_Email",
                table: "NormalizedRegistrations",
                columns: new[] { "OwnerUserId", "SessionId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedRegistrations_SessionId_EmailDomain",
                table: "NormalizedRegistrations",
                columns: new[] { "SessionId", "EmailDomain" });

            // Indexes on NormalizedAttendances
            migrationBuilder.CreateIndex(
                name: "IX_NormalizedAttendances_OwnerUserId_SessionId_Email",
                table: "NormalizedAttendances",
                columns: new[] { "OwnerUserId", "SessionId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedAttendances_SessionId_EmailDomain",
                table: "NormalizedAttendances",
                columns: new[] { "SessionId", "EmailDomain" });

            // Indexes on GraphSubscriptions
            migrationBuilder.CreateIndex(
                name: "IX_GraphSubscriptions_SessionId_SubscriptionId",
                table: "GraphSubscriptions",
                columns: new[] { "SessionId", "SubscriptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GraphSubscriptions_SessionId",
                table: "GraphSubscriptions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphSubscriptions_ExpirationDateTime",
                table: "GraphSubscriptions",
                column: "ExpirationDateTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SessionMetrics");
            migrationBuilder.DropTable(name: "SeriesMetrics");
            migrationBuilder.DropTable(name: "GraphSubscriptions");
            migrationBuilder.DropTable(name: "NormalizedAttendances");
            migrationBuilder.DropTable(name: "NormalizedRegistrations");
            migrationBuilder.DropTable(name: "Sessions");
            migrationBuilder.DropTable(name: "Series");
        }
    }
}
