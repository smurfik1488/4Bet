using _4Bet.Application.DTOs;
using AutoMapper;
using _4Bet.Infrastructure.Domain;
namespace _4Bet.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserRegistrationDto,User>().ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src=>BCrypt.Net.BCrypt.HashPassword(src.Password)));
        CreateMap<SportEvent, SportEventDto>();
    }
}