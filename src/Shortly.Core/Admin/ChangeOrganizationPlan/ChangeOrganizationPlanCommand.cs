using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Core.Admin.ChangeOrganizationPlan;

public sealed record ChangeOrganizationPlanCommand(Guid OrganizationId, enSubscriptionPlan SubscriptionPlanId) : ICommand;
