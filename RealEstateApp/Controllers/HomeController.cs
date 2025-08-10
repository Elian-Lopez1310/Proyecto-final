using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RealEstateApp.Negocio.Servicios;
using RealEstateApp.Shared.Dtos;
using RealEstateApp.Web.ViewModels;

namespace RealEstateApp.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly PropiedadServicio _servicio;

        public HomeController(PropiedadServicio servicio)
        {
            _servicio = servicio;
        }


        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] FiltroPropiedadViewModel filtros)
        {
            filtros ??= new FiltroPropiedadViewModel();

            
            ViewBag.EsCliente = HttpContext.Session.GetString("TipoUsuario") == "Cliente";
            ViewBag.NombreUsuario = HttpContext.Session.GetString("NombreUsuario");

        
            var code = filtros.Codigo?.Trim();
            if (!string.IsNullOrEmpty(code))
            {
                var p = await _servicio.ObtenerPorCodigoAsync(code);
                filtros.Resultados = p == null ? new List<PropiedadDto>() : new List<PropiedadDto> { Map(p) };
                return View(filtros);
            }

          
            var encontrados = await _servicio.BuscarConFiltrosAsync(
                filtros.Codigo,
                filtros.Tipo,
                filtros.PrecioMin,
                filtros.PrecioMax,
                filtros.Habitaciones,
                filtros.Banos
            );

            filtros.Resultados = encontrados.Select(Map).ToList();
            return View(filtros);
        }

   
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> FiltrarAjax([FromQuery] FiltroPropiedadViewModel filtros)
        {
            filtros ??= new FiltroPropiedadViewModel();

            var encontrados = await _servicio.BuscarConFiltrosAsync(
                filtros.Codigo,
                filtros.Tipo,
                filtros.PrecioMin,
                filtros.PrecioMax,
                filtros.Habitaciones,
                filtros.Banos
            );

            filtros.Resultados = encontrados.Select(Map).ToList();

          
            return PartialView("~/Views/Home/_GridPropiedades.cshtml", filtros);
        }

      

        private static PropiedadDto Map(RealEstateApp.Entidades.Propiedad p)
        {
          
            string imagen =
                !string.IsNullOrWhiteSpace(p.ImagenUrl) ? p.ImagenUrl :
                !string.IsNullOrWhiteSpace(p.FotoPrincipal) ? p.FotoPrincipal :
                                                               "/images/default.png";

         
            int metros = (p.Metros ?? 0) > 0 ? (p.Metros ?? 0) : (p.MetrosCuadrados ?? 0);

            return new PropiedadDto
            {
                Id = p.Id,
                Codigo = p.Codigo ?? "N/A",
                Tipo = p.Tipo ?? "Sin tipo",
                Ubicacion = p.Ubicacion ?? "Ubicación no definida",
                TipoVenta = p.TipoVenta ?? "N/A",
                Precio = p.Precio ?? 0m,
                Habitaciones = p.Habitaciones ?? 0,
                Banos = p.Banos ?? 0,
                Metros = metros,
                MetrosCuadrados = p.MetrosCuadrados ?? 0,
                ImagenUrl = imagen,
                FotoPrincipal = p.FotoPrincipal
            };
        }
    }
}
