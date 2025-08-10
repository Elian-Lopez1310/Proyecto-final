using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateApp.Datos.Migrations
{
    /// <inheritdoc />
    public partial class CrearRelacionMejora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mejoras",
                table: "Propiedades");

            migrationBuilder.CreateTable(
                name: "Mejoras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mejoras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropiedadMejora",
                columns: table => new
                {
                    MejorasId = table.Column<int>(type: "int", nullable: false),
                    PropiedadesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropiedadMejora", x => new { x.MejorasId, x.PropiedadesId });
                    table.ForeignKey(
                        name: "FK_PropiedadMejora_Mejoras_MejorasId",
                        column: x => x.MejorasId,
                        principalTable: "Mejoras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PropiedadMejora_Propiedades_PropiedadesId",
                        column: x => x.PropiedadesId,
                        principalTable: "Propiedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropiedadMejora_PropiedadesId",
                table: "PropiedadMejora",
                column: "PropiedadesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropiedadMejora");

            migrationBuilder.DropTable(
                name: "Mejoras");

            migrationBuilder.AddColumn<string>(
                name: "Mejoras",
                table: "Propiedades",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
