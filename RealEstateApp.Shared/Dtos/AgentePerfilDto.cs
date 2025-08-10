using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace RealEstateApp.Shared.Dtos
{
    public class AgentePerfilDto
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        public string Apellido { get; set; }

        [Required]
        public string Telefono { get; set; }

        public string? FotoUrl { get; set; }
    }
}