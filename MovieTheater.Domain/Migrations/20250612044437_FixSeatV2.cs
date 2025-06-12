using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieTheater.Domain.Migrations
{
    /// <inheritdoc />
    public partial class FixSeatV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShowTimeSeat_Seats_SeatId",
                table: "ShowTimeSeat");

            migrationBuilder.DropForeignKey(
                name: "FK_ShowTimeSeat_Showtimes_ShowTimeId",
                table: "ShowTimeSeat");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShowTimeSeat",
                table: "ShowTimeSeat");

            migrationBuilder.RenameTable(
                name: "ShowTimeSeat",
                newName: "ShowTimeSeats");

            migrationBuilder.RenameIndex(
                name: "IX_ShowTimeSeat_SeatId",
                table: "ShowTimeSeats",
                newName: "IX_ShowTimeSeats_SeatId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShowTimeSeats",
                table: "ShowTimeSeats",
                columns: new[] { "ShowTimeId", "SeatId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ShowTimeSeats_Seats_SeatId",
                table: "ShowTimeSeats",
                column: "SeatId",
                principalTable: "Seats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShowTimeSeats_Showtimes_ShowTimeId",
                table: "ShowTimeSeats",
                column: "ShowTimeId",
                principalTable: "Showtimes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShowTimeSeats_Seats_SeatId",
                table: "ShowTimeSeats");

            migrationBuilder.DropForeignKey(
                name: "FK_ShowTimeSeats_Showtimes_ShowTimeId",
                table: "ShowTimeSeats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShowTimeSeats",
                table: "ShowTimeSeats");

            migrationBuilder.RenameTable(
                name: "ShowTimeSeats",
                newName: "ShowTimeSeat");

            migrationBuilder.RenameIndex(
                name: "IX_ShowTimeSeats_SeatId",
                table: "ShowTimeSeat",
                newName: "IX_ShowTimeSeat_SeatId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShowTimeSeat",
                table: "ShowTimeSeat",
                columns: new[] { "ShowTimeId", "SeatId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ShowTimeSeat_Seats_SeatId",
                table: "ShowTimeSeat",
                column: "SeatId",
                principalTable: "Seats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShowTimeSeat_Showtimes_ShowTimeId",
                table: "ShowTimeSeat",
                column: "ShowTimeId",
                principalTable: "Showtimes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
