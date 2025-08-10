
using System.Threading.Tasks;
using System.Collections.Generic;
using RealEstateApp.Entidades; 

public interface IPropiedadServicio
{
    Task<List<Propiedad>> BuscarConFiltrosAsync(string? codigo, string? tipo, decimal? precioMin,
                                                decimal? precioMax, int? habitaciones, int? banos);


    Task<Propiedad?> ObtenerPorCodigoAsync(string codigo);

    Task<bool> EliminarPorCodigoAsync(string codigo);

}
