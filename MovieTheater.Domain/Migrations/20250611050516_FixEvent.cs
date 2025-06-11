using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieTheater.Domain.Migrations
{
    /// <inheritdoc />
    public partial class FixEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Promotions_PromotionId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_PromotionId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "Events");

            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "Promotions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_EventId",
                table: "Promotions",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_Events_EventId",
                table: "Promotions",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_Events_EventId",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_EventId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Promotions");

            migrationBuilder.AddColumn<Guid>(
                name: "PromotionId",
                table: "Events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Events_PromotionId",
                table: "Events",
                column: "PromotionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Promotions_PromotionId",
                table: "Events",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
