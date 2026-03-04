using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdgeFront.Builder.Migrations
{
    /// <inheritdoc />
    public partial class AddJoinWebUrlToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JoinWebUrl",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JoinWebUrl",
                table: "Sessions");
        }
    }
}
