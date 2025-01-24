using AutoMapper;

namespace PlayerTrack.Infrastructure;

public class PlayerNameWorldHistoryMappingProfile : Profile
{
    public PlayerNameWorldHistoryMappingProfile()
    {
        CreateMap<Models.PlayerNameWorldHistory, PlayerNameWorldHistoryDTO>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.is_migrated, opt => opt.MapFrom(src => src.IsMigrated))
            .ForMember(dest => dest.source, opt => opt.MapFrom(src => src.Source))
            .ForMember(dest => dest.player_name, opt => opt.MapFrom(src => src.PlayerName))
            .ForMember(dest => dest.world_id, opt => opt.MapFrom(src => src.WorldId))
            .ForMember(dest => dest.player_id, opt => opt.MapFrom(src => src.PlayerId));

        CreateMap<PlayerNameWorldHistoryDTO, Models.PlayerNameWorldHistory>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.updated))
            .ForMember(dest => dest.IsMigrated, opt => opt.MapFrom(src => src.is_migrated))
            .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.source))
            .ForMember(dest => dest.PlayerName, opt => opt.MapFrom(src => src.player_name))
            .ForMember(dest => dest.WorldId, opt => opt.MapFrom(src => src.world_id))
            .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.player_id));
    }
}
