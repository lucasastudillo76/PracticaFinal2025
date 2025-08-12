using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCoop1._0.Migrations
{
    /// <inheritdoc />
    public partial class admin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Socios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Socios_UsuarioId",
                table: "Socios",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Socios_Usuarios_UsuarioId",
                table: "Socios",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Socios_Usuarios_UsuarioId",
                table: "Socios");

            migrationBuilder.DropIndex(
                name: "IX_Socios_UsuarioId",
                table: "Socios");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Socios");
        }
    }
}
