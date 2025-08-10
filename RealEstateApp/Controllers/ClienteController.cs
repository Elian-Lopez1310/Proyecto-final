using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateApp.Datos;
using RealEstateApp.Entidades;
using RealEstateApp.Negocio.Servicios;
using RealEstateApp.Shared.Dtos;
using RealEstateApp.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateApp.Web.Controllers
{
    public class ClienteController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAgenteServicio _agenteServicio;
        private readonly PropiedadServicio _propiedadServicio;

        public ClienteController(
            AppDbContext context,
            IAgenteServicio agenteServicio,
            PropiedadServicio propiedadServicio)
        {
            _context = context;
            _agenteServicio = agenteServicio;
            _propiedadServicio = propiedadServicio;
        }

        private int ObtenerClienteId()
            => HttpContext.Session.GetInt32("UsuarioId") ?? 0;

        // ===========================
        // INDEX
        // ===========================
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] FiltroPropiedadViewModel filtros)
        {
            var tipoUsuario = HttpContext.Session.GetString("TipoUsuario");
            var usuarioNombre = HttpContext.Session.GetString("UsuarioNombre");
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (string.IsNullOrEmpty(tipoUsuario) || tipoUsuario != "Cliente" || usuarioId == null)
                return RedirectToAction("Login", "Cuenta");

            var favoritosIds = await _context.Favoritos
                .Where(f => f.UsuarioId == usuarioId.Value)
                .Select(f => f.PropiedadId)
                .ToHashSetAsync();

            filtros ??= new FiltroPropiedadViewModel();

            // --- Búsqueda por CÓDIGO (solo si es de agente y disponible)
            var code = filtros.Codigo?.Trim();
            if (!string.IsNullOrWhiteSpace(code))
            {
                var p = await _propiedadServicio.ObtenerPorCodigoAsync(code);
                var lista = new List<PropiedadDto>();
                if (p != null && p.AgenteId != null && p.Disponible)
                {
                    var dto = Map(p);
                    dto.EsFavorita = favoritosIds.Contains(p.Id);
                    lista.Add(dto);
                }

                filtros.Resultados = lista;
                filtros.Agentes = await _agenteServicio.ObtenerAgentesActivosAsync();
                ViewBag.UsuarioNombre = usuarioNombre;
                return View(filtros);
            }

            // --- Búsqueda normal (filtra: SOLO publicadas por agentes y disponibles)
            var encontrados = await _propiedadServicio.BuscarConFiltrosAsync(
                filtros.Codigo,
                filtros.Tipo,
                filtros.PrecioMin,
                filtros.PrecioMax,
                filtros.Habitaciones,
                filtros.Banos
            );

            encontrados = encontrados
                .Where(p => p.AgenteId != null && p.Disponible)
                .ToList();

            filtros.Resultados = encontrados
                .Select(p =>
                {
                    var dto = Map(p);
                    dto.EsFavorita = favoritosIds.Contains(p.Id);
                    return dto;
                })
                .ToList();

            filtros.Agentes = await _agenteServicio.ObtenerAgentesActivosAsync();
            ViewBag.UsuarioNombre = usuarioNombre;
            return View(filtros);
        }

        // ===========================
        // AGENTES
        // ===========================
        [HttpGet]
        public async Task<IActionResult> Agentes()
        {
            var agentes = await _agenteServicio.ObtenerAgentesActivosAsync();
            return View("~/Views/Agente/Index.cshtml", agentes);
        }

        // ===========================
        // DETALLE
        // ===========================
        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var propiedad = await _context.Propiedades
                .Include(p => p.Agente)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (propiedad == null)
                return NotFound();

            var clienteId = ObtenerClienteId();

            var mensajes = await _context.MensajesChat
                .Where(m => m.PropiedadId == id)
                .OrderBy(m => m.Fecha)
                .AsNoTracking()
                .ToListAsync();

            var ofertas = await _context.Ofertas
                .Where(o => o.PropiedadId == id && o.ClienteId == clienteId)
                .OrderByDescending(o => o.Fecha)
                .AsNoTracking()
                .ToListAsync();

            var existeAceptada = await _context.Ofertas
                .AnyAsync(o => o.PropiedadId == id && o.Estado == "Aceptada");

            var existePendienteCliente = await _context.Ofertas
                .AnyAsync(o => o.PropiedadId == id && o.ClienteId == clienteId && o.Estado == "Pendiente");

            var puedeOfertar = !existeAceptada && !existePendienteCliente;

            var modelo = new DetallePropiedadViewModel
            {
                Propiedad = propiedad,
                AgenteNombre = propiedad.Agente?.NombreCompleto ?? "",
                AgenteCorreo = propiedad.Agente?.Correo ?? "",
                AgenteTelefono = propiedad.Agente?.Telefono ?? "",
                AgenteFoto = propiedad.Agente?.FotoUrl ?? "",
                Mensajes = mensajes,
                OfertasEntidad = ofertas,
                PuedeOfertar = puedeOfertar
            };

            return View("Detalle", modelo);
        }

        // ===========================
        // CHAT Y OFERTAS
        // ===========================
        [HttpPost]
        public async Task<IActionResult> EnviarMensaje(int propiedadId, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Json(new { success = false, message = "El mensaje no puede estar vacío." });

            var msg = new MensajeChat
            {
                PropiedadId = propiedadId,
                ClienteId = ObtenerClienteId(),
                Texto = texto.Trim(),
                Fecha = DateTime.Now,
                Remitente = "Cliente"
            };

            _context.MensajesChat.Add(msg);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> EnviarOferta(int propiedadId, decimal monto)
        {
            if (monto <= 0)
                return Json(new { success = false, message = "La oferta debe ser mayor a cero." });

            var clienteId = ObtenerClienteId();

            var existeBloqueo = await _context.Ofertas.AnyAsync(o =>
                o.PropiedadId == propiedadId &&
                (o.Estado == "Aceptada" || (o.ClienteId == clienteId && o.Estado == "Pendiente")));

            if (existeBloqueo)
                return Json(new { success = false, message = "Ya existe una oferta pendiente o aceptada." });

            var oferta = new Oferta
            {
                PropiedadId = propiedadId,
                ClienteId = clienteId,
                Monto = monto,
                Fecha = DateTime.Now,
                Estado = "Pendiente"
            };

            _context.Ofertas.Add(oferta);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Oferta enviada correctamente." });
        }

        // ===========================
        // FAVORITOS (vista)
        // ===========================
        [HttpGet]
        public async Task<IActionResult> Favoritos()
        {
            var userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return RedirectToAction("Login", "Cuenta");

            var lista = await _context.Favoritos
                .Where(f => f.UsuarioId == userId.Value)
                .Include(f => f.Propiedad)
                    .ThenInclude(p => p.Imagenes)
                .OrderByDescending(f => f.Fecha)
                .AsNoTracking()
                .Select(f => new PropiedadDto
                {
                    Codigo = f.Propiedad.Codigo,
                    Tipo = f.Propiedad.Tipo,
                    Ubicacion = f.Propiedad.Ubicacion,
                    TipoVenta = f.Propiedad.TipoVenta,
                    Precio = f.Propiedad.Precio ?? 0m,
                    Habitaciones = f.Propiedad.Habitaciones ?? 0,
                    Banos = f.Propiedad.Banos ?? 0,
                    Metros = (f.Propiedad.Metros ?? 0) > 0 ? (f.Propiedad.Metros ?? 0) : (f.Propiedad.MetrosCuadrados ?? 0),
                    MetrosCuadrados = f.Propiedad.MetrosCuadrados ?? 0,
                    ImagenUrl =
                        !string.IsNullOrWhiteSpace(f.Propiedad.ImagenUrl) ? f.Propiedad.ImagenUrl :
                        !string.IsNullOrWhiteSpace(f.Propiedad.FotoPrincipal) ? f.Propiedad.FotoPrincipal :
                        f.Propiedad.Imagenes.Select(i => i.Url).FirstOrDefault() ?? "/images/default.png",
                    FotoPrincipal = f.Propiedad.FotoPrincipal
                })
                .ToListAsync();

            return View(lista);
        }


        private static string NormCode(string? c) => (c ?? "").Trim().ToUpperInvariant();
        public class FavReq { public string Codigo { get; set; } = ""; }


        [HttpPost]
        public async Task<IActionResult> AgregarFavorito([FromBody] FavReq req)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UsuarioId");
                if (userId == null)
                    return Unauthorized(new { success = false, message = "Debes iniciar sesión." });

                var code = NormCode(req.Codigo);
                if (string.IsNullOrEmpty(code))
                    return BadRequest(new { success = false, message = "Código inválido." });

                var prop = await _context.Propiedades
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Codigo != null && p.Codigo.Trim().ToUpper() == code);

                if (prop == null)
                    return NotFound(new { success = false, message = $"Propiedad '{code}' no encontrada." });

                bool ya = await _context.Favoritos
                    .AnyAsync(f => f.UsuarioId == userId.Value && f.PropiedadId == prop.Id);

                if (ya)
                {
                    var totalFavoritos = await _context.Favoritos.CountAsync(f => f.UsuarioId == userId.Value);
                    return Ok(new { success = true, message = "Ya estaba en favoritos.", isFavorite = true, total = totalFavoritos, codigo = prop.Codigo });
                }

                _context.Favoritos.Add(new Favorito
                {
                    UsuarioId = userId.Value,
                    PropiedadId = prop.Id,
                    Fecha = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                var total = await _context.Favoritos.CountAsync(f => f.UsuarioId == userId.Value);
                return Ok(new { success = true, message = "Agregado a favoritos.", isFavorite = true, total, codigo = prop.Codigo });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { success = false, message = "DB error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error en servidor: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> QuitarFavorito([FromBody] FavReq req)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UsuarioId");
                if (userId == null)
                    return Unauthorized(new { success = false, message = "Debes iniciar sesión." });

                var code = NormCode(req.Codigo);
                if (string.IsNullOrEmpty(code))
                    return BadRequest(new { success = false, message = "Código inválido." });

                var prop = await _context.Propiedades
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Codigo != null && p.Codigo.Trim().ToUpper() == code);

                if (prop == null)
                    return NotFound(new { success = false, message = $"Propiedad '{code}' no encontrada." });

                var fav = await _context.Favoritos
                    .FirstOrDefaultAsync(f => f.UsuarioId == userId.Value && f.PropiedadId == prop.Id);

                if (fav == null)
                {
                    var totalFavoritos = await _context.Favoritos.CountAsync(f => f.UsuarioId == userId.Value);
                    return Ok(new { success = true, message = "No estaba en favoritos.", isFavorite = false, total = totalFavoritos, codigo = prop.Codigo });
                }

                _context.Favoritos.Remove(fav);
                await _context.SaveChangesAsync();

                var total = await _context.Favoritos.CountAsync(f => f.UsuarioId == userId.Value);
                return Ok(new { success = true, message = "Quitado de favoritos.", isFavorite = false, total, codigo = prop.Codigo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error en servidor: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MisPropiedades()
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return RedirectToAction("Login", "Cuenta");

            var propsFav = await _context.Favoritos
                .Where(f => f.UsuarioId == userId.Value)
                .Include(f => f.Propiedad)
                .AsNoTracking()
                .Where(f => f.Propiedad != null) // seguridad por si borran una propiedad
                .OrderByDescending(f => f.Fecha)
                .Select(f => new PropiedadDto
                {
                    Id = f.Propiedad.Id,
                    Codigo = f.Propiedad.Codigo,
                    Tipo = f.Propiedad.Tipo,
                    TipoVenta = f.Propiedad.TipoVenta,
                    Ubicacion = f.Propiedad.Ubicacion,
                    Precio = f.Propiedad.Precio ?? 0,
                    Habitaciones = f.Propiedad.Habitaciones ?? 0,
                    Banos = f.Propiedad.Banos ?? 0,
                    Metros = (f.Propiedad.Metros ?? 0) > 0 ? (f.Propiedad.Metros ?? 0) : (f.Propiedad.MetrosCuadrados ?? 0),
                    MetrosCuadrados = f.Propiedad.MetrosCuadrados ?? 0,
                    ImagenUrl = !string.IsNullOrWhiteSpace(f.Propiedad.ImagenUrl)
                        ? f.Propiedad.ImagenUrl
                        : (!string.IsNullOrWhiteSpace(f.Propiedad.FotoPrincipal)
                            ? f.Propiedad.FotoPrincipal
                            : "/images/default.png"),
                    EsFavorita = true
                })
                .ToListAsync();

            return View("~/Views/Cliente/MisPropiedades.cshtml", propsFav);
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> FavoritosIds()
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return Unauthorized();

            var codigos = await _context.Favoritos
                .Where(f => f.UsuarioId == userId.Value)
                .Join(_context.Propiedades.AsNoTracking(), f => f.PropiedadId, p => p.Id, (f, p) => p.Codigo)
                .Where(c => c != null)
                .Select(c => c!) // non-null
                .ToListAsync();

            return Ok(codigos);
        }


        private static PropiedadDto Map(Propiedad p)
        {
            string imagen =
                !string.IsNullOrWhiteSpace(p.ImagenUrl) ? p.ImagenUrl :
                !string.IsNullOrWhiteSpace(p.FotoPrincipal) ? p.FotoPrincipal :
                "/images/default.png";

            int m = (p.Metros ?? 0) > 0 ? (p.Metros ?? 0) : (p.MetrosCuadrados ?? 0);

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
                Metros = m,
                MetrosCuadrados = p.MetrosCuadrados ?? 0,
                ImagenUrl = imagen,
                FotoPrincipal = p.FotoPrincipal
            };
        }

        // ===========================
        // FILTRAR AJAX (PARCIAL)
        // ===========================
        [HttpGet]
        public async Task<IActionResult> FiltrarAjax([FromQuery] FiltroPropiedadViewModel filtros)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;

            var favoritosIds = await _context.Favoritos
                .Where(f => f.UsuarioId == usuarioId)
                .Select(f => f.PropiedadId)
                .ToListAsync();

            var props = await _propiedadServicio.BuscarConFiltrosAsync(
                filtros.Codigo,
                filtros.Tipo,
                filtros.PrecioMin,
                filtros.PrecioMax,
                filtros.Habitaciones,
                filtros.Banos
            );

            // ⬅️ SOLO publicadas por agente y disponibles
            props = props.Where(p => p.AgenteId != null && p.Disponible).ToList();

            filtros.Resultados = props.Select(p => new PropiedadDto
            {
                Id = p.Id,
                Codigo = p.Codigo ?? "N/A",
                Tipo = p.Tipo ?? "Sin tipo",
                TipoVenta = p.TipoVenta ?? "N/A",
                Precio = p.Precio ?? 0,
                Habitaciones = p.Habitaciones ?? 0,
                Banos = p.Banos ?? 0,
                Metros = (p.Metros ?? 0) > 0 ? (p.Metros ?? 0) : (p.MetrosCuadrados ?? 0),
                MetrosCuadrados = p.MetrosCuadrados ?? 0,
                Ubicacion = p.Ubicacion ?? "Ubicación no definida",
                ImagenUrl = p.ImagenUrl,
                FotoPrincipal = p.FotoPrincipal,
                EsFavorita = favoritosIds.Contains(p.Id)
            }).ToList();

            return PartialView("~/Views/Cliente/_GridPropiedades.cshtml", filtros);
        }
    }
}
