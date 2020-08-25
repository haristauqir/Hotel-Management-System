using AutoMapper;
using Identity.API.Models;
using Identity.API.Models.Dtos;

namespace Identity.API.Helpers
{
  public class AutoMapperProfiles : Profile
  {
    public AutoMapperProfiles()
    {
    CreateMap<RegisterDto, AppUser>()
      .ForPath(dest => dest.UserName, opt =>
      {
        opt.MapFrom(src => src.Email); 
      });
    }
  }
}