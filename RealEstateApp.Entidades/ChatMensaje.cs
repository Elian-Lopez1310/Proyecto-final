using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateApp.Entidades
{
  
    public class ChatMensaje
    {
        public int Id { get; set; }
        public int PropiedadId { get; set; }

        public int EmisorId { get; set; }          
        public int? ReceptorId { get; set; }       
        public string Texto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public bool EsDelAgente { get; set; }     
        public bool Leido { get; set; }            
    }
}