using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdgeFront.Builder.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionOwnerUserIdIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Sessions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerUserId",
                table: "Sessions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_OwnerUserId",
                table: "Sessions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_OwnerUserId_Status",
                table: "Sessions",
                columns: new[] { "OwnerUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_OwnerUserId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_OwnerUserId_Status",
                table: "Sessions");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerUserId",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
