using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Cache;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Exceptions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Core.ShortUrls.CreateShortUrl;

internal sealed class CreateShortUrlCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    ISubscriptionPlanLimitsProvider planLimitsProvider,
    IUserContext userContext,
    ShortCodeGenerator shortCodeGenerator,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateShortUrlCommand, string>
{
    public async Task<Result<string>> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
    {
        var urlResult = OriginalUrl.Create(request.OriginalUrl);
        if (urlResult.IsFailure) return Result.Failure<string>(urlResult.Error);

        var utcNow = dateTimeProvider.UtcNow;

        User? user = null;
        Organization? organization = null;

        switch (userContext.OwnerType)
        {
            case enShortUrlOwnerType.User:
                user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken);
                if (user is null) return Result.Failure<string>(UserErrors.NotFound);

                var userMaxLinks = await planLimitsProvider.GetMaxLinksPerMonthAsync(user.SubscriptionPlanId, cancellationToken);
                if (user.Usage.HasReachedMonthlyLinkLimit(userMaxLinks, utcNow))
                    return Result.Failure<string>(ShortUrlErrors.MonthlyLinkLimitReached);
                break;

            case enShortUrlOwnerType.Organization:
                organization = await organizationRepository.GetByIdAsync(userContext.OrganizationId!.Value, cancellationToken);
                if (organization is null) return Result.Failure<string>(OrganizationErrors.NotFound);
                if (!organization.IsActive) return Result.Failure<string>(OrganizationErrors.Inactive);

                var orgMaxLinks = await planLimitsProvider.GetMaxLinksPerMonthAsync(organization.SubscriptionPlanId, cancellationToken);
                if (organization.Usage.HasReachedMonthlyLinkLimit(orgMaxLinks, utcNow))
                    return Result.Failure<string>(ShortUrlErrors.MonthlyLinkLimitReached);
                break;

            case enShortUrlOwnerType.Anonymous:
                break;

            default:
                return Result.Failure<string>(ShortUrlErrors.OriginalUrlInvalid);
        }

        var shortCodeResult = await ResolveShortCodeAsync(request.CustomShortCode, cancellationToken);
        if (shortCodeResult.IsFailure) return Result.Failure<string>(shortCodeResult.Error);

        var shortUrlResult = userContext.OwnerType switch
        {
            enShortUrlOwnerType.User => ShortUrl.CreateForUser(
                userContext.UserId, urlResult.Value, shortCodeResult.Value, utcNow,
                request.ClickLimit ?? -1, request.TrackingEnabled ?? true, request.IsPrivate ?? false,
                request.ExpiresAt, request.Title, request.Description),

            enShortUrlOwnerType.Organization => ShortUrl.CreateForOrganization(
                userContext.OrganizationId!.Value, userContext.MemberId!.Value, urlResult.Value, shortCodeResult.Value, utcNow,
                request.ClickLimit ?? -1, request.TrackingEnabled ?? true, request.IsPrivate ?? false,
                request.ExpiresAt, request.Title, request.Description),

            enShortUrlOwnerType.Anonymous => ShortUrl.CreateForAnonymous(
                userContext.AnonymousSessionId!, userContext.IpAddress, urlResult.Value, shortCodeResult.Value, utcNow),

            _ => Result.Failure<ShortUrl>(ShortUrlErrors.OriginalUrlInvalid)
        };

        if (shortUrlResult.IsFailure) return Result.Failure<string>(shortUrlResult.Error);

        user?.Usage.IncrementLinksCreated(utcNow);
        organization?.Usage.IncrementLinksCreated(utcNow);

        shortUrlRepository.Add(shortUrlResult.Value);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<string>(ShortUrlErrors.ConcurrentUpdate);
        }

        return shortUrlResult.Value.ShortCode.Value;
    }

    private async Task<Result<ShortCode>> ResolveShortCodeAsync(string? customCode, CancellationToken cancellationToken)
    {
        if (customCode is not null)
        {
            var customResult = ShortCode.Create(customCode);
            if (customResult.IsFailure) return customResult;

            var exists = await shortUrlRepository.ExistsByShortCodeAsync(customResult.Value, cancellationToken);
            return exists ? Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeAlreadyExists) : customResult;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = shortCodeGenerator.Generate();
            if (!await shortUrlRepository.ExistsByShortCodeAsync(candidate, cancellationToken))
                return candidate;
        }

        return Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeInvalidLength);
    }
}