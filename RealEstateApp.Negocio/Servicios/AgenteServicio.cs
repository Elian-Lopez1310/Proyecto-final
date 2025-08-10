using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RealEstateApp.Datos;
using RealEstateApp.Entidades;
using RealEstateApp.Shared.Dtos;

namespace RealEstateApp.Negocio.Servicios
{
    public class AgenteServicio : IAgenteServicio
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public AgenteServicio(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

    
        public async Task<List<AgenteDto>> ObtenerAgentesActivosAsync()
        {
            var agentes = await _context.Agentes
                .Where(a => a.Activo == true)
                .ToListAsync();

            var lista = new List<AgenteDto>();

            foreach (var agente in agentes)
            {
                var propiedadDestacada = await _context.Propiedades
                    .Where(p => p.AgenteId == agente.Id && p.Disponible == true && p.Activo == true)
                    .OrderBy(p => p.Id)
                    .FirstOrDefaultAsync();

                var dto = _mapper.Map<AgenteDto>(agente);
                dto.CodigoPropiedadDestacada = propiedadDestacada?.Codigo;

                lista.Add(dto);
            }

            return lista;
        }

        public async Task<AgenteDto?> ObtenerPorIdAsync(int id)
        {
            var agente = await _context.Agentes.FindAsync(id);
            return agente != null ? _mapper.Map<AgenteDto>(agente) : null;
        }

        public async Task<List<PropiedadDto>> ObtenerPropiedadesPorAgenteAsync(int agenteId)
        {
            var propiedades = await _context.Propiedades
                .Where(p => p.AgenteId == agenteId && p.Activo == true)
                .ToListAsync();

            return _mapper.Map<List<PropiedadDto>>(propiedades);
        }
    }
}
