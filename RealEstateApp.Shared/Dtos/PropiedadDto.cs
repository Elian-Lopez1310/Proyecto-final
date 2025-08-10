using System.Collections.Generic;

namespace RealEstateApp.Shared.Dtos
{
    public class PropiedadDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public decimal Precio { get; set; }

        public string Tipo { get; set; } = string.Empty;          
        public string TipoVenta { get; set; } = string.Empty;      
        public bool Disponible { get; set; }

        public int Habitaciones { get; set; }
        public int Banos { get; set; }


        public int MetrosCuadrados { get; set; }
        public int Metros { get; set; }

        
        public int Area => Metros > 0 ? Metros : MetrosCuadrados;

        public string FotoPrincipal { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;

        public List<string> Imagenes { get; set; } = new();      
        public string ImagenUrl { get; set; } = string.Empty;     
        public List<PropiedadDto> Resultados { get; set; } = new();

        public int AgenteId { get; set; }
        public string AgenteNombre { get; set; } = string.Empty;  
        public string AgenteFoto { get; set; } = string.Empty;     
        public bool EsFavorita { get; set; }

        public string ImagenPrincipal { get; set; } = string.Empty; 
    }
}
