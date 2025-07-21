# Enhanced User Management System with Role-Based Access Control (RBAC)

## Overview

This document outlines the comprehensive user management system implemented for the Shortly URL shortening service, featuring enterprise-grade role-based access control (RBAC), security features, and user administration capabilities.

## 🎯 **Key Features Implemented**

### **1. Enhanced User Entity**
- ✅ **Comprehensive Profile Management**: Full name, bio, company, job title, contact info
- ✅ **Security Features**: 2FA, password policies, account lockout, email verification
- ✅ **Activity Tracking**: Login history, session management, activity logs
- ✅ **GDPR Compliance**: Privacy consent, data processing agreements, terms acceptance
- ✅ **Subscription Integration**: Tier-based access control and billing integration
- ✅ **Audit Trail**: Complete change tracking and user activity monitoring

### **2. Role-Based Access Control (RBAC)**
- ✅ **Hierarchical Roles**: SuperAdmin → Admin → Moderator → Users
- ✅ **Granular Permissions**: 50+ permissions across 10 categories
- ✅ **Dynamic Assignment**: Temporary roles, expiration dates, bulk operations
- ✅ **Permission Inheritance**: Roles inherit permissions from higher roles
- ✅ **Custom Roles**: Create and manage custom roles for specific needs

### **3. Security & Compliance**
- ✅ **Multi-Factor Authentication**: TOTP, recovery codes, device trust
- ✅ **Session Management**: Device tracking, concurrent session control
- ✅ **Risk Assessment**: Login anomaly detection, security scoring
- ✅ **Account Protection**: Lockout policies, password requirements, rate limiting
- ✅ **Audit Logging**: Comprehensive activity tracking and security events

## 🏗️ **System Architecture**

### **Entity Structure**

```
User (Enhanced)
├── Basic Info: FirstName, LastName, Email, Username
├── Profile: Bio, Company, Location, ProfileImage
├── Security: 2FA, Password, Email Verification
├── Status: Active/Suspended/Banned/Deleted
├── Subscription: Tier, Billing Info
├── Audit: Created/Updated/LastLogin
└── Navigation: Roles, Sessions, Activities

Role
├── Properties: Name, Description, Priority, Color
├── Permissions: Many-to-Many with Permission
├── Users: Many-to-Many with User (via UserRole)
└── Metadata: System/Custom, Active/Inactive

Permission
├── Properties: Name, Category, Type, Description
├── Organization: Category, SubCategory, SortOrder
└── Roles: Many-to-Many with Role

UserRole (Junction Table)
├── Assignment: UserId, RoleId, AssignedBy
├── Temporal: AssignedAt, ExpiresAt, IsActive
└── Metadata: Notes, Audit Trail

Additional Entities:
├── UserSession: Device tracking, security monitoring
├── UserActivity: Action logging, audit trail
├── UserLoginHistory: Login attempts, security analysis
└── SecurityAudit: Risk assessment, compliance
```

## 🔐 **Default Roles & Permissions**

### **Role Hierarchy**

| Role | Priority | Description | Users |
|------|----------|-------------|-------|
| **SuperAdmin** | 1000 | Complete system control | System administrators |
| **Admin** | 900 | User & system management | IT administrators |
| **Moderator** | 800 | Content & user moderation | Support staff |
| **EnterpriseUser** | 700 | Advanced features & domains | Enterprise customers |
| **PremiumUser** | 600 | Enhanced features | Premium subscribers |
| **User** | 500 | Standard features | Regular users |
| **ApiUser** | 400 | API-only access | Integration accounts |
| **ReadOnlyUser** | 300 | View-only access | Limited access users |
| **Guest** | 100 | Minimal access | Demo/trial users |

### **Permission Categories**

#### **1. URL Management (9 permissions)**
- `urls.create` - Create new URLs
- `urls.read` - View URL information
- `urls.update` - Modify existing URLs
- `urls.delete` - Remove URLs
- `urls.manage_own` - Manage personal URLs
- `urls.manage_all` - Manage all URLs
- `urls.create_custom` - Create vanity URLs
- `urls.create_bulk` - Bulk URL operations
- `urls.export` - Export URL data

#### **2. Analytics (6 permissions)**
- `analytics.view` - Basic analytics access
- `analytics.view_detailed` - Detailed analytics
- `analytics.view_own` - Personal analytics
- `analytics.view_all` - System-wide analytics
- `analytics.export` - Export analytics
- `analytics.realtime` - Real-time data

#### **3. User Management (7 permissions)**
- `users.create` - Create new users
- `users.read` - View user information
- `users.update` - Modify users
- `users.delete` - Remove users
- `users.manage_roles` - Assign/remove roles
- `users.view_activity` - View user activities
- `users.impersonate` - Login as other users

#### **4. Administration (7 permissions)**
- `admin.view_settings` - View system settings
- `admin.manage_settings` - Modify system settings
- `admin.view_logs` - Access system logs
- `admin.view_health` - System health monitoring
- `admin.manage_backups` - Backup operations
- `admin.manage_cache` - Cache management
- `admin.manage_logs` - Log management

#### **5. Security (4 permissions)**
- `security.view_logs` - View security events
- `security.manage_settings` - Security configuration
- `security.ban_users` - Ban/suspend users
- `security.review_activity` - Review suspicious activity

## 🛡️ **Authorization Attributes**

### **Permission-Based Authorization**
```csharp
[RequirePermission("urls.create")]
public async Task<IActionResult> CreateUrl([FromBody] ShortUrlRequest request)

[RequirePermission(true, "users.read", "users.update")] // Requires ALL permissions
public async Task<IActionResult> ManageUser(Guid id)

[RequirePermission("analytics.view", "analytics.export")] // Requires ANY permission
public async Task<IActionResult> ViewAnalytics()
```

### **Role-Based Authorization**
```csharp
[RequireRole("Admin")]
public async Task<IActionResult> AdminOperation()

[RequireAdmin] // Shorthand for SuperAdmin or Admin
public async Task<IActionResult> AdminOnlyOperation()

[RequireSuperAdmin] // SuperAdmin only
public async Task<IActionResult> SystemOperation()
```

### **Subscription-Based Authorization**
```csharp
[RequireSubscriptionTier(SubscriptionTier.Premium)]
public async Task<IActionResult> PremiumFeature()

[RequireSubscriptionTier(SubscriptionTier.Enterprise)]
public async Task<IActionResult> EnterpriseFeature()
```

### **Resource Ownership Authorization**
```csharp
[RequireResourceOwnership("url", "shortCode")]
public async Task<IActionResult> UpdateUrl(string shortCode)

[RequireResourceOwnership("user", "id")]
public async Task<IActionResult> UpdateProfile(Guid id)
```

## 📋 **API Endpoints**

### **User Management**

| Method | Endpoint | Permission Required | Description |
|--------|----------|-------------------|-------------|
| POST | `/api/usermanagement` | `users.create` | Create new user |
| GET | `/api/usermanagement/{id}` | `users.read` | Get user details |
| GET | `/api/usermanagement` | `users.read` | List users (paginated) |
| PUT | `/api/usermanagement/{id}` | `users.update` | Update user |
| DELETE | `/api/usermanagement/{id}` | `users.delete` | Delete user (soft) |
| POST | `/api/usermanagement/{id}/activate` | `users.update` | Activate user |
| POST | `/api/usermanagement/{id}/suspend` | `security.ban_users` | Suspend user |
| POST | `/api/usermanagement/{id}/ban` | `security.ban_users` | Ban user |

### **Role & Permission Management**

| Method | Endpoint | Permission Required | Description |
|--------|----------|-------------------|-------------|
| GET | `/api/usermanagement/{id}/roles` | `roles.view` | Get user roles |
| POST | `/api/usermanagement/{id}/roles` | `roles.assign` | Assign roles |
| DELETE | `/api/usermanagement/{id}/roles/{roleId}` | `roles.assign` | Remove role |

### **Activity & Sessions**

| Method | Endpoint | Permission Required | Description |
|--------|----------|-------------------|-------------|
| GET | `/api/usermanagement/{id}/activity` | `users.view_activity` | User activity log |
| GET | `/api/usermanagement/{id}/sessions` | `users.view_activity` | Active sessions |
| DELETE | `/api/usermanagement/sessions/{sessionId}` | `users.view_activity` | Terminate session |
| GET | `/api/usermanagement/{id}/login-history` | `users.view_activity` | Login history |

### **Bulk Operations**

| Method | Endpoint | Permission Required | Description |
|--------|----------|-------------------|-------------|
| POST | `/api/usermanagement/bulk` | Admin role | Bulk user operations |

## 🔧 **Implementation Guide**

### **Step 1: Database Migration**

```csharp
// Add new entities to DbContext
public class ShortlyDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<UserActivity> UserActivities { get; set; }
    public DbSet<UserLoginHistory> LoginHistory { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure many-to-many relationships
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });
            
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });
    }
}
```

### **Step 2: Service Registration**

```csharp
// In Program.cs or Startup.cs
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
```

### **Step 3: Initialize Default Data**

```csharp
// Seed default roles and permissions
public async Task InitializeDefaultDataAsync()
{
    await _roleManagementService.InitializeDefaultRolesAndPermissionsAsync();
    await _roleManagementService.SeedDefaultDataAsync();
}
```

### **Step 4: Secure Controllers**

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication
[RequirePermission("urls.manage_own")] // Require specific permission
public class ShortUrlController : ControllerBase
{
    [HttpPost]
    [RequireSubscriptionTier(SubscriptionTier.Premium)] // Premium feature
    public async Task<IActionResult> CreateCustomUrl([FromBody] ShortUrlRequest request)
    {
        // Implementation
    }
}
```

## 🔍 **Usage Examples**

### **1. Creating a User with Roles**

```csharp
var createRequest = new CreateUserRequest(
    FirstName: "John",
    LastName: "Doe",
    Email: "john.doe@company.com",
    Username: "johndoe",
    Password: "SecurePassword123!",
    Roles: new List<string> { "User", "PremiumUser" },
    SendWelcomeEmail: true
);

var user = await _userManagementService.CreateUserAsync(createRequest);
```

### **2. Checking User Permissions**

```csharp
var userId = Guid.Parse("...");

// Check specific permission
var canCreateUrls = await _authorizationService.HasPermissionAsync(userId, "urls.create");

// Check multiple permissions (any)
var hasAnalyticsAccess = await _authorizationService.HasAnyPermissionAsync(
    userId, "analytics.view", "analytics.view_detailed");

// Check role
var isAdmin = await _authorizationService.HasRoleAsync(userId, "Admin");
```

### **3. Role Assignment**

```csharp
var assignmentRequest = new RoleAssignmentRequest(
    UserId: userId,
    RoleIds: new List<Guid> { premiumRoleId, apiUserRoleId },
    ExpiresAt: DateTime.UtcNow.AddMonths(12),
    Notes: "Annual premium subscription"
);

await _roleManagementService.SetUserRolesAsync(
    userId, assignmentRequest.RoleIds, currentUserId);
```

### **4. Activity Logging**

```csharp
await _userManagementService.LogUserActivityAsync(
    userId: userId,
    action: "URL_CREATED",
    entityType: "ShortUrl",
    entityId: shortUrl.Id.ToString(),
    details: new { OriginalUrl = shortUrl.OriginalUrl, ShortCode = shortUrl.ShortCode }
);
```

## 📊 **Security Features**

### **1. Multi-Factor Authentication**

```csharp
// Setup 2FA
var setupResponse = await _userManagementService.SetupTwoFactorAsync(userId);
// Returns: SecretKey, QrCodeUrl, BackupCodes

// Enable 2FA
await _userManagementService.EnableTwoFactorAsync(userId, totpToken);

// Generate recovery codes
var recoveryCodes = await _userManagementService.GenerateRecoveryCodesAsync(userId);
```

### **2. Session Management**

```csharp
// Get active sessions
var sessions = await _userManagementService.GetUserSessionsAsync(userId);

// Terminate specific session
await _userManagementService.TerminateSessionAsync(sessionId);

// Terminate all sessions (security incident)
await _userManagementService.TerminateAllUserSessionsAsync(userId);
```

### **3. Account Lockout**

```csharp
// Lock account for security
await _userManagementService.LockUserAsync(userId, TimeSpan.FromHours(24), "Suspicious activity");

// Unlock account
await _userManagementService.UnlockUserAsync(userId);

// Reset failed login attempts
await _userManagementService.ResetFailedLoginAttemptsAsync(userId);
```

## 📈 **Analytics & Reporting**

### **1. User Statistics**

```csharp
var stats = await _userManagementService.GetUserStatisticsAsync(userId);
// Returns: URL counts, click stats, activity metrics, device info
```

### **2. Activity Monitoring**

```csharp
var searchRequest = new UserActivitySearchRequest
{
    Action = "LOGIN",
    RiskLevel = ActivityRiskLevel.High,
    FromDate = DateTime.UtcNow.AddDays(-30),
    RequiresReview = true
};

var activities = await _userManagementService.GetUserActivityAsync(userId, searchRequest);
```

### **3. Login Analysis**

```csharp
var loginHistory = await _userManagementService.GetLoginHistoryAsync(userId);
// Analyze: New locations, devices, suspicious patterns, 2FA usage
```

## 🚀 **Advanced Features**

### **1. Bulk Operations**

```csharp
var bulkRequest = new BulkUserOperationRequest(
    UserIds: suspiciousUserIds,
    Operation: "suspend",
    Reason: "Security investigation",
    ExpiresAt: DateTime.UtcNow.AddDays(7)
);

var result = await _userManagementService.BulkUserOperationAsync(bulkRequest);
```

### **2. Custom Policies**

```csharp
// Implement custom authorization policies
var canAccess = await _authorizationService.EvaluatePolicyAsync(
    userId, "CanAccessFinancialData", resource);
```

### **3. Resource-Based Authorization**

```csharp
// Check if user can access specific resource
var canAccess = await _authorizationService.CanAccessResourceAsync(
    userId, "ShortUrl", shortUrlId, "update");
```

## 🔄 **Migration Strategy**

### **Phase 1: Infrastructure Setup**
1. Deploy new database schema
2. Initialize default roles and permissions
3. Migrate existing users to new structure

### **Phase 2: Gradual Rollout**
1. Enable role-based access control
2. Assign appropriate roles to existing users
3. Update API endpoints with authorization

### **Phase 3: Feature Enhancement**
1. Enable advanced security features
2. Implement session management
3. Deploy activity monitoring

## 📋 **Best Practices**

### **1. Security**
- Always use HTTPS for authentication endpoints
- Implement rate limiting on sensitive operations
- Log all security-related events
- Regular security audits and penetration testing

### **2. Performance**
- Cache user permissions for frequently accessed operations
- Use efficient database queries with proper indexing
- Implement pagination for large data sets
- Monitor and optimize slow queries

### **3. Compliance**
- Implement GDPR data protection measures
- Maintain audit trails for compliance
- Secure handling of personal data
- Regular compliance assessments

This enhanced user management system transforms your URL shortening service into an enterprise-ready platform with comprehensive security, user management, and role-based access control capabilities. The system is designed to scale with your business needs while maintaining security and compliance standards.