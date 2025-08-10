namespace RealEstateApp.Web.ViewModels
{
    public class AgenteViewModel
    {
        public int Id { get; set; }

        public string Nombre { get; set; }

        public string Apellido { get; set; } // ✅ AÑADIR ESTA LÍNEA

        public string NombreCompleto => $"{Nombre} {Apellido}";

        public string FotoUrl { get; set; }

        public string Correo { get; set; }

        public string Telefono { get; set; }

        public string Descripcion { get; set; } // ✅ Nueva propiedad descriptiva

        public IFormFile Foto { get; set; }
    }
}
