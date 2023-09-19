using AutoMapper;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class PlayerTagMappingProfile : Profile
{
    public PlayerTagMappingProfile()
    {
        this.CreateMap<PlayerTag, PlayerTagDTO>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.player_id, opt => opt.MapFrom(src => src.PlayerId))
            .ForMember(dest => dest.tag_id, opt => opt.MapFrom(src => src.TagId));

        this.CreateMap<PlayerTagDTO, PlayerTag>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.updated))
            .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.player_id))
            .ForMember(dest => dest.TagId, opt => opt.MapFrom(src => src.tag_id));
    }
}
