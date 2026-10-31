using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Admin.ResetAllUsersUsage;

public sealed record ResetAllUsersUsageCommand : ICommand<int>;
