using AutoMapper;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class SocialListMemberMappingProfile : Profile
{
    public SocialListMemberMappingProfile()
    {
        this.CreateMap<SocialListMember, SocialListMemberDTO>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.content_id, opt => opt.MapFrom(src => src.ContentId))
            .ForMember(dest => dest.key, opt => opt.MapFrom(src => src.Key))
            .ForMember(dest => dest.name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.world_id, opt => opt.MapFrom(src => src.WorldId))
            .ForMember(dest => dest.page_number, opt => opt.MapFrom(src => src.PageNumber))
            .ForMember(dest => dest.social_list_id, opt => opt.MapFrom(src => src.SocialListId));

        this.CreateMap<SocialListMemberDTO, SocialListMember>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.updated))
            .ForMember(dest => dest.ContentId, opt => opt.MapFrom(src => src.content_id))
            .ForMember(dest => dest.Key, opt => opt.MapFrom(src => src.key))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.WorldId, opt => opt.MapFrom(src => src.world_id))
            .ForMember(dest => dest.PageNumber, opt => opt.MapFrom(src => src.page_number))
            .ForMember(dest => dest.SocialListId, opt => opt.MapFrom(src => src.social_list_id));
    }
}