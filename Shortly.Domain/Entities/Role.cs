namespace Shortly.Domain.Entities;

public class Role
{
    public byte Id { get; set; }
    public string RoleName { get; set; }
    public string? Description { get; set; }
    public long DefaultPermissions { get; set; }
}