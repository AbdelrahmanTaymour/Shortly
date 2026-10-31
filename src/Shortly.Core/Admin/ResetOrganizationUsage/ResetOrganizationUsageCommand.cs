using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Admin.ResetOrganizationUsage;

public sealed record ResetOrganizationUsageCommand(Guid OrganizationId) : ICommand;
