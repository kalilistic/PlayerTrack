using AutoMapper;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class PlayerCustomizeHistoryMappingProfile : Profile
{
    public PlayerCustomizeHistoryMappingProfile()
    {
        CreateMap<PlayerCustomizeHistory, PlayerCustomizeHistoryDTO>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.is_migrated, opt => opt.MapFrom(src => src.IsMigrated))
            .ForMember(dest => dest.player_id, opt => opt.MapFrom(src => src.PlayerId))
            .ForMember(dest => dest.customize, opt => opt.MapFrom(src => src.Customize));

        CreateMap<PlayerCustomizeHistoryDTO, PlayerCustomizeHistory>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.updated))
            .ForMember(dest => dest.IsMigrated, opt => opt.MapFrom(src => src.is_migrated))
            .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.player_id))
            .ForMember(dest => dest.Customize, opt => opt.MapFrom(src => src.customize));
    }
}
