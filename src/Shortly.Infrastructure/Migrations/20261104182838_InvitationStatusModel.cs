using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InvitationStatusModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The column type does not change; only the meaning of the stored values does.
            // Old: 1 Pending, 2 EmailSent, 3 Failure, 4 UserClicked, 5 Registered, 6 Rejected.
            // New: 1 Pending, 2 Accepted, 3 Expired, 4 Cancelled.
            // One statement, so the 2 <-> 5 swap cannot collide. Pending invitations past their expiry become Expired.
            migrationBuilder.Sql("""
                UPDATE organization_invitations
                SET status = CASE
                    WHEN status IN (1, 2, 3, 4) AND expires_at <= CURRENT_TIMESTAMP THEN 3
                    WHEN status IN (1, 2, 3, 4) THEN 1
                    WHEN status = 5 THEN 2
                    WHEN status = 6 THEN 4
                    ELSE status
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Accepted -> Registered (5), Cancelled -> Rejected (6), Expired -> Pending (1, still past its expiry date).
            migrationBuilder.Sql("""
                UPDATE organization_invitations
                SET status = CASE
                    WHEN status = 2 THEN 5
                    WHEN status = 4 THEN 6
                    WHEN status = 3 THEN 1
                    ELSE status
                END;
                """);
        }
    }
}
