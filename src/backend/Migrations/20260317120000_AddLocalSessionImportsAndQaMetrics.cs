using System;
using EdgeFront.Builder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdgeFront.Builder.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260317120000_AddLocalSessionImportsAndQaMetrics")]
    public partial class AddLocalSessionImportsAndQaMetrics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnsweredQaQuestions",
                table: "SeriesMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalQaQuestions",
                table: "SeriesMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AnsweredQaQuestions",
                table: "SessionMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalQaQuestions",
                table: "SessionMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "NormalizedQaEntries",
                columns: table => new
                {
                    QaEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AskedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AskedByDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AskedByEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    IsAnswered = table.Column<bool>(type: "bit", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnswerText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormalizedQaEntries", x => x.QaEntryId);
                    table.ForeignKey(
                        name: "FK_NormalizedQaEntries_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionImportSummaries",
                columns: table => new
                {
                    SessionImportSummaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionImportSummaries", x => x.SessionImportSummaryId);
                    table.ForeignKey(
                        name: "FK_SessionImportSummaries_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedQaEntries_OwnerUserId_SessionId",
                table: "NormalizedQaEntries",
                columns: new[] { "OwnerUserId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedQaEntries_SessionId",
                table: "NormalizedQaEntries",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionImportSummaries_SessionId_ImportType",
                table: "SessionImportSummaries",
                columns: new[] { "SessionId", "ImportType" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NormalizedQaEntries");

            migrationBuilder.DropTable(
                name: "SessionImportSummaries");

            migrationBuilder.DropColumn(
                name: "AnsweredQaQuestions",
                table: "SeriesMetrics");

            migrationBuilder.DropColumn(
                name: "TotalQaQuestions",
                table: "SeriesMetrics");

            migrationBuilder.DropColumn(
                name: "AnsweredQaQuestions",
                table: "SessionMetrics");

            migrationBuilder.DropColumn(
                name: "TotalQaQuestions",
                table: "SessionMetrics");
        }
    }
}
