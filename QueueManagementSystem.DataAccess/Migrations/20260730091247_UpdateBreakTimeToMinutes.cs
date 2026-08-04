using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBreakTimeToMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalBreakTimeInSeconds",
                table: "Users");

            migrationBuilder.AddColumn<double>(
                name: "TotalBreakTimeInMinutes",
                table: "Users",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalBreakTimeInMinutes",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "TotalBreakTimeInSeconds",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
