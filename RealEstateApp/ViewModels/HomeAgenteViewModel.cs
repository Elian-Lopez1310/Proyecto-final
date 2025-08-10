using RealEstateApp.Shared.Dtos;

namespace RealEstateApp.ViewModels
{
    public class HomeAgenteViewModel
    {
        public string NombreAgente { get; set; }
        public List<PropiedadDto> PropiedadesAgente { get; set; }
        public List<PropiedadDto> PropiedadesDisponibles { get; set; }
    }
}