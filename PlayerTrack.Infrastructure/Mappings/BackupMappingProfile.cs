using AutoMapper;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class BackupMappingProfile : Profile
{
    public BackupMappingProfile()
    {
        this.CreateMap<Backup, BackupDTO>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.backup_type, opt => opt.MapFrom(src => (int)src.BackupType))
            .ForMember(dest => dest.name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.size, opt => opt.MapFrom(src => src.Size))
            .ForMember(dest => dest.is_restorable, opt => opt.MapFrom(src => src.IsRestorable))
            .ForMember(dest => dest.is_protected, opt => opt.MapFrom(src => src.IsProtected))
            .ForMember(dest => dest.notes, opt => opt.MapFrom(src => src.Notes));

        this.CreateMap<BackupDTO, Backup>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.updated))
            .ForMember(dest => dest.BackupType, opt => opt.MapFrom(src => (BackupType)src.backup_type))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Size, opt => opt.MapFrom(src => src.size))
            .ForMember(dest => dest.IsRestorable, opt => opt.MapFrom(src => src.is_restorable))
            .ForMember(dest => dest.IsProtected, opt => opt.MapFrom(src => src.is_protected))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.notes));
    }
}
