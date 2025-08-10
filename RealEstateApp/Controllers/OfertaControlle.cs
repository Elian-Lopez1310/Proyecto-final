using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using RealEstateApp.Datos;
using RealEstateApp.Entidades; 
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateApp.Web.Controllers
{
    [Route("[controller]")]
    public class OfertaController : Controller
    {
        private readonly AppDbContext _context;
        public OfertaController(AppDbContext context) { _context = context; }

        
        [HttpPost("Crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear([FromForm] int PropiedadId, [FromForm] decimal Monto)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UsuarioId");
                var tipoUsuario = HttpContext.Session.GetString("TipoUsuario");

                if (userId is null || userId.Value <= 0)
                    return Json(new { success = false, message = "No autenticado." });

              
                if (!string.Equals(tipoUsuario, "Cliente", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Solo los clientes pueden ofertar." });

                if (Monto <= 0)
                    return Json(new { success = false, message = "Monto inválido." });

                var prop = await _context.Propiedades
                    .FirstOrDefaultAsync(p => p.Id == PropiedadId);

                if (prop == null)
                    return Json(new { success = false, message = "Propiedad no encontrada." });

                if (!prop.Disponible || string.Equals(prop.TipoVenta, "Vendida", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Propiedad no disponible para ofertar." });

            
                var oferta = new Oferta
                {
                    PropiedadId = PropiedadId,
                   
                    ClienteId = userId.Value,
                    Monto = Monto,
                    Estado = "Pendiente",
                    Fecha = DateTime.UtcNow
                };

                _context.Ofertas.Add(oferta);
                await _context.SaveChangesAsync();

                return Json(new { success = true, id = oferta.Id });
            }
            catch (Exception ex)
            {
         
                return Json(new { success = false, message = "Error del servidor al crear la oferta.", detail = ex.Message });
            }
        }

 
        [HttpPost("Responder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Responder([FromForm] int ofertaId, [FromForm] bool aceptar, [FromForm] int? propiedadId)
        {
            try
            {
                var agenteUserId = HttpContext.Session.GetInt32("UsuarioId");
                var tipoUsuario = HttpContext.Session.GetString("TipoUsuario");

                if (agenteUserId is null || agenteUserId.Value <= 0)
                    return Json(new { success = false, message = "No autenticado." });

           
                if (!string.Equals(tipoUsuario, "Agente", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Solo agentes pueden responder ofertas." });

                var oferta = await _context.Ofertas.FirstOrDefaultAsync(o => o.Id == ofertaId);
                if (oferta == null)
                    return Json(new { success = false, message = "Oferta no encontrada." });

                var propId = propiedadId ?? oferta.PropiedadId;
                var prop = await _context.Propiedades.FirstOrDefaultAsync(p => p.Id == propId);
                if (prop == null)
                    return Json(new { success = false, message = "Propiedad no encontrada." });

          
                int? usuarioAgenteProp = null;
                if (prop.AgenteId != null)
                {
                    var correoAgente = await _context.Agentes
                        .Where(a => a.Id == prop.AgenteId.Value)
                        .Select(a => a.Correo)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrWhiteSpace(correoAgente))
                    {
                        usuarioAgenteProp = await _context.Usuarios
                            .Where(u => u.Correo == correoAgente && u.TipoUsuario == "Agente" && u.Activo)
                            .Select(u => u.Id)
                            .FirstOrDefaultAsync();
                    }
                }

                if (usuarioAgenteProp != agenteUserId.Value)
                    return Json(new { success = false, message = "No autorizado." });

     
                if (aceptar)
                {
                    oferta.Estado = "Aceptada";

                 
                    var otrasPendientes = await _context.Ofertas
                        .Where(o => o.PropiedadId == prop.Id && o.Id != oferta.Id && o.Estado == "Pendiente")
                        .ToListAsync();

                    foreach (var o in otrasPendientes)
                        o.Estado = "Rechazada";

                  
                    prop.Disponible = false;
                    prop.TipoVenta = "Vendida";
                }
                else
                {
                    oferta.Estado = "Rechazada";
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
               
                return Json(new { success = false, message = "Error del servidor al actualizar la oferta.", detail = ex.Message });
            }
        }
    }
}
