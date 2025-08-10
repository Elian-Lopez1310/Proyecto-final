using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateApp.ViewModels
{
    public class MensajeDto
    {
        public string Remitente { get; set; } 
        public string Texto { get; set; }
        public DateTime Fecha { get; set; }
    }
}
