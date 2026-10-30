using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Users.UpdateProfile;

public sealed record UpdateProfileCommand(string? Name, string? Bio, string? PhoneNumber, string? Website, string? Company, string? Location, string? Country, string? TimeZone) : ICommand;
