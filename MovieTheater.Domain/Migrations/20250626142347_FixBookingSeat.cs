using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieTheater.Domain.Migrations
{
    /// <inheritdoc />
    public partial class FixBookingSeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BookingFoods",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "BookingFoods",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "BookingFoods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "BookingFoods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "BookingFoods",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "BookingFoods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "BookingFoods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "BookingFoods",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BookingFoods");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "BookingFoods");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "BookingFoods");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BookingFoods");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "BookingFoods");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "BookingFoods");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "BookingFoods");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "BookingFoods");
        }
    }
}
