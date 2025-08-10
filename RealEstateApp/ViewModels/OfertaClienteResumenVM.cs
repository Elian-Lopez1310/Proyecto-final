using System;

namespace RealEstateApp.Web.ViewModels
{
    /// <summary>
    /// Resumen de ofertas por cliente para una propiedad (vista del AGENTE).
    /// </summary>
    public class OfertaResumenClienteVM
    {
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;

        public DateTime UltimaFecha { get; set; }
        public decimal MontoUltima { get; set; }
        public string EstadoUltima { get; set; } = "Pendiente";

        public int PendientesCount { get; set; }
        public int OfertaIdUltima { get; set; }
    }

}