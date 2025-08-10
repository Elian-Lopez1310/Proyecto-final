using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateApp.Entidades
{
    public class Favorito
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int PropiedadId { get; set; }
        public DateTime Fecha { get; set; }  
        public Propiedad Propiedad { get; set; }

    }
}