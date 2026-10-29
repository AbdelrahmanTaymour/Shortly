using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Admin.ResetUserUsage;

public sealed record ResetUserUsageCommand(Guid UserId) : ICommand;
