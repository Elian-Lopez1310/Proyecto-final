using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateApp.Datos;        // DbContext
using RealEstateApp.Entidades;    // Usuario, Agente, Propiedad, ChatMensaje
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RealEstateApp.Web.Controllers
{
    public class ChatController : Controller
    {
        private readonly AppDbContext _db;

        public ChatController(AppDbContext db)
        {
            _db = db;
        }

        // =======================================================
        // Helpers
        // =======================================================

        // Devuelve SIEMPRE Usuarios.Id del usuario autenticado.
        private async Task<int> GetUsuarioIdAsync()
        {
            // 1) Claim numérica más común
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("UserId")?.Value
                     ?? User.FindFirst("Id")?.Value;

            if (int.TryParse(idStr, out var idNum) && idNum > 0)
                return idNum;

            // 2) Por nombre de usuario o correo en Identity.Name
            var userNameOrEmail = User.Identity?.Name
                               ?? User.FindFirst("UserName")?.Value
                               ?? User.FindFirst(ClaimTypes.Email)?.Value;

            if (!string.IsNullOrWhiteSpace(userNameOrEmail))
            {
                var id = await _db.Usuarios
                    .Where(u => u.NombreUsuario == userNameOrEmail || u.Correo == userNameOrEmail)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                if (id > 0) return id;
            }

            return 0;
        }

        private async Task<string?> GetTipoUsuarioAsync(int usuarioId)
        {
            return await _db.Usuarios
                .Where(u => u.Id == usuarioId)
                .Select(u => u.TipoUsuario)
                .FirstOrDefaultAsync();
        }

        // Mapea Agentes.Id -> Usuarios.Id del agente (por Correo).
        // Si más adelante agregas Agente.UsuarioId, cambia este método a un simple lookup por FK.
        private async Task<int> ResolveUsuarioIdDelAgenteAsync(int agenteId)
        {
            var correoAgente = await _db.Agentes
                .Where(a => a.Id == agenteId)
                .Select(a => a.Correo)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(correoAgente)) return 0;

            var usuarioId = await _db.Usuarios
                .Where(u => u.Correo == correoAgente && u.Activo && u.TipoUsuario == "Agente")
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            return usuarioId;
        }

        // =======================================================
        // Cliente -> Agente asignado a la propiedad
        // =======================================================
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EnviarMensaje([FromForm] int PropiedadId, [FromForm] string Texto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Texto))
                    return Json(new { success = false, message = "Mensaje vacío." });

                var emisorId = await GetUsuarioIdAsync(); // Usuarios.Id del cliente
                if (emisorId == 0)
                    return Json(new { success = false, message = "No autenticado." });

                // 1) Propiedad y su AgenteId (tabla Agentes)
                var prop = await _db.Propiedades
                    .Where(p => p.Id == PropiedadId)
                    .Select(p => new { p.Id, p.AgenteId })
                    .FirstOrDefaultAsync();

                if (prop == null)
                    return Json(new { success = false, message = "Propiedad no encontrada." });

                if (prop.AgenteId == null)
                    return Json(new { success = false, message = "La propiedad no tiene un agente asignado." });

                // 2) Convertir Agentes.Id -> Usuarios.Id del agente receptor
                var receptorId = await ResolveUsuarioIdDelAgenteAsync(prop.AgenteId.Value);
                if (receptorId == 0)
                    return Json(new { success = false, message = "No se encontró el usuario del agente asignado." });

                // 3) Guardar mensaje
                _db.ChatMensajes.Add(new ChatMensaje
                {
                    PropiedadId = PropiedadId,
                    EmisorId = emisorId,          // cliente (Usuarios.Id)
                    ReceptorId = receptorId,      // agente (Usuarios.Id)
                    Texto = Texto.Trim(),
                    Fecha = DateTime.UtcNow,
                    EsDelAgente = false,
                    Leido = false
                });

                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (DbUpdateException ex)
            {
                var root = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = root });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =======================================================
        // Listar mensajes para la propiedad (según usuario logueado)
        //  - Cliente: lo que envió + respuestas del agente hacia él
        //  - Agente : lo que le enviaron + sus respuestas
        // =======================================================
        [HttpGet]
        public async Task<IActionResult> ListarMensajes(int propiedadId)
        {
            var userId = await GetUsuarioIdAsync(); // SIEMPRE Usuarios.Id
            if (userId == 0)
                return Json(new { success = false, message = "No autenticado." });

            var tipo = await GetTipoUsuarioAsync(userId);

            if (string.Equals(tipo, "Agente", StringComparison.OrdinalIgnoreCase))
            {
                // Agente
                var mensajesAgente = await _db.ChatMensajes
                    .Where(m => m.PropiedadId == propiedadId &&
                                (m.ReceptorId == userId || (m.EsDelAgente && m.EmisorId == userId)))
                    .OrderBy(m => m.Fecha)
                    .Select(m => new
                    {
                        texto = m.Texto,
                        esDelAgente = m.EsDelAgente,
                        fecha = m.Fecha
                    })
                    .ToListAsync();

                return Json(new { success = true, data = mensajesAgente });
            }
            else
            {
                // Cliente
                var mensajesCliente = await _db.ChatMensajes
                    .Where(m => m.PropiedadId == propiedadId &&
                                (m.EmisorId == userId || (m.EsDelAgente && m.ReceptorId == userId)))
                    .OrderBy(m => m.Fecha)
                    .Select(m => new
                    {
                        texto = m.Texto,
                        esDelAgente = m.EsDelAgente,
                        fecha = m.Fecha
                    })
                    .ToListAsync();

                return Json(new { success = true, data = mensajesCliente });
            }
        }

      
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Responder([FromForm] int PropiedadId, [FromForm] int ClienteId, [FromForm] string Texto)
        {
            if (string.IsNullOrWhiteSpace(Texto))
                return Json(new { success = false, message = "Mensaje vacío." });

            var agenteId = await GetUsuarioIdAsync();
            if (agenteId == 0)
                return Json(new { success = false, message = "No autenticado." });

            var tipo = await GetTipoUsuarioAsync(agenteId);
            if (!string.Equals(tipo, "Agente", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Solo agentes pueden responder aquí." });

            // Crea el mensaje de respuesta (del agente hacia el cliente)
            _db.ChatMensajes.Add(new ChatMensaje
            {
                PropiedadId = PropiedadId,
                EmisorId = agenteId,     
                ReceptorId = ClienteId,  
                Texto = Texto.Trim(),
                Fecha = DateTime.UtcNow,
                EsDelAgente = true,
                Leido = false
            });

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
