using System;

namespace RealEstateApp.Shared.Dtos
{
    public class AgenteDto
    {
        public int Id { get; set; }


        public string NombreCompleto { get; set; } = string.Empty;


        public string FotoUrl { get; set; } = string.Empty;

     
        public string? CodigoPropiedadDestacada { get; set; }

   
        public string? CodigoPrimeraPropiedad { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
    }
}
