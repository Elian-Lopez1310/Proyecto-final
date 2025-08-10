using System.ComponentModel.DataAnnotations;

namespace RealEstateApp.ViewModels
{
    public class PropiedadCrearViewModel
    {
        [Required(ErrorMessage = "Tipo es obligatorio.")]
        public string Tipo { get; set; }

        [Required(ErrorMessage = "Tipo de venta es obligatorio.")]
        public string TipoVenta { get; set; }

        [Required(ErrorMessage = "Ubicación es obligatoria.")]
        public string Ubicacion { get; set; }

        [Required(ErrorMessage = "Precio es obligatorio.")]
        [Range(1, double.MaxValue, ErrorMessage = "El precio debe ser mayor que 0.")]
        public decimal? Precio { get; set; }

        [Required(ErrorMessage = "Habitaciones es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe ser mayor que 0.")]
        public int? Habitaciones { get; set; }

        [Required(ErrorMessage = "Baños es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe ser mayor que 0.")]
        public int? Banos { get; set; }

        // Imagen principal
        [Required(ErrorMessage = "Imagen principal es obligatoria.")]
        public IFormFile ImagenPrincipal { get; set; }

        // Opcionales
        public string Descripcion { get; set; }
        public string Mejoras { get; set; }
        public decimal? Metros { get; set; }
        public decimal? MetrosCuadrados { get; set; }
    }
}
