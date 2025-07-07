using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieTheater.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestPhoneNumber",
                table: "Tickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TicketType",
                table: "Tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestPhoneNumber",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TicketType",
                table: "Tickets");
        }
    }
}
