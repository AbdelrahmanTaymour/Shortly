using AutoMapper;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

public class ShortUrlMappingProfile: Profile
{
    public ShortUrlMappingProfile()
    {
        CreateMap<ShortUrl, ShortUrlResponse>().ReverseMap();
        CreateMap<ShortUrl, ShortUrlRequest>().ReverseMap();
        CreateMap<ShortUrl, StatusShortUrlResponse>().ReverseMap();
    }
}


/*
 To Ignore
 * CreateMap<ShortUrl, ShortUrlDto>()
   .ForMember(dest => dest.ShortCode, opt => opt.Ignore())
   .ReverseMap();
   
   
   To map
   CreateMap<ShortUrl, ShortUrlDto>()
   .ForMember(dest => dest.ShortCode, opt => opt.ShortCode)
 */