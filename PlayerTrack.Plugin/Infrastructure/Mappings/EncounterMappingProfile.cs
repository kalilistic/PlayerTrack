using AutoMapper;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class EncounterMappingProfile : Profile
{
    public EncounterMappingProfile()
    {
        CreateMap<Encounter, EncounterDTO>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.territory_type_id, opt => opt.MapFrom(src => src.TerritoryTypeId))
            .ForMember(dest => dest.ended, opt => opt.MapFrom(src => src.Ended));

        CreateMap<EncounterDTO, Encounter>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.updated))
            .ForMember(dest => dest.TerritoryTypeId, opt => opt.MapFrom(src => src.territory_type_id))
            .ForMember(dest => dest.Ended, opt => opt.MapFrom(src => src.ended));
    }
}
