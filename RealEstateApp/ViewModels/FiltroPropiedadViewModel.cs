using RealEstateApp.Shared.Dtos;

namespace RealEstateApp.Web.ViewModels
{
    public class FiltroPropiedadViewModel
    {
        public string? Codigo { get; set; }
        public string? Tipo { get; set; }
        public decimal? PrecioMin { get; set; }
        public decimal? PrecioMax { get; set; }
        public int? Habitaciones { get; set; }
        public int? Banos { get; set; }

        // 🔹 Nueva propiedad
        public List<AgenteDto> Agentes { get; set; } = new();

        public List<PropiedadDto> Resultados { get; set; } = new();

        public List<string> TiposDisponibles { get; set; } = new();
    }
}
