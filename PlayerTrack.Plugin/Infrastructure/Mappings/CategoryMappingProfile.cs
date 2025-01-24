using AutoMapper;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class CategoryMappingProfile : Profile
{
    public CategoryMappingProfile()
    {
        CreateMap<Category, CategoryDTO>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.rank, opt => opt.MapFrom(src => src.Rank))
            .ForMember(dest => dest.social_list_id, opt => opt.MapFrom(src => src.SocialListId));

        CreateMap<CategoryDTO, Category>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.updated))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Rank, opt => opt.MapFrom(src => src.rank))
            .ForMember(dest => dest.SocialListId, opt => opt.MapFrom(src => src.social_list_id));
    }
}
