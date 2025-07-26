using AutoMapper;
using Shortly.Core.DTOs.UserDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserProfileDto>()
            .ReverseMap()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.TwoFactorSecret, opt => opt.Ignore())
            .ForMember(dest => dest.FailedLoginAttempts, opt => opt.Ignore())
            .ForMember(dest => dest.LockedUntil, opt => opt.Ignore())
            .ForMember(dest => dest.ShortUrls, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore())
            .ForMember(dest => dest.UrlTags, opt => opt.Ignore())
            .ForMember(dest => dest.OwnedOrganizations, opt => opt.Ignore())
            .ForMember(dest => dest.OrganizationMemberships, opt => opt.Ignore());

        CreateMap<UpdateUserProfileRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.Username, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.SubscriptionPlan, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.EmailVerified, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.MonthlyLinksCreated, opt => opt.Ignore())
            .ForMember(dest => dest.TotalLinksCreated, opt => opt.Ignore())
            .ForMember(dest => dest.MonthlyResetDate, opt => opt.Ignore())
            .ForMember(dest => dest.FailedLoginAttempts, opt => opt.Ignore())
            .ForMember(dest => dest.LockedUntil, opt => opt.Ignore())
            .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.TwoFactorSecret, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFromExpression(src => DateTime.UtcNow))
            .ForMember(dest => dest.ShortUrls, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore())
            .ForMember(dest => dest.UrlTags, opt => opt.Ignore())
            .ForMember(dest => dest.OwnedOrganizations, opt => opt.Ignore())
            .ForMember(dest => dest.OrganizationMemberships, opt => opt.Ignore());
    }
}