using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateApp.Entidades
{
    public class Oferta
    {
        public int Id { get; set; }
        public int PropiedadId { get; set; }
        public int ClienteId { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Estado { get; set; } = "Pendiente"; 
    }
}