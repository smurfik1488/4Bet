using _4Bet.Application.DTOs;
using AutoMapper;
using _4Bet.Infrastructure.Domain;
namespace _4Bet.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserRegistrationDto, User>()
            .ForMember(dest => dest.PasswordHash, 
                opt => opt.MapFrom(src => BCrypt.Net.BCrypt.HashPassword(src.Password)))
            .ForMember(dest => dest.Birthday,
                opt => opt.MapFrom(src =>
                    src.Birthday.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(src.Birthday, DateTimeKind.Utc)
                        : src.Birthday.ToUniversalTime()))
            .ForMember(dest => dest.UpdatedAt,
                opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Wallet, 
                opt => opt.MapFrom(_ => new Wallet
                {
                    Balance = 5000m,
                    UpdatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                }));

        CreateMap<SportEvent, SportEventDto>();
    }
}