using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiMarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IntegridadRelacional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KitReferenciaSku",
                table: "KitComponentes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductoSku",
                table: "KitComponentes",
                type: "TEXT",
                nullable: true);

            // Conserva la composición de bases existentes antes de activar las FK de SKU.
            migrationBuilder.Sql("UPDATE KitComponentes SET ProductoSku = ComponenteSku WHERE EsKit = 0;");
            migrationBuilder.Sql("UPDATE KitComponentes SET KitReferenciaSku = ComponenteSku WHERE EsKit = 1;");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Productos_Sku",
                table: "Productos",
                column: "Sku");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_ClienteId",
                table: "Pedidos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Referencia",
                table: "MovimientosInventario",
                column: "Referencia");

            migrationBuilder.CreateIndex(
                name: "IX_KitComponentes_KitReferenciaSku",
                table: "KitComponentes",
                column: "KitReferenciaSku");

            migrationBuilder.CreateIndex(
                name: "IX_KitComponentes_ProductoSku",
                table: "KitComponentes",
                column: "ProductoSku");

            migrationBuilder.AddForeignKey(
                name: "FK_Despachos_Pedidos_PedidoId",
                table: "Despachos",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Devoluciones_Pedidos_PedidoId",
                table: "Devoluciones",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KitComponentes_Kits_KitReferenciaSku",
                table: "KitComponentes",
                column: "KitReferenciaSku",
                principalTable: "Kits",
                principalColumn: "Sku",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KitComponentes_Productos_ProductoSku",
                table: "KitComponentes",
                column: "ProductoSku",
                principalTable: "Productos",
                principalColumn: "Sku",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_Pedidos_Referencia",
                table: "MovimientosInventario",
                column: "Referencia",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Pedidos_PedidoId",
                table: "Pagos",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Clientes_ClienteId",
                table: "Pedidos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Despachos_Pedidos_PedidoId",
                table: "Despachos");

            migrationBuilder.DropForeignKey(
                name: "FK_Devoluciones_Pedidos_PedidoId",
                table: "Devoluciones");

            migrationBuilder.DropForeignKey(
                name: "FK_KitComponentes_Kits_KitReferenciaSku",
                table: "KitComponentes");

            migrationBuilder.DropForeignKey(
                name: "FK_KitComponentes_Productos_ProductoSku",
                table: "KitComponentes");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_Pedidos_Referencia",
                table: "MovimientosInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Pedidos_PedidoId",
                table: "Pagos");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Clientes_ClienteId",
                table: "Pedidos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Productos_Sku",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_ClienteId",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_Referencia",
                table: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_KitComponentes_KitReferenciaSku",
                table: "KitComponentes");

            migrationBuilder.DropIndex(
                name: "IX_KitComponentes_ProductoSku",
                table: "KitComponentes");

            migrationBuilder.DropColumn(
                name: "KitReferenciaSku",
                table: "KitComponentes");

            migrationBuilder.DropColumn(
                name: "ProductoSku",
                table: "KitComponentes");
        }
    }
}
