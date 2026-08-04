using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCalledAtColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CalledAt",
                table: "QueueTickets",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalledAt",
                table: "QueueTickets");
        }
    }
}
