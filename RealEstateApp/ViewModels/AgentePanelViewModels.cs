using System;
using System.Collections.Generic;

namespace RealEstateApp.Web.ViewModels
{
    // =========================================================
    // CHAT MENSAJES (lo que usan tus vistas: Texto, EsDelAgente, Fecha)
    // =========================================================
    public class ChatMensajeViewModel
    {
        public string Texto { get; set; } = string.Empty;   // contenido del mensaje
        public bool EsDelAgente { get; set; }               // para pintar a la derecha/izquierda
        public DateTime Fecha { get; set; }                 // fecha/hora a mostrar
    }

    // =========================================================
    // OFERTA (lo que usan tus vistas: Fecha, Monto, Estado)
    // + ALIAS de compatibilidad: OfertaId/Id, Ofertald, Ofertal, PuedeResponder
    // =========================================================
    public class OfertaViewModel
    {
        private int _ofertaId;
        public int OfertaId { get => _ofertaId; set => _ofertaId = value; }
        public int Id { get => _ofertaId; set => _ofertaId = value; }

        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public string Estado { get; set; } = "Pendiente";

        // Aliases (solo si de verdad los necesitas en vistas viejas)
        public decimal Ofertald { get => Monto; set => Monto = value; }
        public decimal Ofertal { get => Monto; set => Monto = value; }

        // 👇 calculado para mostrar Aceptar/Rechazar:
        public bool PuedeResponder =>
            string.Equals(Estado, "Pendiente", StringComparison.OrdinalIgnoreCase);
    }


    // =========================================================
    // Item simple de usuario (usado en listados)
    // =========================================================
    public class SimpleUsuarioVM
    {
        public int ClienteId { get; set; }
        public string NombreCliente { get; set; } = string.Empty;

        public int CantOfertas { get; set; }        // para listado de ofertas por cliente
        public DateTime? Fecha { get; set; }        // último evento (msg/oferta)
        public string? UltimoMensaje { get; set; }  // último mensaje (opcional)
        public int NoLeidos { get; set; }           // no leídos (opcional)
    }

    // =========================================================
    // LISTA DE CHATS (vista: Chats.cshtml)
    // =========================================================
    public class AgenteChatListVM
    {
        // Soportamos ambos por compatibilidad
        public int PropiedadId { get; set; }
        public string CodigoPropiedad { get; set; } = string.Empty;
        public string TituloPropiedad { get; set; } = string.Empty;

        public List<ClienteChatItemVM> Clientes { get; set; } = new();
    }

    public class ClienteChatItemVM
    {
        private string _clienteNombre = string.Empty;
        private DateTime? _fechaUltimo;

        public int ClienteId { get; set; }

        // La vista a veces usa ClienteNombre y otras NombreCliente → soportamos ambos.
        public string ClienteNombre
        {
            get => _clienteNombre;
            set => _clienteNombre = value ?? string.Empty;
        }

        // Alias requerido por la vista (NombreCliente)
        public string NombreCliente
        {
            get => _clienteNombre;
            set => _clienteNombre = value ?? string.Empty;
        }

        public string UltimoMensaje { get; set; } = string.Empty;

        // La vista usa 'FechaUlt' → alias para 'FechaUltimo'
        public DateTime? FechaUltimo
        {
            get => _fechaUltimo;
            set => _fechaUltimo = value;
        }

        public DateTime? FechaUlt
        {
            get => _fechaUltimo;
            set => _fechaUltimo = value;
        }

        public int NoLeidos { get; set; }
    }

    // =========================================================
    // DETALLE DE CHAT (vista: ChatCliente.cshtml)
    // =========================================================
    public class AgenteChatDetalleVM
    {
        public int PropiedadId { get; set; }
        public string CodigoPropiedad { get; set; } = string.Empty;

        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;   // la vista lo pide así

        public List<ChatMensajeViewModel> Mensajes { get; set; } = new();

        // Si tu form de respuesta rápida lo usa
        public string Texto { get; set; } = string.Empty;
    }

    // =========================================================
    // LISTA DE OFERTAS POR CLIENTE (vista: Ofertas.cshtml)
    // =========================================================
    public class AgenteOfertasListaVM
    {
        public int PropiedadId { get; set; }
        public string CodigoPropiedad { get; set; } = string.Empty;
        public string TituloPropiedad { get; set; } = string.Empty;

        public List<ClienteOfertaResumenVM> Clientes { get; set; } = new();
    }

    public class ClienteOfertaResumenVM
    {
        private string _clienteNombre = string.Empty;
        private DateTime? _ultFecha;

        public int ClienteId { get; set; }

        // La vista usa 'NombreCliente' y/o 'ClienteNombre' → soportamos ambos
        public string ClienteNombre
        {
            get => _clienteNombre;
            set => _clienteNombre = value ?? string.Empty;
        }

        public string NombreCliente
        {
            get => _clienteNombre;
            set => _clienteNombre = value ?? string.Empty;
        }

        public int CantOfertas { get; set; }
        public decimal? MontoMaximo { get; set; }

        // La vista usa 'UltFecha' y también 'UltimaFecha' en algunos ejemplos → alias
        public DateTime? UltimaFecha
        {
            get => _ultFecha;
            set => _ultFecha = value;
        }

        public DateTime? UltFecha
        {
            get => _ultFecha;
            set => _ultFecha = value;
        }

        // La vista usa 'UltEstado'
        public string UltEstado { get; set; } = string.Empty;

        // Para marcar si hay alguna pendiente
        public bool TienePendiente { get; set; }
    }

    // =========================================================
    // DETALLE DE OFERTAS DE UN CLIENTE (vista: OfertasCliente.cshtml)
    // =========================================================
    public class AgenteOfertasClienteVM
    {
        public int PropiedadId { get; set; }
        public string CodigoPropiedad { get; set; } = string.Empty;

        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;

        public List<OfertaViewModel> Ofertas { get; set; } = new();

        // Para habilitar/ocultar botones en la UI
        public bool PuedeResponder { get; set; }
        public bool HayAceptada { get; set; }
        public bool HayPendiente { get; set; }

        public class ClienteChatResumenVM
        {
            public int ClienteId { get; set; }
            public string Nombre { get; set; } = "";
            public string UltimoTexto { get; set; } = "";
            public DateTime UltimaFecha { get; set; }
            public int NoLeidos { get; set; }
        }
     
    }

}


