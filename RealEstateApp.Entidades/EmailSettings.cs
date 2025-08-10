using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateApp.Helpers
{
    public class EmailSettings
    {
        public string ServidorSMTP { get; set; }
        public int Puerto { get; set; }
        public string CorreoRemitente { get; set; }
        public string Clave { get; set; }
        public bool Simular { get; set; }
    }
}