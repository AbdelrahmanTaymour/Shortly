using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.Context;

public class UserContext
{
    public User User { get; set; }
    public enPermissions Permissions { get; set; }
    public Dictionary<string, int> Limits { get; set; }
    public Organization Organization { get; set; }
}