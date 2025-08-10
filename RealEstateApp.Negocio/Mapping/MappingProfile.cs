
using AutoMapper;
using RealEstateApp.Entidades;
using RealEstateApp.Shared.Dtos;

namespace RealEstateApp.Negocio.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
       
            CreateMap<Propiedad, PropiedadDto>()
                .ForMember(d => d.Precio, opt => opt.MapFrom(s => s.Precio ?? 0))
                .ForMember(d => d.Metros, opt => opt.MapFrom(s => s.Metros ?? 0))
                .ForMember(d => d.MetrosCuadrados, opt => opt.MapFrom(s => s.MetrosCuadrados ?? 0))
                .ForMember(d => d.Habitaciones, opt => opt.MapFrom(s => s.Habitaciones ?? 0))
                .ForMember(d => d.Banos, opt => opt.MapFrom(s => s.Banos ?? 0))
                .ForMember(d => d.ImagenUrl, opt => opt.MapFrom(s =>
                    string.IsNullOrWhiteSpace(s.ImagenUrl) ? s.FotoPrincipal : s.ImagenUrl));

            CreateMap<PropiedadDto, Propiedad>();

  
            CreateMap<Agente, AgenteDto>()
                .ForMember(d => d.NombreCompleto, opt => opt.MapFrom(s => $"{s.Nombre} {s.Apellido}".Trim()))
                .ForMember(d => d.FotoUrl, opt => opt.MapFrom(s => s.FotoUrl ?? string.Empty));
        }
    }
}
