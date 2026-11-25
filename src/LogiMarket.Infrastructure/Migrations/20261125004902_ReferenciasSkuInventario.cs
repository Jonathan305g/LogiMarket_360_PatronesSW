using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiMarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReferenciasSkuInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DevolucionReintegros_Sku",
                table: "DevolucionReintegros",
                column: "Sku");

            migrationBuilder.AddForeignKey(
                name: "FK_DevolucionReintegros_Productos_Sku",
                table: "DevolucionReintegros",
                column: "Sku",
                principalTable: "Productos",
                principalColumn: "Sku",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_Productos_Sku",
                table: "MovimientosInventario",
                column: "Sku",
                principalTable: "Productos",
                principalColumn: "Sku",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DevolucionReintegros_Productos_Sku",
                table: "DevolucionReintegros");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_Productos_Sku",
                table: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_DevolucionReintegros_Sku",
                table: "DevolucionReintegros");
        }
    }
}
