using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Api.Controllers.Admin;

public sealed record ChangeOrganizationPlanRequest(enSubscriptionPlan SubscriptionPlanId);
