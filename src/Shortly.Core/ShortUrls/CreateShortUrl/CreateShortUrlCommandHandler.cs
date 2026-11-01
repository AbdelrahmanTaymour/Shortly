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
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateShortUrlCommand, string>
{
    private const int MaxShortCodeAttempts = 5;
    private const int UnlimitedClickLimit = -1;

    public async Task<Result<string>> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
    {
        var urlResult = OriginalUrl.Create(request.OriginalUrl);
        if (urlResult.IsFailure) return Result.Failure<string>(urlResult.Error);

        var utcNow = dateTimeProvider.UtcNow;

        var ownerResult = await ResolveOwnerAsync(utcNow, cancellationToken);
        if (ownerResult.IsFailure) return Result.Failure<string>(ownerResult.Error);

        var shortCodeResult = await ResolveShortCodeAsync(request.CustomShortCode, cancellationToken);
        if (shortCodeResult.IsFailure) return Result.Failure<string>(shortCodeResult.Error);

        var shortUrlResult = CreateShortUrl(request, ownerResult.Value, urlResult.Value, shortCodeResult.Value, utcNow);
        if (shortUrlResult.IsFailure) return Result.Failure<string>(shortUrlResult.Error);

        var owner = ownerResult.Value;

        owner.User?.Usage.IncrementLinksCreated(utcNow);
        owner.Organization?.Usage.IncrementLinksCreated(utcNow);

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

    private async Task<Result<OwnerContext>> ResolveOwnerAsync(
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        switch (userContext.OwnerType)
        {
            case enShortUrlOwnerType.User:
            {
                var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken);
                if (user is null) return Result.Failure<OwnerContext>(UserErrors.NotFound);

                var limits = await planLimitsProvider.GetLimitsAsync(user.SubscriptionPlanId, cancellationToken);
                if (!limits.HasUnlimitedLinks && user.Usage.HasReachedMonthlyLinkLimit(limits.MaxLinksPerMonth, utcNow))
                    return Result.Failure<OwnerContext>(ShortUrlErrors.MonthlyLinkLimitReached);

                return new OwnerContext(user);
            }

            case enShortUrlOwnerType.Organization:
            {
                if (userContext.OrganizationId is not { } organizationId ||
                    userContext.MemberId is not { } memberId)
                    return Result.Failure<OwnerContext>(ShortUrlErrors.InvalidOwnerType);

                var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
                if (organization is null) return Result.Failure<OwnerContext>(OrganizationErrors.NotFound);
                if (!organization.IsActive) return Result.Failure<OwnerContext>(OrganizationErrors.Inactive);

                var limits = await planLimitsProvider.GetLimitsAsync(organization.SubscriptionPlanId, cancellationToken);

                if (!limits.HasUnlimitedLinks && organization.Usage.HasReachedMonthlyLinkLimit(limits.MaxLinksPerMonth, utcNow))
                    return Result.Failure<OwnerContext>(ShortUrlErrors.MonthlyLinkLimitReached);

                return new OwnerContext(Organization: organization, OrganizationId: organizationId, MemberId: memberId);
            }

            case enShortUrlOwnerType.Anonymous:
            {
                if (string.IsNullOrWhiteSpace(userContext.AnonymousSessionId))
                    return Result.Failure<OwnerContext>(ShortUrlErrors.InvalidOwnerType);

                return new OwnerContext(AnonymousSessionId: userContext.AnonymousSessionId);
            }

            default:
                return Result.Failure<OwnerContext>(ShortUrlErrors.InvalidOwnerType);
        }
    }

    private Result<ShortUrl> CreateShortUrl(
        CreateShortUrlCommand request,
        OwnerContext owner,
        OriginalUrl originalUrl,
        ShortCode shortCode,
        DateTime utcNow)
    {
        var clickLimit = request.ClickLimit ?? UnlimitedClickLimit;
        var trackingEnabled = request.TrackingEnabled ?? true;
        var isPrivate = request.IsPrivate ?? false;

        if (owner.User is not null)
        {
            return ShortUrl.CreateForUser(
                owner.User.Id,
                originalUrl,
                shortCode,
                utcNow,
                clickLimit,
                trackingEnabled,
                isPrivate,
                request.ExpiresAt,
                request.Title,
                request.Description);
        }

        if (owner.Organization is not null && 
            owner is { OrganizationId: { } organizationId, MemberId: { } memberId })
        {
            return ShortUrl.CreateForOrganization(
                organizationId,
                memberId,
                originalUrl,
                shortCode,
                utcNow,
                clickLimit,
                trackingEnabled,
                isPrivate,
                request.ExpiresAt,
                request.Title,
                request.Description);
        }

        if (owner.AnonymousSessionId is not null)
        {
            return ShortUrl.CreateForAnonymous(
                owner.AnonymousSessionId,
                userContext.IpAddress,
                originalUrl,
                shortCode,
                utcNow);
        }

        return Result.Failure<ShortUrl>(ShortUrlErrors.InvalidOwnerType);
    }

    private async Task<Result<ShortCode>> ResolveShortCodeAsync(
        string? customCode,
        CancellationToken cancellationToken)
    {
        if (customCode is not null)
        {
            var result = ShortCode.Create(customCode);
            if (result.IsFailure) return result;

            var exists = await shortUrlRepository.ExistsByShortCodeAsync(result.Value, cancellationToken);

            return exists
                ? Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeAlreadyExists)
                : result;
        }

        for (var attempt = 0; attempt < MaxShortCodeAttempts; attempt++)
        {
            var candidate = shortCodeGenerator.Generate();

            var exists = await shortUrlRepository.ExistsByShortCodeAsync(candidate, cancellationToken);

            if (!exists) return candidate;
        }

        return Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeInvalidLength);
    }

    private sealed record OwnerContext(
        User? User = null,
        Organization? Organization = null,
        Guid? OrganizationId = null,
        Guid? MemberId = null,
        string? AnonymousSessionId = null);
}