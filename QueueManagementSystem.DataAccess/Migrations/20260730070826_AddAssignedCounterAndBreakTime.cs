using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedCounterAndBreakTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedCounter",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalBreakTimeInSeconds",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedCounter",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TotalBreakTimeInSeconds",
                table: "Users");
        }
    }
}
