using System.Collections.Generic;
using AutoMapper;
using Newtonsoft.Json;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class SocialListMappingProfile : Profile
{
    public SocialListMappingProfile()
    {
        this.CreateMap<SocialList, SocialListDTO>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.content_id, opt => opt.MapFrom(src => src.ContentId))
            .ForMember(dest => dest.list_type, opt => opt.MapFrom(src => src.ListType))
            .ForMember(dest => dest.list_number, opt => opt.MapFrom(src => src.ListNumber))
            .ForMember(dest => dest.data_center_id, opt => opt.MapFrom(src => src.DataCenterId))
            .ForMember(dest => dest.page_count, opt => opt.MapFrom(src => src.PageCount))
            .ForMember(dest => dest.add_players, opt => opt.MapFrom(src => src.AddPlayers))
            .ForMember(dest => dest.sync_with_category, opt => opt.MapFrom(src => src.SyncWithCategory))
            .ForMember(dest => dest.default_category_id, opt => opt.MapFrom(src => src.DefaultCategoryId))
            .ForMember(dest => dest.page_last_updated, opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.PageLastUpdated)));

        this.CreateMap<SocialListDTO, SocialList>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.updated))
            .ForMember(dest => dest.ContentId, opt => opt.MapFrom(src => src.content_id))
            .ForMember(dest => dest.ListType, opt => opt.MapFrom(src => src.list_type))
            .ForMember(dest => dest.ListNumber, opt => opt.MapFrom(src => src.list_number))
            .ForMember(dest => dest.DataCenterId, opt => opt.MapFrom(src => src.data_center_id))
            .ForMember(dest => dest.PageCount, opt => opt.MapFrom(src => src.page_count))
            .ForMember(dest => dest.AddPlayers, opt => opt.MapFrom(src => src.add_players))
            .ForMember(dest => dest.SyncWithCategory, opt => opt.MapFrom(src => src.sync_with_category))
            .ForMember(dest => dest.DefaultCategoryId, opt => opt.MapFrom(src => src.default_category_id))
            .ForMember(dest => dest.PageLastUpdated, opt => opt.MapFrom(src => JsonConvert.DeserializeObject<Dictionary<ushort, long>>(src.page_last_updated)));
    }
}