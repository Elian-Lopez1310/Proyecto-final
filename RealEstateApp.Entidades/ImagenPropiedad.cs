using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateApp.Entidades
{
    public class ImagenPropiedad
    {
        public int Id { get; set; }
        public string Url { get; set; }

        public int PropiedadId { get; set; }
        public Propiedad Propiedad { get; set; }

  
    }

}
