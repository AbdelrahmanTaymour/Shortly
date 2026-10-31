using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Analytics.GetShortUrlPortfolioSummary;

public sealed record GetShortUrlPortfolioSummaryQuery(Guid? UserId, Guid? OrganizationId)
    : IQuery<ShortUrlPortfolioSummaryResponse>;