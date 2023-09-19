using AutoMapper;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class PlayerEncounterMappingProfile : Profile
{
    public PlayerEncounterMappingProfile()
    {
        this.CreateMap<PlayerEncounter, PlayerEncounterDTO>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.ended, opt => opt.MapFrom(src => src.Ended))
            .ForMember(dest => dest.job_id, opt => opt.MapFrom(src => src.JobId))
            .ForMember(dest => dest.job_lvl, opt => opt.MapFrom(src => src.JobLvl))
            .ForMember(dest => dest.player_id, opt => opt.MapFrom(src => src.PlayerId))
            .ForMember(dest => dest.encounter_id, opt => opt.MapFrom(src => src.EncounterId));

        this.CreateMap<PlayerEncounterDTO, PlayerEncounter>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.updated))
            .ForMember(dest => dest.Ended, opt => opt.MapFrom(src => src.ended))
            .ForMember(dest => dest.JobId, opt => opt.MapFrom(src => src.job_id))
            .ForMember(dest => dest.JobLvl, opt => opt.MapFrom(src => src.job_lvl))
            .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.player_id))
            .ForMember(dest => dest.EncounterId, opt => opt.MapFrom(src => src.encounter_id));
    }
}
