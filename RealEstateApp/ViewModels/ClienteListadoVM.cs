using System;

namespace RealEstateApp.Web.ViewModels
{
    public class ClienteListadoVM
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;

        public bool TieneChat { get; set; }
        public int NoLeidos { get; set; }
        public DateTime? UltimaFecha { get; set; }
        public string UltimoTexto { get; set; } = string.Empty;
    }
}
