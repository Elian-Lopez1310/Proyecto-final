using System;
using System.Collections.Generic;
using RealEstateApp.Shared.Dtos;

namespace RealEstateApp.Web.ViewModels
{
    // Resumen de un último mensaje recibido por el agente
    public class MensajeResumenVM
    {
        public int PropiedadId { get; set; }
        public string CodigoPropiedad { get; set; } = string.Empty;

        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;

        public string Texto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public bool Leido { get; set; }
    }

    public class PropiedadesDelAgenteViewModel
    {
        // Id del agente actual (útil para filtros en controlador/vista)
        public int AgenteId { get; set; }

        // Nombre para encabezado
        public string NombreAgente { get; set; } = "Agente";

        // Propiedades del agente autenticado
        public List<PropiedadDto> PropiedadesAgente { get; set; } = new();

        // Otras propiedades disponibles (si las usas en otra vista/panel)
        public List<PropiedadDto> PropiedadesDisponibles { get; set; } = new();

        // Información resumida del agente (DTO)
        public AgenteDto? Agente { get; set; }

        // (Opcional) Datos de la última publicación para evitar duplicados o resaltar
        public int? JustPublishedId { get; set; }
        public string? JustPublishedCodigo { get; set; }

        // 👉 NUEVO: últimos mensajes que llegaron al agente
        public List<MensajeResumenVM> MensajesRecientes { get; set; } = new();
    }
}
