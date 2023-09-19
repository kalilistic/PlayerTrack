using AutoMapper;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class LodestoneLookupMappingProfile : Profile
{
        public LodestoneLookupMappingProfile()
        {
            this.CreateMap<LodestoneLookup, LodestoneLookupDTO>()
                .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.created, opt => opt.MapFrom(src => src.Created))
                .ForMember(dest => dest.updated, opt => opt.MapFrom(src => src.Updated))
                .ForMember(dest => dest.player_id, opt => opt.MapFrom(src => src.PlayerId))
                .ForMember(dest => dest.player_name, opt => opt.MapFrom(src => src.PlayerName))
                .ForMember(dest => dest.world_name, opt => opt.MapFrom(src => src.WorldName))
                .ForMember(dest => dest.lookup_status, opt => opt.MapFrom(src => src.LodestoneStatus))
                .ForMember(dest => dest.failure_count, opt => opt.MapFrom(src => src.FailureCount))
                .ForMember(dest => dest.lodestone_id, opt => opt.MapFrom(src => src.LodestoneId));

            this.CreateMap<LodestoneLookupDTO, LodestoneLookup>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
                .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.created))
                .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.updated))
                .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.player_id))
                .ForMember(dest => dest.PlayerName, opt => opt.MapFrom(src => src.player_name))
                .ForMember(dest => dest.WorldName, opt => opt.MapFrom(src => src.world_name))
                .ForMember(dest => dest.LodestoneStatus, opt => opt.MapFrom(src => src.lookup_status))
                .ForMember(dest => dest.FailureCount, opt => opt.MapFrom(src => src.failure_count))
                .ForMember(dest => dest.LodestoneId, opt => opt.MapFrom(src => src.lodestone_id));
        }
}
