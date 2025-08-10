using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateApp.Datos;
using RealEstateApp.Negocio.Servicios;
using RealEstateApp.Shared.Dtos;
using RealEstateApp.Web.ViewModels;
using System.Linq; 
using System.Security.Claims;

namespace RealEstateApp.Web.Controllers
{
    public class AgenteController : Controller
    {
        private readonly IAgenteServicio _agenteServicio;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AgenteController(
            IAgenteServicio agenteServicio,
            AppDbContext context,
            IWebHostEnvironment env)
        {
            _agenteServicio = agenteServicio;
            _context = context;
            _env = env;
        }

     

        private int GetUsuarioIdActual()
        {
         
            var s = HttpContext.Session.GetInt32("UsuarioId");
            if (s.HasValue && s.Value > 0) return s.Value;

           
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("UserId")?.Value
                     ?? User.FindFirst("Id")?.Value;

            return int.TryParse(idStr, out var id) ? id : 0;
        }

  
        private async Task<bool> EsPropiedadDelAgenteAsync(int propiedadId, int usuarioAgenteId)
        {
            var data = await _context.Propiedades
                .Where(p => p.Id == propiedadId)
                .Select(p => new { p.Id, p.AgenteId })
                .FirstOrDefaultAsync();

            if (data == null || data.AgenteId == null) return false;

            var correoAgente = await _context.Agentes
                .Where(a => a.Id == data.AgenteId.Value)
                .Select(a => a.Correo)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(correoAgente)) return false;

            var usuarioIdDelAgente = await _context.Usuarios
                .Where(u => u.Correo == correoAgente && u.TipoUsuario == "Agente" && u.Activo)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            return usuarioIdDelAgente == usuarioAgenteId;
        }

 
        public async Task<IActionResult> Index(string? nombre)
        {
            var agentes = await _agenteServicio.ObtenerAgentesActivosAsync();

            foreach (var a in agentes)
            {
                var props = await _agenteServicio.ObtenerPropiedadesPorAgenteAsync(a.Id);
                var primera = props?.FirstOrDefault(p => p.Disponible);
                a.CodigoPrimeraPropiedad = primera?.Codigo;
            }

            if (!string.IsNullOrWhiteSpace(nombre))
                agentes = agentes
                    .Where(a => a.NombreCompleto.Contains(nombre, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            return View(agentes.OrderBy(a => a.NombreCompleto).ToList());
        }

   
        public async Task<IActionResult> PropiedadesDelAgente(int agenteId)
        {
            var propiedades = await _agenteServicio.ObtenerPropiedadesPorAgenteAsync(agenteId);
            var agente = await _agenteServicio.ObtenerPorIdAsync(agenteId);
            if (agente == null) return NotFound();

            var codigoPrimera = propiedades?.FirstOrDefault()?.Codigo;

            var agenteDto = new AgenteDto
            {
                Id = agente.Id,
                NombreCompleto = !string.IsNullOrWhiteSpace(agente.NombreCompleto)
                    ? agente.NombreCompleto
                    : $"{agente.Nombre} {agente.Apellido}".Trim(),
                FotoUrl = agente.FotoUrl ?? string.Empty,
                CodigoPrimeraPropiedad = codigoPrimera,
                CodigoPropiedadDestacada = codigoPrimera
            };

            var vm = new PropiedadesDelAgenteViewModel
            {
                NombreAgente = string.IsNullOrWhiteSpace(agenteDto.NombreCompleto) ? "Agente" : agenteDto.NombreCompleto,
                Agente = agenteDto,
                PropiedadesAgente = propiedades ?? new List<PropiedadDto>(),
            
                PropiedadesDisponibles = await ObtenerPropiedadesDisponiblesAsync()
            };

            return View("PropiedadesDelAgente", vm);
        }

   
        [HttpGet]
        public IActionResult Home() => RedirectToAction(nameof(HomeAgente));

    
        [HttpGet]
        public async Task<IActionResult> HomeAgente()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Cuenta");

      
            var agenteIdClaim = User.FindFirst("AgenteId")?.Value;
            int? agenteId = null;
            if (int.TryParse(agenteIdClaim, out var tmp)) agenteId = tmp;

            var usuarioId = GetUsuarioIdActual();

            if (agenteId == null)
            {
                var correo = await _context.Usuarios
                    .AsNoTracking()
                    .Where(u => u.Id == usuarioId)
                    .Select(u => u.Correo)
                    .FirstOrDefaultAsync();

                agenteId = await _context.Agentes
                    .AsNoTracking()
                    .Where(a => a.Correo == correo)
                    .Select(a => a.Id)
                    .FirstOrDefaultAsync();
            }

            if (agenteId == null || agenteId.Value <= 0)
                return RedirectToAction("Login", "Cuenta");

            var propiedades = await _agenteServicio.ObtenerPropiedadesPorAgenteAsync(agenteId.Value);
            var agente = await _agenteServicio.ObtenerPorIdAsync(agenteId.Value);
            if (agente == null) return RedirectToAction("Login", "Cuenta");

            var codigoPrimera = propiedades?.FirstOrDefault()?.Codigo;
            agente.CodigoPrimeraPropiedad = codigoPrimera;
            agente.CodigoPropiedadDestacada = codigoPrimera;

           
            var mensajesRecientes = await _context.ChatMensajes
                .AsNoTracking()
                .Where(m => !m.EsDelAgente && m.ReceptorId == usuarioId)
                .Join(_context.Propiedades.AsNoTracking(),
                      m => m.PropiedadId,
                      p => p.Id,
                      (m, p) => new { m, p })
                .Where(x => x.p.AgenteId == agenteId.Value)
                .Join(_context.Usuarios.AsNoTracking(),
                      mp => mp.m.EmisorId,  
                      u => u.Id,
                      (mp, u) => new MensajeResumenVM
                      {
                          PropiedadId = mp.p.Id,
                          CodigoPropiedad = mp.p.Codigo ?? "",
                          ClienteId = u.Id,
                          ClienteNombre = $"{(u.Nombre ?? "").Trim()} {(u.Apellido ?? "").Trim()}".Trim(),
                          Texto = mp.m.Texto ?? "",
                          Fecha = mp.m.Fecha,
                          Leido = mp.m.Leido
                      })
                .OrderByDescending(x => x.Fecha)
                .Take(12)
                .ToListAsync();

            var vm = new PropiedadesDelAgenteViewModel
            {
                NombreAgente = string.IsNullOrWhiteSpace(agente.NombreCompleto) ? "Agente" : agente.NombreCompleto,
                Agente = agente,
                PropiedadesAgente = propiedades ?? new List<PropiedadDto>(),
                PropiedadesDisponibles = await ObtenerPropiedadesDisponiblesAsync(),
                MensajesRecientes = mensajesRecientes
            };

            return View("HomeAgente", vm);
        }


     
        public async Task<IActionResult> Perfil()
        {
            var idUsuario = HttpContext.Session.GetInt32("UsuarioId");

            var usuario = await _context.Usuarios
                .Where(u => u.Id == idUsuario && u.TipoUsuario == "Agente")
                .Select(u => new AgentePerfilDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Telefono = u.Telefono,
                    FotoUrl = u.FotoUrl
                }).FirstOrDefaultAsync();

            if (usuario == null)
                return RedirectToAction("Login", "Cuenta");

            return View(usuario);
        }

      
        [HttpGet]
        public async Task<IActionResult> EditarPerfil()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return RedirectToAction("Login", "Cuenta");

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId && u.TipoUsuario == "Agente");

            if (usuario == null)
                return NotFound();

            var model = new AgentePerfilDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Telefono = usuario.Telefono,
                FotoUrl = usuario.FotoUrl
            };

            return View("EditarPerfil", model);
        }

        // ✅ POST: Editar perfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPerfil([FromForm] AgentePerfilDto model, IFormFile FotoNueva)
        {
            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "❌ Errores: " + errores });
            }

            try
            {
                var agente = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Id == model.Id && u.TipoUsuario == "Agente");

                if (agente == null)
                {
                    return Json(new { success = false, message = "❌ Agente no encontrado." });
                }

                agente.Nombre = model.Nombre;
                agente.Apellido = model.Apellido;
                agente.Telefono = model.Telefono;

                if (FotoNueva != null && FotoNueva.Length > 0)
                {
                    var carpeta = Path.Combine(_env.WebRootPath, "images/agentes");
                    if (!Directory.Exists(carpeta))
                        Directory.CreateDirectory(carpeta);

                    var nombreArchivo = Guid.NewGuid() + Path.GetExtension(FotoNueva.FileName);
                    var ruta = Path.Combine(carpeta, nombreArchivo);

                    using (var stream = new FileStream(ruta, FileMode.Create))
                    {
                        await FotoNueva.CopyToAsync(stream);
                    }

                    agente.FotoUrl = "/images/agentes/" + nombreArchivo;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "✅ Perfil actualizado correctamente.",
                    redirectUrl = Url.Action("Perfil", "Agente")
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "❌ Error inesperado al guardar: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarMensajeAgente(int propiedadId, int clienteId, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Json(new { success = false, message = "Mensaje vacío." });

            var agenteUserId = GetUsuarioIdActual();
            if (agenteUserId == 0) return Json(new { success = false, message = "No autenticado." });

            if (!await EsPropiedadDelAgenteAsync(propiedadId, agenteUserId))
                return Json(new { success = false, message = "Propiedad no pertenece al agente." });

            _context.ChatMensajes.Add(new Entidades.ChatMensaje
            {
                PropiedadId = propiedadId,
                EmisorId = agenteUserId,
                ReceptorId = clienteId,
                Texto = texto.Trim(),
                Fecha = DateTime.UtcNow,
                EsDelAgente = true,
                Leido = false
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

      
   
        public async Task<IActionResult> OfertasDelCliente(int propiedadId, int clienteId)
        {
            var agenteUserId = GetUsuarioIdActual();
            if (agenteUserId == 0) return RedirectToAction("Login", "Cuenta");

            if (!await EsPropiedadDelAgenteAsync(propiedadId, agenteUserId))
                return Forbid();

            var ofertas = await _context.Ofertas
                .Where(o => o.PropiedadId == propiedadId && o.ClienteId == clienteId)
                .OrderByDescending(o => o.Fecha)
                .Select(o => new OfertaViewModel
                {
                    Fecha = o.Fecha,
                    Monto = o.Monto,
                    Estado = o.Estado
                })
                .ToListAsync();

            ViewBag.PropiedadId = propiedadId;
            ViewBag.ClienteId = clienteId;
            return View("OfertasDelCliente", ofertas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResponderOferta(int ofertaId, bool aceptar)
        {
            var agenteUserId = GetUsuarioIdActual();
            if (agenteUserId == 0) return Json(new { success = false, message = "No autenticado." });

            var oferta = await _context.Ofertas.FirstOrDefaultAsync(o => o.Id == ofertaId);
            if (oferta == null) return Json(new { success = false, message = "Oferta no encontrada." });

            if (!await EsPropiedadDelAgenteAsync(oferta.PropiedadId, agenteUserId))
                return Json(new { success = false, message = "Propiedad no pertenece al agente." });

            if (aceptar)
            {
         
                oferta.Estado = "Aceptada";

            
                var otras = await _context.Ofertas
                    .Where(o => o.PropiedadId == oferta.PropiedadId && o.Id != oferta.Id && o.Estado == "Pendiente")
                    .ToListAsync();

                foreach (var o in otras) o.Estado = "Rechazada";

          
                var prop = await _context.Propiedades.FirstOrDefaultAsync(p => p.Id == oferta.PropiedadId);
                if (prop != null)
                {
                    prop.Disponible = false;
                    prop.TipoVenta = "Vendida";
                }
            }
            else
            {
                oferta.Estado = "Rechazada";
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

    
        private async Task<List<PropiedadDto>> ObtenerPropiedadesDisponiblesAsync()
        {
            return await _context.Propiedades
                .Where(p => p.Disponible)
                .OrderBy(p => p.Codigo)
                .Select(p => new PropiedadDto
                {
                    Codigo = p.Codigo,
                    Tipo = p.Tipo ?? string.Empty,
                    Ubicacion = p.Ubicacion ?? string.Empty,
                    TipoVenta = p.TipoVenta ?? string.Empty,

                    Precio = p.Precio ?? 0m,
                    Habitaciones = p.Habitaciones ?? 0,
                    Banos = p.Banos ?? 0,
                    Metros = p.Metros ?? 0,
                    MetrosCuadrados = p.MetrosCuadrados ?? 0,

                    FotoPrincipal = p.FotoPrincipal ?? string.Empty,
                    Disponible = p.Disponible
                })
                .ToListAsync();
        }

        public async Task<IActionResult> ChatConCliente(int propiedadId, int clienteId)
        {
            var agenteUserId = GetUsuarioIdActual();
            if (agenteUserId == 0) return RedirectToAction("Login", "Cuenta");

            if (!await EsPropiedadDelAgenteAsync(propiedadId, agenteUserId))
                return Forbid();

            var mensajes = await _context.ChatMensajes
                .Where(m => m.PropiedadId == propiedadId &&
                       ((m.EmisorId == clienteId && m.ReceptorId == agenteUserId) ||
                        (m.EmisorId == agenteUserId && m.ReceptorId == clienteId)))
                .OrderBy(m => m.Fecha)
                .Select(m => new ChatMensajeViewModel
                {
                    Texto = m.Texto,
                    EsDelAgente = m.EsDelAgente,
                    Fecha = m.Fecha
                })
                .ToListAsync();

            ViewBag.PropiedadId = propiedadId;
            ViewBag.ClienteId = clienteId;

         
            return View("ChatCliente", mensajes);
        }

        [HttpGet]
        public async Task<IActionResult> ListaClientesChat(int propiedadId)
        {
            var agenteUserId = GetUsuarioIdActual();
            if (agenteUserId == 0) return RedirectToAction("Login", "Cuenta");

            if (!await EsPropiedadDelAgenteAsync(propiedadId, agenteUserId))
                return Forbid();

            ViewBag.PropiedadId = propiedadId;
            ViewBag.CodigoPropiedad = await _context.Propiedades.AsNoTracking()
                .Where(p => p.Id == propiedadId)
                .Select(p => p.Codigo)
                .FirstOrDefaultAsync();

     
            var clientesBase = await _context.Usuarios.AsNoTracking()
                .Where(u => u.TipoUsuario == "Cliente" && u.Activo)
                .Select(u => new { u.Id, u.Nombre, u.Apellido, u.Correo, u.Telefono })
                .ToListAsync();

     
            var resumenChat = await _context.ChatMensajes.AsNoTracking()
                .Where(m => m.PropiedadId == propiedadId &&
                            (m.EmisorId == agenteUserId || m.ReceptorId == agenteUserId))
                .Select(m => new
                {
                    ClienteId = (int?)((m.EmisorId == agenteUserId) ? m.ReceptorId : m.EmisorId),
                    m.Texto,
                    m.Fecha,
                    m.EsDelAgente,
                    m.Leido
                })
                .Where(x => x.ClienteId.HasValue)
                .GroupBy(x => x.ClienteId.Value)
                .Select(g => new
                {
                    ClienteId = g.Key,
                    UltimaFecha = g.Max(y => y.Fecha),
                    UltimoTexto = g.OrderByDescending(y => y.Fecha).Select(y => y.Texto).FirstOrDefault(),
                    NoLeidos = g.Count(y => !y.EsDelAgente && !y.Leido)
                })
                .ToListAsync();

            var dic = resumenChat.ToDictionary(x => x.ClienteId, x => x);

            var lista = clientesBase
                .Select(u =>
                {
                    dic.TryGetValue(u.Id, out var r);
                    return new ClienteListadoVM
                    {
                        Id = u.Id,
                        Nombre = $"{(u.Nombre ?? "").Trim()} {(u.Apellido ?? "").Trim()}".Trim(),
                        Correo = u.Correo ?? "",
                        Telefono = u.Telefono ?? "",
                        TieneChat = r != null,
                        NoLeidos = r?.NoLeidos ?? 0,
                        UltimaFecha = r?.UltimaFecha,
                        UltimoTexto = r?.UltimoTexto ?? ""
                    };
                })
                .OrderByDescending(x => x.TieneChat)
                .ThenBy(x => x.Nombre)
                .ToList();

            return View("ListaClientesChat", lista);
        }

      

    }


    public class SimpleUsuarioVm
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }


}
