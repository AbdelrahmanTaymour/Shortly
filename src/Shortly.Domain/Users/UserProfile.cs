using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Users;

public sealed class UserProfile
{
    private UserProfile(Guid userId) => UserId = userId;
    private UserProfile() { }

    public Guid UserId { get; private set; }
    public string? Name { get; private set; }
    public string? Bio { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? ProfilePictureUrl { get; private set; }
    public string? Website { get; private set; }
    public string? Company { get; private set; }
    public string? Location { get; private set; }
    public string? Country { get; private set; }
    public string? TimeZone { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    internal static UserProfile CreateDefault(Guid userId) => new(userId);

    public Result Update(
        string? name, string? bio, string? phoneNumber, string? website,
        string? company, string? location, string? country, string? timeZone, DateTime utcNow)
    {
        Name = name;
        Bio = bio;
        PhoneNumber = phoneNumber;
        Website = website;
        Company = company;
        Location = location;
        Country = country;
        TimeZone = timeZone;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    /// <summary>
    /// Partial update: a null argument keeps the current value, a blank one clears it, anything else replaces it.
    /// </summary>
    public Result Patch(
        string? name, string? bio, string? phoneNumber, string? website,
        string? company, string? location, string? country, string? timeZone, DateTime utcNow)
    {
        Name = Resolve(name, Name);
        Bio = Resolve(bio, Bio);
        PhoneNumber = Resolve(phoneNumber, PhoneNumber);
        Website = Resolve(website, Website);
        Company = Resolve(company, Company);
        Location = Resolve(location, Location);
        Country = Resolve(country, Country);
        TimeZone = Resolve(timeZone, TimeZone);
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    private static string? Resolve(string? requested, string? current) =>
        requested is null ? current : string.IsNullOrWhiteSpace(requested) ? null : requested.Trim();

    public Result UpdateName(string? name, DateTime utcNow)
    {
        Name = name;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result UpdateProfilePicture(string? url, DateTime utcNow)
    {
        ProfilePictureUrl = url;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }
}