using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCoop1._0.Migrations
{
    /// <inheritdoc />
    public partial class mejoraslogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "usuarioLogin",
                table: "Socios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "usuarioLogin",
                table: "Socios");
        }
    }
}
