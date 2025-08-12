using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCoop1._0.Migrations
{
    /// <inheritdoc />
    public partial class CrearTablaUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Usuarios",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "Usuarios",
                newName: "usuario");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Usuarios",
                newName: "contrasenia");

            migrationBuilder.RenameColumn(
                name: "IsAdmin",
                table: "Usuarios",
                newName: "esadmin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "Usuarios",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "usuario",
                table: "Usuarios",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "esadmin",
                table: "Usuarios",
                newName: "IsAdmin");

            migrationBuilder.RenameColumn(
                name: "contrasenia",
                table: "Usuarios",
                newName: "Password");
        }
    }
}
