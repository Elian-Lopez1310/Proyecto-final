using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RealEstateApp.Shared.Dtos;

namespace RealEstateApp.Negocio.Servicios
{
    public interface IAgenteServicio
    {
        Task<List<AgenteDto>> ObtenerAgentesActivosAsync();
        Task<AgenteDto?> ObtenerPorIdAsync(int id);
        Task<List<PropiedadDto>> ObtenerPropiedadesPorAgenteAsync(int agenteId);
    }
}
