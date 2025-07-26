# User Management System Documentation

## Overview

This comprehensive User management system provides robust functionality for user profile management, authentication security, email verification, two-factor authentication, usage tracking, and administrative operations. The system is built using clean architecture principles with proper separation of concerns.

## Features

### 🔐 Security Features
- **Password Management**: Secure password hashing with BCrypt, change password, forgot/reset password functionality
- **Account Security**: Failed login attempt tracking, automatic account locking, manual lock/unlock
- **Two-Factor Authentication**: TOTP-based 2FA with QR codes, backup codes, and secure setup/disable process
- **Email Verification**: Email confirmation system with token-based verification

### 👤 Profile Management
- **User Profiles**: Complete profile management with name, email, username, timezone, profile picture
- **Subscription Plans**: Support for Free, Starter, Professional, and Enterprise plans
- **User Roles**: Hierarchical role system (Viewer, StandardUser, PowerUser, TeamManager, OrgAdmin, OrgOwner, SuperAdmin)
- **Usage Tracking**: Monthly and total link creation tracking with subscription-based limits

### 🔍 Advanced Features
- **Search & Pagination**: Comprehensive user search with filtering by role, subscription, status
- **Bulk Operations**: Mass activation/deactivation, subscription plan updates
- **Analytics**: User statistics and reporting for administrators
- **Organization Support**: Multi-organization membership and ownership

## Architecture

### Domain Layer
- **User Entity**: Main user entity with comprehensive properties
- **UrlTag Entity**: Tagging system for user-created URLs
- **Enums**: User roles and subscription plans

### Core Layer
- **IUserService**: Comprehensive service interface
- **IUserRepository**: Data access interface
- **DTOs**: Request/response data transfer objects
- **Validators**: FluentValidation rules for all operations
- **Mappers**: AutoMapper profiles for entity-DTO mapping

### Infrastructure Layer
- **UserRepository**: Complete data access implementation
- **Database Configuration**: Entity Framework Core setup

### API Layer
- **UserController**: RESTful API endpoints with proper authorization

## API Endpoints

### Profile Management
```http
GET    /api/user/profile              # Get current user profile
PUT    /api/user/profile              # Update user profile
DELETE /api/user/account              # Delete user account
```

### Password Management
```http
POST /api/user/change-password        # Change password
POST /api/user/forgot-password        # Initiate password reset
POST /api/user/reset-password         # Reset password with token
```

### Email Verification
```http
POST /api/user/send-email-verification     # Send verification email
POST /api/user/verify-email               # Verify email with token
POST /api/user/resend-email-verification  # Resend verification email
```

### Two-Factor Authentication
```http
POST /api/user/2fa/setup                   # Setup 2FA (get QR code)
POST /api/user/2fa/enable                  # Enable 2FA with verification
POST /api/user/2fa/disable                 # Disable 2FA
POST /api/user/2fa/generate-backup-codes   # Generate backup codes
```

### Usage Statistics
```http
GET /api/user/usage                   # Get usage statistics
```

### Admin Operations
```http
GET  /api/user/all                    # Search users (admin only)
POST /api/user/{id}/lock              # Lock user account (admin only)
POST /api/user/{id}/unlock            # Unlock user account (admin only)
POST /api/user/{id}/activate          # Activate user (admin only)
POST /api/user/{id}/deactivate        # Deactivate user (admin only)
GET  /api/user/analytics              # User analytics (admin only)
```

## Usage Examples

### Profile Update
```json
PUT /api/user/profile
{
  "name": "John Doe",
  "timeZone": "America/New_York",
  "profilePictureUrl": "https://example.com/avatar.jpg"
}
```

### Password Change
```json
POST /api/user/change-password
{
  "currentPassword": "oldPassword123!",
  "newPassword": "newPassword456@",
  "confirmPassword": "newPassword456@"
}
```

### Two-Factor Authentication Setup
```json
POST /api/user/2fa/setup
Response:
{
  "qrCodeUri": "otpauth://totp/Shortly:user@example.com?secret=...&issuer=Shortly",
  "manualEntryKey": "JBSWY3DPEHPK3PXP",
  "backupCodes": ["123456", "789012", ...]
}
```

### User Search (Admin)
```http
GET /api/user/all?searchTerm=john&role=StandardUser&isActive=true&page=1&pageSize=10
```

## Security Considerations

### Password Security
- Passwords are hashed using BCrypt
- Minimum 8 characters with complexity requirements
- Support for password history (TODO: implement in future)

### Account Protection
- Automatic lockout after 5 failed login attempts
- 30-minute lockout duration (configurable)
- Admin can manually lock/unlock accounts

### Two-Factor Authentication
- TOTP-based (Time-based One-Time Password)
- Secure secret generation
- Backup codes for account recovery
- QR code generation for easy setup

### Token Security
- Secure token generation for password reset
- Email verification tokens
- Token expiration and validation

## Subscription Limits

| Plan         | Monthly Links | Features                    |
|--------------|---------------|-----------------------------|
| Free         | 10            | Basic features              |
| Starter      | 100           | Enhanced features           |
| Professional | 1,000         | Advanced features           |
| Enterprise   | Unlimited     | Full feature set            |

## Error Handling

The system implements comprehensive error handling:
- Validation errors with detailed messages
- Security-aware error responses (don't leak sensitive info)
- Proper HTTP status codes
- Structured error responses

## Testing

### Unit Tests (TODO)
- Service layer tests
- Repository tests
- Validation tests

### Integration Tests (TODO)
- API endpoint tests
- Database integration tests
- Authentication flow tests

## Future Enhancements

### Security
- [ ] Password history tracking
- [ ] Advanced 2FA options (SMS, hardware tokens)
- [ ] Session management
- [ ] Login history and audit trails

### Features
- [ ] User preferences and settings
- [ ] Profile picture upload
- [ ] Social login integration
- [ ] Email notifications system
- [ ] Password reset token storage in database
- [ ] Proper TOTP implementation with libraries

### Performance
- [ ] Caching for frequently accessed data
- [ ] Optimize database queries
- [ ] Rate limiting for sensitive operations

### Monitoring
- [ ] Security event logging
- [ ] Performance metrics
- [ ] Health checks

## Dependencies

- **BCrypt.Net**: Password hashing
- **FluentValidation**: Input validation
- **AutoMapper**: Object mapping
- **Entity Framework Core**: Data access
- **Microsoft.AspNetCore.Authentication.JwtBearer**: JWT authentication

## Configuration

```json
{
  "App": {
    "Name": "Shortly"
  },
  "Jwt": {
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

## Getting Started

1. **Register Dependencies**: Services are automatically registered via `AddCore()` and `AddInfrastructure()`
2. **Run Migrations**: Update database with new User entity structure
3. **Configure Authentication**: Ensure JWT authentication is properly configured
4. **Test Endpoints**: Use the provided API endpoints for user management

## Support

For questions or issues, please refer to the codebase documentation or contact the development team.