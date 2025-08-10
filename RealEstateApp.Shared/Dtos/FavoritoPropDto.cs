using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateApp.Shared.Dtos
{
    public class FavoritoPropDto
    {
        public string Codigo { get; set; }
        public string Titulo { get; set; }
        public string Tipo { get; set; }
        public string Ubicacion { get; set; }
        public string TipoVenta { get; set; }
        public decimal Precio { get; set; }
        public int Habitaciones { get; set; }
        public int Banos { get; set; }
        public int Metros { get; set; }
        public int MetrosCuadrados { get; set; }
        public DateTime Fecha { get; set; }
        public string ImagenUrlFinal { get; set; }
    }
}