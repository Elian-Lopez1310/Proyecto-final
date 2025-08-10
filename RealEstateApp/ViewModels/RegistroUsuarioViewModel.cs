using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RealEstateApp.ViewModels
{
    public class RegistroUsuarioViewModel
    {
        [Required] public string Nombre { get; set; }
        [Required] public string Apellido { get; set; }
        [Required] public string Telefono { get; set; }
        public IFormFile Foto { get; set; }
        [Required] public string NombreUsuario { get; set; }
        [Required, EmailAddress] public string Correo { get; set; }
        [Required] public string Contrasena { get; set; }
        [Compare("Contrasena")] public string ConfirmarClave { get; set; }
        [Required] public string TipoUsuario { get; set; }
    }
}