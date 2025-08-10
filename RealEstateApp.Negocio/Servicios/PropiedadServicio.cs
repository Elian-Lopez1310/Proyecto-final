using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RealEstateApp.Datos;
using RealEstateApp.Entidades;
using RealEstateApp.Shared.Dtos;

namespace RealEstateApp.Negocio.Servicios
{
    public class PropiedadServicio
    {
        private readonly AppDbContext _context;

        public PropiedadServicio(AppDbContext context)
        {
            _context = context;
        }

     
        private List<Propiedad> ObtenerPropiedadesSimuladas()
        {
            return new List<Propiedad>
            {
                new Propiedad
                {
                    Codigo = "PROP-001", Tipo = "Apartamento", TipoVenta = "Venta",
                    Precio = 120000, Habitaciones = 3, Banos = 2, MetrosCuadrados = 100,
                    Ubicacion = "Santo Domingo", ImagenUrl = "/images/propiedades/Apartamento.jpg",
                    Disponible = true, FechaCreacion = DateTime.Now.AddDays(-10)
                },
                new Propiedad
                {
                    Codigo = "PROP-002", Tipo = "Casa", TipoVenta = "Venta",
                    Precio = 150000, Habitaciones = 4, Banos = 3, MetrosCuadrados = 150,
                    Ubicacion = "Santiago", ImagenUrl = "/images/propiedades/Mm.jpg",
                    Disponible = true, FechaCreacion = DateTime.Now.AddDays(-8)
                },
                new Propiedad
                {
                    Codigo = "PROP-003", Tipo = "Casa", TipoVenta = "Alquiler",
                    Precio = 2000, Habitaciones = 5, Banos = 4, MetrosCuadrados = 200,
                    Ubicacion = "Punta Cana", ImagenUrl = "/images/propiedades/prop1.jpg",
                    Disponible = true, FechaCreacion = DateTime.Now.AddDays(-5)
                },
                new Propiedad
                {
                    Codigo = "PROP-004", Tipo = "Villa", TipoVenta = "Venta",
                    Precio = 250000, Habitaciones = 6, Banos = 5, MetrosCuadrados = 300,
                    Ubicacion = "La Romana", ImagenUrl = "/images/propiedades/Villa.jpg",
                    Disponible = true, FechaCreacion = DateTime.Now.AddDays(-2)
                }
            };
        }

        
        public async Task<Propiedad?> ObtenerPorCodigoAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;

            var code = codigo.Trim().ToUpper();
            return await _context.Propiedades
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Codigo != null &&
                    p.Codigo.Trim().ToUpper() == code   
                );
        }

      
        public async Task<List<Propiedad>> ObtenerPropiedadesDisponiblesAsync(string? codigo = null)
        {
            var query = _context.Propiedades
                .AsNoTracking()
                .Where(p => p.Disponible);

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                var code = codigo.Trim().ToUpper();
                query = query.Where(p =>
                    p.Codigo != null &&
                    p.Codigo.Trim().ToUpper() == code   
                );
            }

            var lista = await query
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            if (lista.Any()) return lista;

            var simuladas = ObtenerPropiedadesSimuladas();

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                var code = codigo.Trim();
                simuladas = simuladas
                    .Where(p => p.Codigo != null &&
                                p.Codigo.Trim().Equals(code, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return simuladas;
        }

     
        public async Task<List<Propiedad>> BuscarConFiltrosAsync(
            string? codigo, string? tipo, decimal? precioMin, decimal? precioMax, int? habitaciones, int? banos)
        {
         
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                var code = codigo.Trim().ToUpper();

                var porCodigo = await _context.Propiedades
                    .AsNoTracking()
                    .Where(p => p.Disponible
                                && p.Codigo != null
                                && p.Codigo.Trim().ToUpper() == code)  
                    .OrderByDescending(p => p.FechaCreacion)
                    .ToListAsync();

                if (porCodigo.Any()) return porCodigo;

          
                return ObtenerPropiedadesSimuladas()
                    .Where(p => p.Codigo != null
                                && p.Codigo.Trim().Equals(code, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

        
            var query = _context.Propiedades
                .AsNoTracking()
                .Where(p => p.Disponible)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tipo) && tipo != "Todos")
                query = query.Where(p => p.Tipo == tipo);

            if (precioMin.HasValue) query = query.Where(p => p.Precio >= precioMin.Value);
            if (precioMax.HasValue) query = query.Where(p => p.Precio <= precioMax.Value);
            if (habitaciones.HasValue) query = query.Where(p => p.Habitaciones >= habitaciones.Value);
            if (banos.HasValue) query = query.Where(p => p.Banos >= banos.Value);

            var lista = await query
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            if (lista.Any()) return lista;

            
            var sims = ObtenerPropiedadesSimuladas();
            if (!string.IsNullOrWhiteSpace(tipo) && tipo != "Todos") sims = sims.Where(p => p.Tipo == tipo).ToList();
            if (precioMin.HasValue) sims = sims.Where(p => p.Precio >= precioMin.Value).ToList();
            if (precioMax.HasValue) sims = sims.Where(p => p.Precio <= precioMax.Value).ToList();
            if (habitaciones.HasValue) sims = sims.Where(p => p.Habitaciones >= habitaciones.Value).ToList();
            if (banos.HasValue) sims = sims.Where(p => p.Banos >= banos.Value).ToList();

            return sims.OrderByDescending(p => p.FechaCreacion).ToList();
        }

   
        public async Task<List<PropiedadDto>> ObtenerPropiedadesAsync()
        {
            var propiedades = await _context.Propiedades
                .AsNoTracking()
                .Where(p => p.Disponible)
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            if (!propiedades.Any())
                propiedades = ObtenerPropiedadesSimuladas();

            return propiedades.Select(p => new PropiedadDto
            {
                Id = p.Id,
                Codigo = p.Codigo ?? "N/A",
                Tipo = p.Tipo ?? "Sin tipo",
                TipoVenta = p.TipoVenta ?? "N/A",
                Precio = p.Precio ?? 0,
                Habitaciones = p.Habitaciones ?? 0,
                Banos = p.Banos ?? 0,
                MetrosCuadrados = p.MetrosCuadrados ?? 0,
                Ubicacion = p.Ubicacion ?? "Ubicación no definida",
                ImagenUrl = string.IsNullOrWhiteSpace(p.ImagenUrl) ? "/images/default.png" : p.ImagenUrl
            }).ToList();
        }

        public async Task<bool> EliminarPorCodigoAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            var prop = await _context.Propiedades
                .FirstOrDefaultAsync(p => p.Codigo == codigo);

            if (prop == null)
                return false;

        
            prop.Disponible = false;
            prop.Activo = false;
            prop.EsPublica = false; 
            if (!string.Equals(prop.TipoVenta, "Vendida", StringComparison.OrdinalIgnoreCase))
                prop.TipoVenta = "Eliminada"; 

            await _context.SaveChangesAsync();
            return true;
        }


    }

}