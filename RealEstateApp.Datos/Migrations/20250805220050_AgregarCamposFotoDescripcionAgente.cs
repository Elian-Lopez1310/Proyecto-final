using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateApp.Datos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposFotoDescripcionAgente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Agentes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Foto",
                table: "Agentes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Agentes");

            migrationBuilder.DropColumn(
                name: "Foto",
                table: "Agentes");
        }
    }
}
