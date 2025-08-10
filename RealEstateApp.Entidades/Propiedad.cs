using System;
using System.Collections.Generic;

namespace RealEstateApp.Entidades
{
    public class Propiedad
    {
        public int Id { get; set; }


        public string Codigo { get; set; } = null!;

        public string? Tipo { get; set; }
        public string? TipoVenta { get; set; }
        public string? Ubicacion { get; set; }
        public string? Direccion { get; set; }

 
        public decimal? Precio { get; set; }
        public int? Metros { get; set; }
        public int? MetrosCuadrados { get; set; }
        public int? Habitaciones { get; set; }
        public int? Banos { get; set; }

        public bool EsPublica { get; set; } = true;

   
        public bool Disponible { get; set; } = true;
        public bool Activo { get; set; } = true;
        public string? Descripcion { get; set; }
        public string? ImagenUrl { get; set; }
        public string FotoPrincipal { get; set; } = string.Empty;


        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        
        public int? AgenteId { get; set; }
        public string? MejorasTexto { get; set; }

        public DateTime FechaPublicacion { get; set; }

        public Agente? Agente { get; set; }
        public bool EsFavorita { get; set; } 
        public List<Mejora>? Mejoras { get; set; }
        public List<ImagenPropiedad>? Imagenes { get; set; }
    }
}
