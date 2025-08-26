namespace Shortly.Core.Models;

public record InvitationClaims
{
    public Guid InvitationId { get; set; } 
    public required string Email { get; set; }
}