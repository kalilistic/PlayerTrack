using AutoMapper;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class ArchiveRecordMappingProfile : Profile
{
    public ArchiveRecordMappingProfile()
    {
        CreateMap<ArchiveRecord, ArchiveRecordDTO>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.archive_type, opt => opt.MapFrom(src => src.ArchiveType))
            .ForMember(dest => dest.data, opt => opt.MapFrom(src => src.Data));

        CreateMap<ArchiveRecordDTO, ArchiveRecord>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.updated))
            .ForMember(dest => dest.ArchiveType, opt => opt.MapFrom(src => src.archive_type))
            .ForMember(dest => dest.Data, opt => opt.MapFrom(src => src.data));
    }
}
