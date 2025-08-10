using RealEstateApp.Entidades;
using System;
using System.Collections.Generic;
using static RealEstateApp.Web.ViewModels.AgenteOfertasClienteVM;

namespace RealEstateApp.Web.ViewModels
{
    public class DetallePropiedadViewModel
    {
        // ===== Identificación / datos base
        public int PropiedadId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string TipoVenta { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Habitaciones { get; set; }
        public int Banos { get; set; }

        // En tu VM anterior usas int Metros; la vista usa a veces MetrosCuadrados.
        public int Metros { get; set; }
        public decimal MetrosCuadrados { get; set; }  // si no lo tienes en DB, puedes mapearlo con (decimal)Metros

        public string Descripcion { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;

        // ===== Imágenes / mejoras (lo que pide la vista)
        public string ImagenPrincipal { get; set; } = string.Empty;
        public List<string> Imagenes { get; set; } = new();
        public List<string> Mejoras { get; set; } = new();
        public List<OfertaViewModel> Ofertas { get; set; }

        // ===== Agente
        public string AgenteNombre { get; set; } = string.Empty;
        public string AgenteCorreo { get; set; } = string.Empty;
        public string AgenteTelefono { get; set; } = string.Empty;
        public string AgenteFoto { get; set; } = string.Empty;

        // ===== Flags de oferta/visibilidad en la vista
        public bool SePuedeOfertar { get; set; } = true;
        public bool EsPropiedadDelAgenteActual { get; set; }
        public bool HayOfertaAprobada { get; set; }          // por si tu vista lo usa
        public bool TieneOfertaPendienteDelCliente { get; set; } // por si tu vista lo usa

        // ===== Chat y Ofertas (compatibilidad con tus entidades actuales)
        // Si ya estás usando VM específicos (ChatMensajeViewModel / OfertaViewModel), puedes cambiarlos.
        public List<MensajeChat> Mensajes { get; set; } = new();  // Entidad
        public List<Oferta> OfertasEntidad { get; set; } = new(); // Entidad

        // Alternativas usadas por otras vistas:
        public List<ChatMensajeViewModel> Chat { get; set; } = new();       // VM opcional
     

        // ===== Campos de formulario (si los usas)
        public string NuevoMensaje { get; set; } = string.Empty;
        public decimal? NuevaOferta { get; set; }
        public bool PuedeOfertar { get; set; } = true;

        // ===== Propiedad completa (si necesitas mostrar algo puntual)
        public Propiedad? Propiedad { get; set; }

        public List<ClienteChatResumenVM> ClientesChat { get; set; } = new();

   

        // ===== 🔵 NUEVO: Resumen de ofertas por cliente (lado agente) =====
        public List<OfertaResumenClienteVM> OfertasResumenAgente { get; set; } = new();


    }
}
