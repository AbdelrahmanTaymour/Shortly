using Shortly.Domain.Enums;

namespace Shortly.Domain.Entities;

public class Role
{
    public enUserRole Id { get; set; }
    public string RoleName { get; set; }
    public string? Description { get; set; }
    public long DefaultPermissions { get; set; }
}