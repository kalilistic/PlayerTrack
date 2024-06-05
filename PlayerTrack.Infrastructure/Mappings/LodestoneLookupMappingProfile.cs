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
                .ForMember(dest => dest.world_id, opt => opt.MapFrom(src => src.WorldId))
                .ForMember(dest => dest.updated_player_name, opt => opt.MapFrom(src => src.UpdatedPlayerName))
                .ForMember(dest => dest.updated_world_id, opt => opt.MapFrom(src => src.UpdatedWorldId))
                .ForMember(dest => dest.lookup_status, opt => opt.MapFrom(src => src.LodestoneStatus))
                .ForMember(dest => dest.failure_count, opt => opt.MapFrom(src => src.FailureCount))
                .ForMember(dest => dest.lodestone_id, opt => opt.MapFrom(src => src.LodestoneId))
                .ForMember(dest => dest.prerequisite_lookup_id, opt => opt.MapFrom(src => src.PrerequisiteLookupId))
                .ForMember(dest => dest.lookup_type, opt => opt.MapFrom(src => src.LodestoneLookupType))
                .ForMember(dest => dest.is_done, opt => opt.MapFrom(src => src.IsDone));

            this.CreateMap<LodestoneLookupDTO, LodestoneLookup>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
                .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.created))
                .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.updated))
                .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.player_id))
                .ForMember(dest => dest.PlayerName, opt => opt.MapFrom(src => src.player_name))
                .ForMember(dest => dest.WorldId, opt => opt.MapFrom(src => src.world_id))
                .ForMember(dest => dest.UpdatedPlayerName, opt => opt.MapFrom(src => src.updated_player_name))
                .ForMember(dest => dest.UpdatedWorldId, opt => opt.MapFrom(src => src.updated_world_id))
                .ForMember(dest => dest.LodestoneStatus, opt => opt.MapFrom(src => src.lookup_status))
                .ForMember(dest => dest.FailureCount, opt => opt.MapFrom(src => src.failure_count))
                .ForMember(dest => dest.LodestoneId, opt => opt.MapFrom(src => src.lodestone_id))
                .ForMember(dest => dest.PrerequisiteLookupId, opt => opt.MapFrom(src => src.prerequisite_lookup_id))
                .ForMember(dest => dest.LodestoneLookupType, opt => opt.MapFrom(src => src.lookup_type))
                .ForMember(dest => dest.IsDone, opt => opt.MapFrom(src => src.is_done));
        }
}
