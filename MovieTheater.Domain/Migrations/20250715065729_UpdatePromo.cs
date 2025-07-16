using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieTheater.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePromo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PromotionId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PromotionId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PromotionId",
                table: "Tickets",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PromotionId",
                table: "Invoices",
                column: "PromotionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Promotions_PromotionId",
                table: "Invoices",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Promotions_PromotionId",
                table: "Tickets",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Promotions_PromotionId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Promotions_PromotionId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_PromotionId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PromotionId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "Invoices");
        }
    }
}
