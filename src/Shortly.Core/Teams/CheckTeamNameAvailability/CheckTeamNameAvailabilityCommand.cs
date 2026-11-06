using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Teams.CheckTeamNameAvailability;

/// <summary>
/// Asks whether a team name is free in the organization, so a UI can warn before a team is created or renamed.
/// Pass <paramref name="ExcludeTeamId"/> when renaming, so a team does not clash with its own current name.
/// </summary>
public sealed record CheckTeamNameAvailabilityCommand(Guid OrganizationId, string Name, Guid? ExcludeTeamId = null)
    : ICommand<bool>;
