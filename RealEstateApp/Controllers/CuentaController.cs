using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateApp.Datos;
using RealEstateApp.Entidades;
using RealEstateApp.Helpers;
using RealEstateApp.ViewModels;
using RealEstateApp.Web.ViewModels;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;

namespace RealEstateApp.Controllers
{
    public class CuentaController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public CuentaController(IWebHostEnvironment env, AppDbContext context, IConfiguration config)
        {
            _env = env;
            _context = context;
            _config = config;
        }

        // =======================
        // REGISTRO
        // =======================
        [HttpGet]
        public IActionResult Registro() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(RegistroUsuarioViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "❌ Verifica los datos del formulario." });

            if (model.Contrasena != model.ConfirmarClave)
                return Json(new { success = false, message = "❌ Las contraseñas no coinciden." });

            if (await _context.Usuarios.AnyAsync(u => u.Correo == model.Correo || u.NombreUsuario == model.NombreUsuario))
                return Json(new { success = false, message = "❌ El correo o nombre de usuario ya está en uso." });

            // Subida opcional de foto de usuario
            string? fotoNombre = null;
            if (model.Foto != null)
            {
                var uploads = Path.Combine(_env.WebRootPath, "images", "usuarios");
                Directory.CreateDirectory(uploads);
                fotoNombre = Guid.NewGuid() + Path.GetExtension(model.Foto.FileName);
                await using var fs = new FileStream(Path.Combine(uploads, fotoNombre), FileMode.Create);
                await model.Foto.CopyToAsync(fs);
            }

            var rol = (model.TipoUsuario ?? "").Trim();
            var fotoUrlUsuario = fotoNombre is null ? null : "/images/usuarios/" + fotoNombre;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1) USUARIO
                var usuario = new Usuario
                {
                    Nombre = model.Nombre?.Trim(),
                    Apellido = model.Apellido?.Trim(),
                    Telefono = model.Telefono?.Trim(),
                    FotoUrl = fotoUrlUsuario,
                    NombreUsuario = model.NombreUsuario?.Trim(),
                    Correo = model.Correo?.Trim(),
                    Clave = model.Contrasena?.Trim(), // TODO: hashear
                    TipoUsuario = rol,
                    Activo = false,
                    FechaRegistro = DateTime.Now
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // 2) AGENTE (si corresponde) — asegura FotoUrl NOT NULL y Activo
                if (rol.Equals("Agente", StringComparison.OrdinalIgnoreCase))
                {
                    var correoNorm = (usuario.Correo ?? "").Trim().ToLower();
                    var existe = await _context.Agentes
                        .AnyAsync(a => (a.Correo ?? "").Trim().ToLower() == correoNorm);

                    if (!existe)
                    {
                        var agente = new Agente
                        {
                            Nombre = usuario.Nombre ?? "",
                            Apellido = usuario.Apellido ?? "",
                            Telefono = usuario.Telefono ?? "",
                            Correo = usuario.Correo ?? "",
                            FotoUrl = string.IsNullOrWhiteSpace(usuario.FotoUrl)
                                      ? "/images/agentes/default.jpg"
                                      : usuario.FotoUrl,
                            // campos no nulos por si tu esquema los exige
                            Foto = string.IsNullOrWhiteSpace(usuario.FotoUrl) ? "/images/agentes/default.jpg" : usuario.FotoUrl,
                            Descripcion = "",
                            Activo = true
                        };

                        _context.Agentes.Add(agente);
                        await _context.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Json(new { success = false, message = "❌ No se pudo completar el registro: " + (ex.InnerException?.Message ?? ex.Message) });
            }

            // 3) Envío de correo de activación (best-effort)
            try
            {
                var usuarioCreado = await _context.Usuarios.FirstAsync(u => u.Correo == model.Correo);
                await EnviarCorreoActivacion(usuarioCreado);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = true,
                    message = "⚠️ Usuario/Agente creado, pero no se pudo enviar el correo: " + ex.Message
                });
            }

            return Json(new { success = true, message = "✅ Registro exitoso. Revisa tu correo para activar la cuenta." });
        }

        private async Task EnviarCorreoActivacion(Usuario usuario)
        {
            var emailConfig = _config.GetSection("EmailSettings").Get<EmailSettings>();
            if (emailConfig?.Simular == true) return;

            var urlActivacion = Url.Action("ActivarCuenta", "Cuenta", new { correo = usuario.Correo }, Request.Scheme);

            var mensaje = $@"
                <h2>Hola, {usuario.Nombre} 👋</h2>
                <p>Gracias por registrarte en nuestra plataforma inmobiliaria.</p>
                <p>Haz clic en el siguiente enlace para activar tu cuenta:</p>
                <p><a href='{urlActivacion}'>✅ Activar cuenta</a></p>
                <p>Si no te registraste, puedes ignorar este correo.</p>";

            var correo = new MailMessage(
                new MailAddress(emailConfig!.CorreoRemitente),
                new MailAddress(usuario.Correo))
            {
                Subject = "Activa tu cuenta en RealEstateApp",
                Body = mensaje,
                IsBodyHtml = true
            };

            using var smtp = new SmtpClient(emailConfig.ServidorSMTP)
            {
                Port = emailConfig.Puerto,
                Credentials = new NetworkCredential(emailConfig.CorreoRemitente, emailConfig.Clave),
                EnableSsl = true
            };

            await smtp.SendMailAsync(correo);
        }

        [HttpGet]
        public async Task<IActionResult> ActivarCuenta(string correo)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
            if (usuario == null) return NotFound("Usuario no encontrado.");

            usuario.Activo = true;
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "✅ Cuenta activada correctamente. Ahora puedes iniciar sesión.";
            return RedirectToAction("Login");
        }

        // =======================
        // LOGIN / LOGOUT
        // =======================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "❌ Verifica los datos del formulario." });

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Correo == model.Correo && u.Clave == model.Clave && u.Activo);

            if (usuario == null)
                return Json(new { success = false, message = "❌ Credenciales inválidas." });

            var rol = (usuario.TipoUsuario ?? "").Trim();

           
            int? agenteId = null;
            if (rol.Equals("Agente", StringComparison.OrdinalIgnoreCase))
            {
                var agente = await _context.Agentes
                    .FirstOrDefaultAsync(a => a.Correo == usuario.Correo);

                if (agente == null)
                {
                   
                    agente = new Agente
                    {
                        Nombre = usuario.Nombre ?? "",
                        Apellido = usuario.Apellido ?? "",
                        Telefono = usuario.Telefono ?? "",
                        Correo = usuario.Correo ?? "",
                        FotoUrl = string.IsNullOrWhiteSpace(usuario.FotoUrl)
                                  ? "/images/agentes/default.jpg"
                                  : usuario.FotoUrl,
                        Foto = string.IsNullOrWhiteSpace(usuario.FotoUrl) ? "/images/agentes/default.jpg" : usuario.FotoUrl,
                        Descripcion = "",
                        Activo = true
                    };
                    _context.Agentes.Add(agente);
                    await _context.SaveChangesAsync();
                }

                agenteId = agente.Id;
            }

           
            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            HttpContext.Session.SetString("UsuarioNombre", (usuario.NombreUsuario ?? usuario.Nombre)?.Trim() ?? "");
            HttpContext.Session.SetString("TipoUsuario", rol);
            if (agenteId.HasValue)
                HttpContext.Session.SetInt32("AgenteId", agenteId.Value);

          
            var nameIdentifier = (agenteId ?? usuario.Id).ToString();

            var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, nameIdentifier),      
    new Claim(ClaimTypes.Name, (usuario.NombreUsuario ?? usuario.Nombre)?.Trim() ?? ""),
    new Claim(ClaimTypes.Role, rol),
    new Claim("UsuarioId", usuario.Id.ToString()),
    new Claim(ClaimTypes.Email, usuario.Correo ?? "")          
};
            if (agenteId.HasValue)
                claims.Add(new Claim("AgenteId", agenteId.Value.ToString())); 

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimTypes.Name,
                ClaimTypes.Role
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));


          
            if (rol.Equals("Agente", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = true, redirectUrl = Url.Action("HomeAgente", "Agente") });

            if (rol.Equals("Cliente", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Cliente") });

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
        }

       

        [HttpGet]
        public async Task<IActionResult> CerrarSesion()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Cuenta");
        }
    }
}
