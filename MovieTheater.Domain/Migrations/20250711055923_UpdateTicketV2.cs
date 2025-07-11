using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieTheater.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTicketV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Showtime",
                table: "Tickets");

            migrationBuilder.AddColumn<Guid>(
                name: "ShowTimeId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ShowTimeId",
                table: "Tickets",
                column: "ShowTimeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Showtimes_ShowTimeId",
                table: "Tickets",
                column: "ShowTimeId",
                principalTable: "Showtimes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Showtimes_ShowTimeId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ShowTimeId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ShowTimeId",
                table: "Tickets");

            migrationBuilder.AddColumn<DateTime>(
                name: "Showtime",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
