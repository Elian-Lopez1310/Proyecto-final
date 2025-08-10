using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateApp.Entidades
{
    public class Agente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        public string Apellido { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public string FotoUrl { get; set; }
        public bool? Activo { get; set; }
        public string Foto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string NombreCompleto => $"{Nombre} {Apellido}";

        public List<Propiedad> Propiedades { get; set; }
    }

}