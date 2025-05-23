using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieTheater.Domain.Migrations
{
    /// <inheritdoc />
    public partial class FixName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_otps",
                table: "otps");

            migrationBuilder.RenameTable(
                name: "otps",
                newName: "OtpStorages");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OtpStorages",
                table: "OtpStorages",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OtpStorages",
                table: "OtpStorages");

            migrationBuilder.RenameTable(
                name: "OtpStorages",
                newName: "otps");

            migrationBuilder.AddPrimaryKey(
                name: "PK_otps",
                table: "otps",
                column: "Id");
        }
    }
}
