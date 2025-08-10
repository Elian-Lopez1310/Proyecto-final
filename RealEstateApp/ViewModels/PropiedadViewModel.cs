namespace RealEstateApp.Web.ViewModels
{
    public class PropiedadViewModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; }

        public decimal Precio { get; set; }
        public string TipoVenta { get; set; }     // Venta o Alquiler
        public string Tipo { get; set; }          // Casa, Apartamento, Proyecto
        public int Metros { get; set; }
        public int Habitaciones { get; set; }  // en lugar de int?
        public string Titulo { get; set; } = "";
        public int Banos { get; set; }
        public string Ubicacion { get; set; }
        public string ImagenPrincipal { get; set; } // Url de la imagen principal
        public bool EsFavorita { get; set; }        // true si el cliente la marcó como favorita
        public string ImagenUrl { get; set; }

        public int MetrosCuadrados { get; set; }
    }
}
