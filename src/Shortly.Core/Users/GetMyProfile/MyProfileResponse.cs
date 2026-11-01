namespace Shortly.Core.Users.GetMyProfile;

public sealed class MyProfileResponse
{
    public Guid Id { get; init; }

    public required string Email { get; init; }

    public required string Username { get; init; }

    public bool IsEmailConfirmed { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public string? Name { get; init; }

    public string? Bio { get; init; }

    public string? PhoneNumber { get; init; }

    public string? ProfilePictureUrl { get; init; }

    public string? Website { get; init; }

    public string? Company { get; init; }

    public string? Location { get; init; }

    public string? Country { get; init; }

    public string? TimeZone { get; init; }
}