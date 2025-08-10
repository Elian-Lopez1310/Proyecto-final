using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateApp.Datos;
using RealEstateApp.Entidades;

namespace RealEstateApp.Controllers
{
    public class FavoritosController : Controller
    {
        private readonly AppDbContext _context;

        public FavoritosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Marcar(int id)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Json(new { success = false });

            var favorito = await _context.Favoritos
                .FirstOrDefaultAsync(f => f.UsuarioId == usuarioId && f.PropiedadId == id);

            bool esFavorito;

            if (favorito != null)
            {
                _context.Favoritos.Remove(favorito);
                esFavorito = false;
            }
            else
            {
                _context.Favoritos.Add(new Favorito
                {
                    UsuarioId = usuarioId.Value,
                    PropiedadId = id
                });
                esFavorito = true;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, esFavorito });
        }
    }
}
