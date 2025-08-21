namespace Shortly.Domain.Enums;

public enum enUserRole : byte
{
    Viewer = 1,
    Member = 2,
    TeamManager = 3,
    OrgAdmin = 4,
    OrgOwner = 5,
    SuperAdmin = 6,
    SystemAdmin = 7
}