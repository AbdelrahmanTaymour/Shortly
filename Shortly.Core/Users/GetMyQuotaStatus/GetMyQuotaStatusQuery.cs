using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Users.GetMyQuotaStatus;

public sealed record GetMyQuotaStatusQuery : IQuery<QuotaStatusResponse>;
