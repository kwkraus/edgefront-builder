using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdgeFront.Builder.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStatusSyncDriftFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriftStatus",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "JoinWebUrl",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ReconcileStatus",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "TeamsWebinarId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Series");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriftStatus",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "JoinWebUrl",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "Sessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReconcileStatus",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Synced");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TeamsWebinarId",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Series",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
