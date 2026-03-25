using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdgeFront.Builder.Migrations
{
    /// <inheritdoc />
    public partial class AddNameFieldsToNormalizedRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "NormalizedRegistrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "NormalizedRegistrations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "NormalizedRegistrations");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "NormalizedRegistrations");
        }
    }
}
