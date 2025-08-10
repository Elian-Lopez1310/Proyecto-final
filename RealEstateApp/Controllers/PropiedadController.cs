using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateApp.Datos;
using RealEstateApp.Entidades;
using RealEstateApp.Negocio.Servicios;
using RealEstateApp.Shared.Dtos;
using RealEstateApp.ViewModels;
using RealEstateApp.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static RealEstateApp.Web.ViewModels.AgenteOfertasClienteVM;

namespace RealEstateApp.Web.Controllers
{
    public class PropiedadController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PropiedadServicio _propiedadServicio;

        public PropiedadController(AppDbContext context)
        {
            _context = context;
            _propiedadServicio = new PropiedadServicio(_context);
        }

       
        public async Task<IActionResult> Index(FiltroPropiedadViewModel filtro)
        {
            var propiedades = await _propiedadServicio.BuscarConFiltrosAsync(
                filtro.Codigo,
                filtro.Tipo,
                filtro.PrecioMin,
                filtro.PrecioMax,
                filtro.Habitaciones,
                filtro.Banos
            );

            ViewBag.FiltroActual = filtro;

            if (!propiedades.Any())
                TempData["Mensaje"] = "No se encontraron propiedades con los criterios ingresados.";

            return View(propiedades);
        }

    
        public async Task<IActionResult> PropiedadesDelAgente(int agenteId)
        {
            var propiedades = await _context.Propiedades
                .Where(p => p.AgenteId == agenteId)
                .ToListAsync();

            var agente = await _context.Agentes.FindAsync(agenteId);
            if (agente == null) return NotFound();

            var propiedadesDto = propiedades.Select(p => new PropiedadDto
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Tipo = p.Tipo,
                TipoVenta = p.TipoVenta,
                Ubicacion = p.Ubicacion,
                Precio = p.Precio ?? 0,
                Habitaciones = p.Habitaciones ?? 0,
                Banos = p.Banos ?? 0,
                Metros = p.Metros ?? 0,
                MetrosCuadrados = p.MetrosCuadrados ?? 0,
                ImagenUrl = string.IsNullOrWhiteSpace(p.ImagenUrl) ? p.FotoPrincipal : p.ImagenUrl,
                Disponible = p.Disponible
            }).ToList();

            var codigoPrimeraDisponible = propiedades
                .Where(p => p.Disponible)
                .OrderBy(p => p.Id)
                .Select(p => p.Codigo)
                .FirstOrDefault();

            var agenteDto = new AgenteDto
            {
                Id = agente.Id,
                NombreCompleto = $"{agente.Nombre} {agente.Apellido}".Trim(),
                FotoUrl = agente.FotoUrl ?? string.Empty,
                CodigoPropiedadDestacada = codigoPrimeraDisponible,
                CodigoPrimeraPropiedad = codigoPrimeraDisponible
            };

            var viewModel = new PropiedadesDelAgenteViewModel
            {
                NombreAgente = string.IsNullOrWhiteSpace(agenteDto.NombreCompleto) ? "Agente" : agenteDto.NombreCompleto,
                Agente = agenteDto,
                PropiedadesAgente = propiedadesDto,
                PropiedadesDisponibles = new System.Collections.Generic.List<PropiedadDto>()
            };

            return View(viewModel);
        }

      
        public async Task<IActionResult> VentasDelAgente(int agenteId)
        {
            var propiedadesEnVenta = await _context.Propiedades
                .Include(p => p.Agente)
                .Include(p => p.Imagenes)
                .Where(p => p.AgenteId == agenteId && p.Disponible && p.TipoVenta == "Venta")
                .ToListAsync();

            if (!propiedadesEnVenta.Any())
                TempData["Mensaje"] = "Este agente no tiene propiedades en venta actualmente.";

            ViewBag.NombreAgente = propiedadesEnVenta.FirstOrDefault()?.Agente?.NombreCompleto ?? "Agente desconocido";
            return View("Index", propiedadesEnVenta);
        }

     
        public async Task<IActionResult> Detalle(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return NotFound();

            var prop = await _context.Propiedades
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Codigo == codigo);

            if (prop == null) return NotFound();

           
            string agenteNombre = null, agenteFoto = null, agenteTelefono = null, agenteCorreo = null;
            int? agenteUsuarioId = null;

            if (prop.AgenteId != null)
            {
                var agente = await _context.Agentes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == prop.AgenteId);

                if (agente != null)
                {
                    agenteNombre = !string.IsNullOrWhiteSpace(agente.NombreCompleto)
                        ? agente.NombreCompleto
                        : $"{(agente.Nombre ?? "").Trim()} {(agente.Apellido ?? "").Trim()}".Trim();

                    agenteFoto = agente.FotoUrl;
                    agenteTelefono = agente.Telefono;
                    agenteCorreo = agente.Correo;

                    if (!string.IsNullOrWhiteSpace(agenteCorreo))
                    {
                        var uidAg = await _context.Usuarios
                            .AsNoTracking()
                            .Where(u => u.Correo == agenteCorreo && u.TipoUsuario == "Agente" && u.Activo)
                            .Select(u => u.Id)
                            .FirstOrDefaultAsync();

                        if (uidAg > 0) agenteUsuarioId = uidAg;
                    }
                }
            }

          
            var uid = HttpContext.Session.GetInt32("UsuarioId");
            bool esAgente = false;
            bool esPropiedadDelAgenteActual = false;

            if (uid.HasValue)
            {
                var tipo = await _context.Usuarios
                    .AsNoTracking()
                    .Where(u => u.Id == uid.Value)
                    .Select(u => u.TipoUsuario)
                    .FirstOrDefaultAsync();

                esAgente = string.Equals(tipo, "Agente", StringComparison.OrdinalIgnoreCase);
                if (agenteUsuarioId.HasValue && agenteUsuarioId.Value == uid.Value)
                    esPropiedadDelAgenteActual = true;
            }

            
            var imagenes = new List<string>();

           
            var chat = new List<ChatMensajeViewModel>();
            if (!esAgente && !esPropiedadDelAgenteActual && uid.HasValue && agenteUsuarioId.HasValue)
            {
                chat = await _context.ChatMensajes
                    .AsNoTracking()
                    .Where(m => m.PropiedadId == prop.Id &&
                           ((m.EmisorId == uid.Value && m.ReceptorId == agenteUsuarioId.Value) ||
                            (m.EmisorId == agenteUsuarioId.Value && m.ReceptorId == uid.Value)))
                    .OrderBy(m => m.Fecha)
                    .Select(m => new ChatMensajeViewModel
                    {
                        Texto = m.Texto,
                        EsDelAgente = m.EsDelAgente,
                        Fecha = m.Fecha
                    })
                    .ToListAsync();
            }

          
            var ofertas = new List<OfertaViewModel>();
            if (!esAgente && uid.HasValue)
            {
                ofertas = await _context.Ofertas
                    .AsNoTracking()
                    .Where(o => o.PropiedadId == prop.Id && o.ClienteId == uid.Value)
                    .OrderByDescending(o => o.Fecha)
                    .Select(o => new OfertaViewModel
                    {
                        OfertaId = o.Id,
                        Fecha = o.Fecha,
                        Monto = o.Monto,
                        Estado = o.Estado
                    })
                    .ToListAsync();
            }

           
            var clientesChat = new List<ClienteChatResumenVM>();
            if (esAgente && esPropiedadDelAgenteActual && agenteUsuarioId.HasValue)
            {
                clientesChat = await _context.ChatMensajes
                    .AsNoTracking()
                    .Where(m => m.PropiedadId == prop.Id &&
                                (m.EmisorId == agenteUsuarioId.Value || m.ReceptorId == agenteUsuarioId.Value))
                    .Select(m => new
                    {
                        ClienteId = (int?)((m.EmisorId == agenteUsuarioId.Value) ? m.ReceptorId : m.EmisorId),
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
                    .ToListAsync()
                    .ContinueWith(async t =>
                    {
                        var baseList = t.Result;
                        var ids = baseList.Select(b => b.ClienteId).ToList();

                        var nombres = await _context.Usuarios.AsNoTracking()
                            .Where(u => ids.Contains(u.Id))
                            .Select(u => new { u.Id, u.Nombre, u.Apellido })
                            .ToListAsync();

                        var dic = nombres.ToDictionary(u => u.Id,
                            u => $"{(u.Nombre ?? "").Trim()} {(u.Apellido ?? "").Trim()}".Trim());

                        return baseList
                            .Select(g => new ClienteChatResumenVM
                            {
                                ClienteId = g.ClienteId,
                                Nombre = dic.TryGetValue(g.ClienteId, out var n) ? n : $"Cliente {g.ClienteId}",
                                UltimoTexto = g.UltimoTexto ?? "",
                                UltimaFecha = g.UltimaFecha,
                                NoLeidos = g.NoLeidos
                            })
                            .OrderByDescending(x => x.UltimaFecha)
                            .ToList();
                    }).Unwrap();
            }

            
            var ofertasResumenAgente = new List<OfertaResumenClienteVM>();

            if (esAgente && esPropiedadDelAgenteActual)
            {
                
                var agregados = await _context.Ofertas
                    .AsNoTracking()
                    .Where(o => o.PropiedadId == prop.Id)
                    .GroupBy(o => o.ClienteId)
                    .Select(g => new
                    {
                        ClienteId = g.Key,
                        UltimaFecha = g.Max(o => o.Fecha),
                        Pendientes = g.Count(o => o.Estado == "Pendiente")
                    })
                    .ToListAsync();

            
                var ultimas = await _context.Ofertas
                    .AsNoTracking()
                    .Where(o => o.PropiedadId == prop.Id)
                    .GroupBy(o => o.ClienteId)
                    .Select(g => g
                        .OrderByDescending(x => x.Fecha)
                        .Select(x => new
                        {
                            x.ClienteId,
                            OfertaIdUltima = x.Id,
                            MontoUltima = x.Monto,
                            EstadoUltima = x.Estado,
                            FechaUltima = x.Fecha
                        })
                        .FirstOrDefault())
                    .ToListAsync();

              
                var ids = agregados.Select(a => a.ClienteId).Union(ultimas.Select(u => u.ClienteId)).ToList();

                var usuarios = await _context.Usuarios
                    .AsNoTracking()
                    .Where(u => ids.Contains(u.Id))
                    .Select(u => new { u.Id, u.Nombre, u.Apellido })
                    .ToListAsync();

                var nomDic = usuarios.ToDictionary(
                    u => u.Id,
                    u => ($"{(u.Nombre ?? "").Trim()} {(u.Apellido ?? "").Trim()}").Trim()
                );

          
                ofertasResumenAgente = agregados
                    .Join(ultimas, a => a.ClienteId, u => u.ClienteId, (a, u) => new { a, u })
                    .Select(z => new OfertaResumenClienteVM
                    {
                        ClienteId = z.a.ClienteId,
                        ClienteNombre = nomDic.TryGetValue(z.a.ClienteId, out var n) ? n : $"Cliente {z.a.ClienteId}",
                        UltimaFecha = z.a.UltimaFecha,
                        MontoUltima = z.u?.MontoUltima ?? 0m,
                        EstadoUltima = z.u?.EstadoUltima ?? "Pendiente",
                        PendientesCount = z.a.Pendientes,
                        OfertaIdUltima = z.u?.OfertaIdUltima ?? 0
                    })
                    .OrderByDescending(x => x.UltimaFecha)
                    .ToList();
            }


          
            var vm = new DetallePropiedadViewModel
            {
                PropiedadId = prop.Id,
                Codigo = prop.Codigo,
                Tipo = prop.Tipo ?? string.Empty,
                TipoVenta = prop.TipoVenta ?? string.Empty,
                Precio = prop.Precio ?? 0m,
                Habitaciones = prop.Habitaciones ?? 0,
                Banos = prop.Banos ?? 0,
                Metros = prop.Metros ?? 0,
                MetrosCuadrados = prop.MetrosCuadrados ?? 0,
                Descripcion = prop.Descripcion,
                ImagenPrincipal = prop.FotoPrincipal,
                Imagenes = imagenes,

                AgenteNombre = agenteNombre,
                AgenteFoto = agenteFoto,
                AgenteTelefono = agenteTelefono,
                AgenteCorreo = agenteCorreo,

                EsPropiedadDelAgenteActual = esPropiedadDelAgenteActual,
                Chat = chat,
                Ofertas = ofertas,
                SePuedeOfertar = (prop.Disponible && !esAgente && !esPropiedadDelAgenteActual
                                  && !string.Equals(prop.TipoVenta, "Vendida", StringComparison.OrdinalIgnoreCase)),

                ClientesChat = clientesChat,
                OfertasResumenAgente = ofertasResumenAgente  
            };

            ViewBag.EsAgente = esAgente;
            return View(vm);
        }


      
        [HttpGet]
        public IActionResult Publicar()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Cuenta");

            return View(); 
        }

     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publicar([FromForm] PropiedadCrearViewModel model)
        {
           
            string FV(string key)
            {
                if (Request?.Form == null) return string.Empty;

                var direct = Request.Form[key].ToString();
                if (!string.IsNullOrWhiteSpace(direct)) return direct.Trim();

                var kv = Request.Form.FirstOrDefault(p =>
                    p.Key.EndsWith("." + key, StringComparison.OrdinalIgnoreCase));
                return kv.Value.ToString().Trim();
            }

            decimal? ParseDec(string key, decimal? fallback = null)
            {
                var s = FV(key);
                if (string.IsNullOrWhiteSpace(s) && fallback.HasValue) return fallback;

                s = (s ?? "").Trim().Replace(" ", "").Replace(",", ".");
                return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                    ? v : (decimal?)null;
            }

            int? ParseInt(string key, int? fallback = null)
            {
                var s = FV(key);
                if (string.IsNullOrWhiteSpace(s) && fallback.HasValue) return fallback;
                return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
                    ? v : (int?)null;
            }

           
            var file = Request?.Form?.Files?["ImagenPrincipal"] ?? Request?.Form?.Files?.FirstOrDefault();
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "❌ Debes seleccionar una imagen principal." });

            var okTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
            if (!okTypes.Contains(file.ContentType?.ToLower()))
                return Json(new { success = false, message = "❌ Formato inválido. Usa JPG o PNG." });

            if (file.Length > 10 * 1024 * 1024)
                return Json(new { success = false, message = "❌ La imagen supera los 10 MB." });

            
            var tipo = !string.IsNullOrWhiteSpace(model.Tipo) ? model.Tipo.Trim() : FV("Tipo");
            var tipoVenta = !string.IsNullOrWhiteSpace(model.TipoVenta) ? model.TipoVenta.Trim() : FV("TipoVenta");
            var ubicacion = !string.IsNullOrWhiteSpace(model.Ubicacion) ? model.Ubicacion.Trim() : FV("Ubicacion");

            var precio = ParseDec("Precio", model.Precio);
            var habitaciones = ParseInt("Habitaciones", model.Habitaciones);
            var banos = ParseInt("Banos", model.Banos);

            var metros = ParseDec("Metros", model.Metros);
            var metrosCuadrados = ParseDec("MetrosCuadrados", model.MetrosCuadrados);

            
            string why = null;
            if (string.IsNullOrWhiteSpace(tipo)) why = "Tipo es obligatorio.";
            else if (string.IsNullOrWhiteSpace(tipoVenta)) why = "Tipo de venta es obligatorio.";
            else if (string.IsNullOrWhiteSpace(ubicacion)) why = "Ubicación es obligatoria.";
            else if (precio is null || precio <= 0) why = "Precio debe ser mayor que 0.";
            else if (habitaciones is null || habitaciones <= 0) why = "Habitaciones debe ser mayor que 0.";
            else if (banos is null || banos <= 0) why = "Baños debe ser mayor que 0.";

            if (why != null)
                return Json(new
                {
                    success = false,
                    message = "❌ " + why,
                    debug = Request.Form.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())
                });

          
            var agenteId = GetAgenteIdActual();
            if (agenteId is null)
                return Json(new { success = false, message = "❌ No se ha podido identificar al agente. Inicia sesión." });

            var existeAgente = await _context.Agentes.AsNoTracking().AnyAsync(a => a.Id == agenteId.Value);
            if (!existeAgente)
                return Json(new { success = false, message = "❌ Agente inválido (no existe en la BD)." });

            try
            {
               
                var ext = (Path.GetExtension(file.FileName) ?? "").ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(ext) || !(ext == ".jpg" || ext == ".jpeg" || ext == ".png"))
                    ext = ".jpg";

                var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "propiedades");
                Directory.CreateDirectory(carpeta);

                var nombreArchivo = $"prop_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
                var ruta = Path.Combine(carpeta, nombreArchivo);

                await using (var fs = new FileStream(ruta, FileMode.Create))
                    await file.CopyToAsync(fs);

                var fotoUrl = $"/images/propiedades/{nombreArchivo}";

             
                var codigo = await GenerarCodigoUnicoAsync();

             
                var propiedad = new Propiedad
                {
                    Codigo = codigo,
                    Tipo = tipo,
                    TipoVenta = tipoVenta,
                    Ubicacion = ubicacion,
                    Precio = precio,
                    Habitaciones = habitaciones,
                    Banos = banos,
                    Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim(),

                    Metros = metros.HasValue ? (int?)Convert.ToInt32(metros.Value) : null,
                    MetrosCuadrados = metrosCuadrados.HasValue ? (int?)Convert.ToInt32(metrosCuadrados.Value) : null,

                    FotoPrincipal = fotoUrl,

                    Disponible = true,
                    Activo = true,
                    EsFavorita = false,
                    EsPublica = true,

                    FechaCreacion = DateTime.Now,
                    FechaPublicacion = DateTime.Now,
                    AgenteId = agenteId.Value
                };

                propiedad.ImagenUrl ??= propiedad.FotoPrincipal;

                _context.Propiedades.Add(propiedad);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "✅ Propiedad publicada exitosamente.",
                    redirectUrl = Url.Action("HomeAgente", "Agente", new { agenteId = agenteId.Value })
                });
            }
            catch (DbUpdateException ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "❌ Error DB: " + inner });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "❌ Error al publicar la propiedad: " + ex.Message });
            }
        }

        private async Task<string> GenerarCodigoUnicoAsync()
        {
            string Nuevo() => "P" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var intento = 0;
            var codigo = Nuevo();

            while (await _context.Propiedades.AsNoTracking().AnyAsync(p => p.Codigo == codigo))
            {
                await Task.Delay(5);
                codigo = Nuevo();
                if (++intento > 5)
                {
                    codigo = "P" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
                    break;
                }
            }
            return codigo;
        }

        private int? GetAgenteIdActual()
        {
            var cAg = User.FindFirst("AgenteId")?.Value;
            if (int.TryParse(cAg, out var idClaim)) return idClaim;

            var sesAg = HttpContext.Session.GetInt32("AgenteId");
            if (sesAg.HasValue) return sesAg.Value;

            var nameId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(nameId, out var idName)) return idName;

            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrWhiteSpace(email))
            {
                var ag = _context.Agentes.AsNoTracking().FirstOrDefault(a => a.Correo == email);
                if (ag != null)
                {
                    HttpContext.Session.SetInt32("AgenteId", ag.Id);
                    return ag.Id;
                }
            }

            var sesUsuario = HttpContext.Session.GetInt32("UsuarioId");
            if (sesUsuario.HasValue)
            {
                var usuario = _context.Usuarios.AsNoTracking().FirstOrDefault(u => u.Id == sesUsuario.Value);
                if (usuario != null && !string.IsNullOrWhiteSpace(usuario.Correo))
                {
                    var ag = _context.Agentes.AsNoTracking().FirstOrDefault(a => a.Correo == usuario.Correo);
                    if (ag != null)
                    {
                        HttpContext.Session.SetInt32("AgenteId", ag.Id);
                        return ag.Id;
                    }
                }
            }
            return null;
        }

        [HttpPost("/propiedad/eliminar-publicacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarPublicacion([FromForm] string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                TempData["Success"] = "Código inválido.";
                return RedirectToAction("HomeAgente", "Agente");
            }

            var ok = await _propiedadServicio.EliminarPorCodigoAsync(codigo);

            TempData["Success"] = ok
                ? "Publicación eliminada correctamente."
                : "No se pudo eliminar la publicación.";

            return RedirectToAction("HomeAgente", "Agente");
        }
    }
}
