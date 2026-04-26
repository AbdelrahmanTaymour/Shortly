IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Roles] (
    [Id] tinyint NOT NULL,
    [RoleName] varchar(30) NOT NULL,
    [Description] nvarchar(200) NULL,
    [DefaultPermissions] bigint NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [SubscriptionPlans] (
    [Id] tinyint NOT NULL,
    [Name] nvarchar(50) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Price] decimal(10,2) NOT NULL,
    [MaxQrCodesPerMonth] int NOT NULL,
    [MaxLinksPerMonth] int NOT NULL,
    [ClickDataRetentionDays] int NOT NULL,
    [LinkAnalysis] bit NOT NULL,
    [BulkCreation] bit NOT NULL,
    [LinkProtection] bit NOT NULL,
    [CustomShortCode] bit NOT NULL,
    [CampaignTracking] bit NOT NULL,
    [GeoDeviceTracking] bit NOT NULL,
    CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [Email] varchar(320) NOT NULL,
    [Username] varchar(50) NOT NULL,
    [PasswordHash] varchar(256) NOT NULL,
    [SubscriptionPlanId] tinyint NOT NULL,
    [Permissions] bigint NOT NULL DEFAULT CAST(17592186109967 AS bigint),
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsEmailConfirmed] bit NOT NULL DEFAULT CAST(0 AS bit),
    [LastLoginAt] datetime2(0) NULL,
    [UpdatedAt] datetime2(0) NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedAt] datetime2(0) NOT NULL DEFAULT (GETUTCDATE()),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2(0) NULL,
    [DeletedBy] uniqueidentifier NULL,
    [GoogleId] nvarchar(max) NULL,
    [GoogleProfilePicture] nvarchar(max) NULL,
    [IsOAuthUser] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_SubscriptionPlans_SubscriptionPlanId] FOREIGN KEY ([SubscriptionPlanId]) REFERENCES [SubscriptionPlans] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmailChangeTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Token] nvarchar(256) NOT NULL,
    [OldEmail] nvarchar(256) NOT NULL,
    [NewEmail] nvarchar(256) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsUsed] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UsedAt] datetime2 NULL,
    CONSTRAINT [PK_EmailChangeTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmailChangeTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Organizations] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Website] varchar(500) NULL,
    [LogoUrl] nvarchar(500) NULL,
    [MemberLimit] int NOT NULL DEFAULT 10,
    [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsSubscribed] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2(0) NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2(0) NOT NULL DEFAULT (GETUTCDATE()),
    [DeletedAt] datetime2(0) NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [SubscriptionPlanId] tinyint NOT NULL,
    CONSTRAINT [PK_Organizations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Organizations_SubscriptionPlans_SubscriptionPlanId] FOREIGN KEY ([SubscriptionPlanId]) REFERENCES [SubscriptionPlans] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Organizations_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TokenHash] varchar(128) NOT NULL,
    [ExpiresAt] datetime2(0) NOT NULL,
    [IsRevoked] bit NOT NULL DEFAULT CAST(0 AS bit),
    [RevokedAt] datetime2 NULL,
    [CreatedAt] datetime2(0) NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2(0) NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserActionTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TokenType] tinyint NOT NULL,
    [TokenHash] nvarchar(255) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [Used] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_UserActionTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserActionTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserAuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Action] varchar(100) NOT NULL,
    [Details] nvarchar(1000) NULL,
    [TimeStamp] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [IpAddress] varchar(45) NULL,
    [UserAgent] nvarchar(500) NULL,
    CONSTRAINT [PK_UserAuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserAuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserProfiles] (
    [UserId] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NULL,
    [Bio] nvarchar(500) NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [ProfilePictureUrl] nvarchar(500) NULL,
    [Website] nvarchar(500) NULL,
    [Company] nvarchar(100) NULL,
    [Location] nvarchar(100) NULL,
    [Country] nvarchar(50) NULL,
    [TimeZone] nvarchar(50) NULL,
    [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_UserProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserSecurity] (
    [UserId] uniqueidentifier NOT NULL,
    [FailedLoginAttempts] int NOT NULL DEFAULT 0,
    [LockedUntil] datetime2 NULL,
    [LockoutReason] nvarchar(100) NULL,
    [TwoFactorEnabled] bit NOT NULL DEFAULT CAST(0 AS bit),
    [TwoFactorSecret] nvarchar(150) NULL,
    [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_UserSecurity] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_UserSecurity_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserUsage] (
    [UserId] uniqueidentifier NOT NULL,
    [MonthlyLinksCreated] int NOT NULL DEFAULT 0,
    [MonthlyQrCodesCreated] int NOT NULL DEFAULT 0,
    [TotalLinksCreated] int NOT NULL DEFAULT 0,
    [TotalQrCodesCreated] int NOT NULL DEFAULT 0,
    [MonthlyResetDate] datetime2 NOT NULL DEFAULT (DATEADD(month, 1, GETUTCDATE())),
    CONSTRAINT [PK_UserUsage] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_UserUsage_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [OrganizationMembers] (
    [Id] uniqueidentifier NOT NULL,
    [OrganizationId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] tinyint NOT NULL DEFAULT CAST(2 AS tinyint),
    [CustomPermissions] bigint NOT NULL DEFAULT CAST(0 AS bigint),
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [InvitedBy] uniqueidentifier NOT NULL,
    [JoinedAt] datetime2(0) NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_OrganizationMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrganizationMembers_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrganizationMembers_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganizationMembers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [OrganizationUsage] (
    [OrganizationId] uniqueidentifier NOT NULL,
    [MonthlyLinksCreated] int NOT NULL DEFAULT 0,
    [MonthlyQrCodesCreated] int NOT NULL DEFAULT 0,
    [TotalLinksCreated] int NOT NULL DEFAULT 0,
    [TotalQrCodesCreated] int NOT NULL DEFAULT 0,
    [MonthlyResetDate] datetime2 NOT NULL DEFAULT (DATEADD(month, 1, GETUTCDATE())),
    CONSTRAINT [PK_OrganizationUsage] PRIMARY KEY ([OrganizationId]),
    CONSTRAINT [FK_OrganizationUsage_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [OrganizationAuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [OrganizationId] uniqueidentifier NOT NULL,
    [ActorId] uniqueidentifier NULL,
    [Event] varchar(100) NULL,
    [TargetEntity] varchar(100) NULL,
    [TargetId] varchar(50) NULL,
    [Details] nvarchar(1000) NULL,
    [TimeStamp] datetime2(0) NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_OrganizationAuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrganizationAuditLogs_OrganizationMembers_ActorId] FOREIGN KEY ([ActorId]) REFERENCES [OrganizationMembers] ([Id]),
    CONSTRAINT [FK_OrganizationAuditLogs_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
);

CREATE TABLE [OrganizationInvitations] (
    [Id] uniqueidentifier NOT NULL,
    [OrganizationId] uniqueidentifier NOT NULL,
    [InvitedUserEmail] nvarchar(320) NOT NULL,
    [InvitedUserRoleId] tinyint NOT NULL,
    [InvitedUserPermissions] bigint NOT NULL,
    [InvitedBy] uniqueidentifier NOT NULL,
    [Status] tinyint NOT NULL DEFAULT CAST(1 AS tinyint),
    [RegisteredAt] datetime2(0) NULL,
    [ExpiresAt] datetime2(0) NULL DEFAULT (DATEADD(day, 5, GETUTCDATE())),
    [CreatedAt] datetime2(0) NOT NULL DEFAULT (GETUTCDATE()),
    [IsExpired] AS CASE WHEN [ExpiresAt] < GETUTCDATE() OR [Status] IN (3,5,6) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END,
    CONSTRAINT [PK_OrganizationInvitations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrganizationInvitations_OrganizationMembers_InvitedBy] FOREIGN KEY ([InvitedBy]) REFERENCES [OrganizationMembers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganizationInvitations_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrganizationInvitations_Roles_InvitedUserRoleId] FOREIGN KEY ([InvitedUserRoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrganizationTeams] (
    [Id] uniqueidentifier NOT NULL,
    [OrganizationId] uniqueidentifier NOT NULL,
    [TeamManagerId] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [CreatedAt] datetime2(0) NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_OrganizationTeams] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrganizationTeams_OrganizationMembers_TeamManagerId] FOREIGN KEY ([TeamManagerId]) REFERENCES [OrganizationMembers] ([Id]),
    CONSTRAINT [FK_OrganizationTeams_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
);

CREATE TABLE [ShortUrls] (
    [Id] bigint NOT NULL IDENTITY,
    [OriginalUrl] nvarchar(2048) NOT NULL,
    [ShortCode] varchar(15) NULL,
    [OwnerType] tinyint NOT NULL,
    [UserId] uniqueidentifier NULL,
    [OrganizationId] uniqueidentifier NULL,
    [CreatedByMemberId] uniqueidentifier NULL,
    [AnonymousSessionId] nvarchar(128) NULL,
    [IpAddress] nvarchar(45) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [TrackingEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
    [ClickLimit] int NOT NULL DEFAULT -1,
    [TotalClicks] int NOT NULL DEFAULT 0,
    [IsPasswordProtected] bit NOT NULL DEFAULT CAST(0 AS bit),
    [PasswordHash] nvarchar(256) NULL,
    [IsPrivate] bit NOT NULL DEFAULT CAST(0 AS bit),
    [ExpiresAt] datetime2(0) NULL,
    [Title] nvarchar(50) NULL,
    [Description] nvarchar(500) NULL,
    [CreatedAt] datetime2(0) NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2(0) NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_ShortUrls] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ShortUrls_SingleOwner] CHECK (
            (
                -- User owned
                [OwnerType] = 1
                AND [UserId] IS NOT NULL
                AND [OrganizationId] IS NULL
                AND [CreatedByMemberId] IS NULL
                AND [AnonymousSessionId] IS NULL
                )
                OR
                (
                    -- Organization owned
                    [OwnerType] = 2
                    AND [UserId] IS NULL
                    AND [OrganizationId] IS NOT NULL
                    AND [CreatedByMemberId] IS NOT NULL
                    AND [AnonymousSessionId] IS NULL
                )
                OR
                (
                    -- Anonymous owned
                    [OwnerType] = 3
                    AND [UserId] IS NULL
                    AND [OrganizationId] IS NULL
                    AND [CreatedByMemberId] IS NULL
                    AND [AnonymousSessionId] IS NOT NULL
            )
        ),
    CONSTRAINT [FK_ShortUrls_OrganizationMembers_CreatedByMemberId] FOREIGN KEY ([CreatedByMemberId]) REFERENCES [OrganizationMembers] ([Id]),
    CONSTRAINT [FK_ShortUrls_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
    CONSTRAINT [FK_ShortUrls_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [OrganizationTeamMembers] (
    [Id] uniqueidentifier NOT NULL,
    [TeamId] uniqueidentifier NOT NULL,
    [MemberId] uniqueidentifier NOT NULL,
    [JoinedAt] datetime2(0) NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_OrganizationTeamMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrganizationTeamMembers_OrganizationMembers_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [OrganizationMembers] ([Id]),
    CONSTRAINT [FK_OrganizationTeamMembers_OrganizationTeams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [OrganizationTeams] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ClickEvents] (
    [Id] uniqueidentifier NOT NULL,
    [ShortUrlId] bigint NOT NULL,
    [ClickedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [IpAddress] varchar(45) NOT NULL,
    [SessionId] varchar(128) NOT NULL,
    [UserAgent] nvarchar(500) NOT NULL,
    [Referrer] nvarchar(500) NULL,
    [UtmSource] nvarchar(100) NULL,
    [UtmMedium] nvarchar(100) NULL,
    [UtmCampaign] nvarchar(100) NULL,
    [UtmTerm] nvarchar(100) NULL,
    [UtmContent] nvarchar(100) NULL,
    [Country] nvarchar(50) NOT NULL,
    [City] nvarchar(80) NOT NULL,
    [Browser] nvarchar(50) NOT NULL,
    [OperatingSystem] nvarchar(50) NOT NULL,
    [Device] nvarchar(50) NOT NULL,
    [DeviceType] nvarchar(20) NULL,
    [ReferrerDomain] nvarchar(100) NULL,
    [TrafficSource] nvarchar(50) NULL,
    CONSTRAINT [PK_ClickEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClickEvents_ShortUrls_ShortUrlId] FOREIGN KEY ([ShortUrlId]) REFERENCES [ShortUrls] ([Id]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DefaultPermissions', N'Description', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [DefaultPermissions], [Description], [RoleName])
VALUES (CAST(1 AS tinyint), CAST(458752 AS bigint), N'Read-only access to resources.', 'Viewer'),
(CAST(2 AS tinyint), CAST(17592723111951 AS bigint), N'Can manage and track their own content.', 'Member'),
(CAST(3 AS tinyint), CAST(17594065320991 AS bigint), N'Can manage their team and content.', 'TeamManager'),
(CAST(4 AS tinyint), CAST(34683993685023 AS bigint), N'Can manage users and settings for the organization.', 'OrgAdmin'),
(CAST(5 AS tinyint), CAST(35184357375007 AS bigint), N'Owns the organization with full control.', 'OrgOwner'),
(CAST(6 AS tinyint), CAST(576460752288709663 AS bigint), N'Platform-wide admin access.', 'PlatformAdmin'),
(CAST(7 AS tinyint), CAST(-1 AS bigint), N'System-wide root access.', 'SystemAdmin');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DefaultPermissions', N'Description', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BulkCreation', N'CampaignTracking', N'ClickDataRetentionDays', N'CustomShortCode', N'Description', N'GeoDeviceTracking', N'LinkAnalysis', N'LinkProtection', N'MaxLinksPerMonth', N'MaxQrCodesPerMonth', N'Name', N'Price') AND [object_id] = OBJECT_ID(N'[SubscriptionPlans]'))
    SET IDENTITY_INSERT [SubscriptionPlans] ON;
INSERT INTO [SubscriptionPlans] ([Id], [BulkCreation], [CampaignTracking], [ClickDataRetentionDays], [CustomShortCode], [Description], [GeoDeviceTracking], [LinkAnalysis], [LinkProtection], [MaxLinksPerMonth], [MaxQrCodesPerMonth], [Name], [Price])
VALUES (CAST(1 AS tinyint), CAST(0 AS bit), CAST(0 AS bit), 0, CAST(0 AS bit), N'Try it out for free', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 5, 2, N'Free', 0.0),
(CAST(2 AS tinyint), CAST(0 AS bit), CAST(0 AS bit), 30, CAST(1 AS bit), N'Unlock powerful data', CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 100, 5, N'Starter', 10.0),
(CAST(3 AS tinyint), CAST(1 AS bit), CAST(0 AS bit), 120, CAST(1 AS bit), N'Create memorable brand experiences', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), 500, 10, N'Professional', 50.0),
(CAST(4 AS tinyint), CAST(1 AS bit), CAST(1 AS bit), 365, CAST(1 AS bit), N'Track your brand in depth', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), 3000, 200, N'Enterprise', 200.0);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BulkCreation', N'CampaignTracking', N'ClickDataRetentionDays', N'CustomShortCode', N'Description', N'GeoDeviceTracking', N'LinkAnalysis', N'LinkProtection', N'MaxLinksPerMonth', N'MaxQrCodesPerMonth', N'Name', N'Price') AND [object_id] = OBJECT_ID(N'[SubscriptionPlans]'))
    SET IDENTITY_INSERT [SubscriptionPlans] OFF;

CREATE INDEX [IX_ClickEvents_ShortUrlId] ON [ClickEvents] ([ShortUrlId]);

CREATE INDEX [IX_ClickEvents_ShortUrlId_ClickedAt] ON [ClickEvents] ([ShortUrlId], [ClickedAt]);

CREATE INDEX [IX_ClickEvents_ShortUrlId_Country] ON [ClickEvents] ([ShortUrlId], [Country]);

CREATE INDEX [IX_ClickEvents_ShortUrlId_DeviceType] ON [ClickEvents] ([ShortUrlId], [DeviceType]);

CREATE INDEX [IX_ClickEvents_ShortUrlId_TrafficSource] ON [ClickEvents] ([ShortUrlId], [TrafficSource]);

CREATE INDEX [IX_EmailChangeTokens_ExpiresAt] ON [EmailChangeTokens] ([ExpiresAt]);

CREATE UNIQUE INDEX [IX_EmailChangeTokens_Token] ON [EmailChangeTokens] ([Token]);

CREATE INDEX [IX_EmailChangeTokens_UserId] ON [EmailChangeTokens] ([UserId]);

CREATE INDEX [IX_EmailChangeTokens_UserId_IsUsed] ON [EmailChangeTokens] ([UserId], [IsUsed]);

CREATE INDEX [IX_OrganizationAuditLogs_ActorId] ON [OrganizationAuditLogs] ([ActorId]);

CREATE INDEX [IX_OrganizationAuditLogs_OrganizationId] ON [OrganizationAuditLogs] ([OrganizationId]);

CREATE INDEX [IX_OrganizationAuditLogs_OrganizationId_TimeStamp] ON [OrganizationAuditLogs] ([OrganizationId], [TimeStamp]);

CREATE INDEX [IX_OrganizationInvitations_InvitedBy] ON [OrganizationInvitations] ([InvitedBy]);

CREATE INDEX [IX_OrganizationInvitations_InvitedUserRoleId] ON [OrganizationInvitations] ([InvitedUserRoleId]);

CREATE INDEX [IX_OrganizationInvitations_OrganizationId_InvitedUserEmail_Status] ON [OrganizationInvitations] ([OrganizationId], [InvitedUserEmail], [Status]);

CREATE INDEX [IX_OrganizationMembers_OrganizationId_IsActive] ON [OrganizationMembers] ([OrganizationId], [IsActive]);

CREATE UNIQUE INDEX [IX_OrganizationMembers_OrganizationId_UserId] ON [OrganizationMembers] ([OrganizationId], [UserId]);

CREATE INDEX [IX_OrganizationMembers_RoleId] ON [OrganizationMembers] ([RoleId]);

CREATE INDEX [IX_OrganizationMembers_UserId] ON [OrganizationMembers] ([UserId]);

CREATE INDEX [IX_Organizations_IsActive] ON [Organizations] ([IsActive]);

CREATE INDEX [IX_Organizations_IsActive_DeletedAt] ON [Organizations] ([IsActive], [DeletedAt]);

CREATE INDEX [IX_Organizations_OwnerId] ON [Organizations] ([OwnerId]);

CREATE UNIQUE INDEX [IX_Organizations_OwnerId_Name] ON [Organizations] ([OwnerId], [Name]);

CREATE INDEX [IX_Organizations_SubscriptionPlanId] ON [Organizations] ([SubscriptionPlanId]);

CREATE INDEX [IX_OrganizationTeamMembers_MemberId] ON [OrganizationTeamMembers] ([MemberId]);

CREATE UNIQUE INDEX [IX_OrganizationTeamMembers_TeamId_MemberId] ON [OrganizationTeamMembers] ([TeamId], [MemberId]);

CREATE UNIQUE INDEX [IX_OrganizationTeams_OrganizationId_Name] ON [OrganizationTeams] ([OrganizationId], [Name]);

CREATE INDEX [IX_OrganizationTeams_TeamManagerId] ON [OrganizationTeams] ([TeamManagerId]);

CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);

CREATE INDEX [IX_RefreshTokens_UserId_IsRevoked_ExpiresAt] ON [RefreshTokens] ([UserId], [IsRevoked], [ExpiresAt]);

CREATE UNIQUE INDEX [IX_Roles_RoleName] ON [Roles] ([RoleName]);

CREATE INDEX [IX_ShortUrls_CreatedByMemberId] ON [ShortUrls] ([CreatedByMemberId]);

CREATE INDEX [IX_ShortUrls_IsActive_ExpiresAt] ON [ShortUrls] ([IsActive], [ExpiresAt]);

CREATE INDEX [IX_ShortUrls_OrganizationId] ON [ShortUrls] ([OrganizationId]);

CREATE INDEX [IX_ShortUrls_OwnerType_AnonymousSessionId] ON [ShortUrls] ([OwnerType], [AnonymousSessionId]);

CREATE UNIQUE INDEX [IX_ShortUrls_ShortCode] ON [ShortUrls] ([ShortCode]) WHERE [ShortCode] IS NOT NULL;

CREATE INDEX [IX_ShortUrls_UserId] ON [ShortUrls] ([UserId]);

CREATE UNIQUE INDEX [IX_SubscriptionPlans_Name] ON [SubscriptionPlans] ([Name]);

CREATE UNIQUE INDEX [IX_UserActionTokens_TokenHash] ON [UserActionTokens] ([TokenHash]);

CREATE INDEX [IX_UserActionTokens_UserId] ON [UserActionTokens] ([UserId]);

CREATE INDEX [IX_UserActionTokens_UserId_TokenType_Used] ON [UserActionTokens] ([UserId], [TokenType], [Used]);

CREATE INDEX [IX_UserAuditLogs_TimeStamp] ON [UserAuditLogs] ([TimeStamp]);

CREATE INDEX [IX_UserAuditLogs_UserId] ON [UserAuditLogs] ([UserId]);

CREATE INDEX [IX_UserAuditLogs_UserId_TimeStamp] ON [UserAuditLogs] ([UserId], [TimeStamp]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE INDEX [IX_Users_IsDeleted] ON [Users] ([IsDeleted]);

CREATE INDEX [IX_Users_IsDeleted_IsActive] ON [Users] ([IsDeleted], [IsActive]);

CREATE INDEX [IX_Users_SubscriptionPlanId] ON [Users] ([SubscriptionPlanId]);

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251212113220_Initial', N'9.0.6');

DROP INDEX [IX_ShortUrls_UserId] ON [ShortUrls];

DROP INDEX [IX_ClickEvents_ShortUrlId_ClickedAt] ON [ClickEvents];

DROP INDEX [IX_ClickEvents_ShortUrlId_Country] ON [ClickEvents];

DROP INDEX [IX_ClickEvents_ShortUrlId_DeviceType] ON [ClickEvents];

DROP INDEX [IX_ClickEvents_ShortUrlId_TrafficSource] ON [ClickEvents];

CREATE INDEX [IX_ShortUrls_TotalClicks_Popular] ON [ShortUrls] ([TotalClicks] DESC, [UserId]) INCLUDE ([Id], [ShortCode], [OriginalUrl], [CreatedAt], [IsActive]);

CREATE INDEX [IX_ShortUrls_UserId_Analytics] ON [ShortUrls] ([UserId], [CreatedAt] DESC) INCLUDE ([Id], [ShortCode], [OriginalUrl], [TotalClicks], [IsActive]) WHERE [UserId] IS NOT NULL;

CREATE INDEX [IX_ClickEvents_Analytics_Covering] ON [ClickEvents] ([ShortUrlId], [ClickedAt]) INCLUDE ([Country], [City], [DeviceType], [Browser], [OperatingSystem], [TrafficSource], [ReferrerDomain], [IpAddress], [SessionId], [UtmSource], [UtmCampaign]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260319211027_Added indesies', N'9.0.6');

DROP INDEX [IX_ClickEvents_Analytics_Covering] ON [ClickEvents];
DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClickEvents]') AND [c].[name] = N'Country');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [ClickEvents] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [ClickEvents] ALTER COLUMN [Country] nvarchar(100) NOT NULL;
CREATE INDEX [IX_ClickEvents_Analytics_Covering] ON [ClickEvents] ([ShortUrlId], [ClickedAt]) INCLUDE ([Country], [City], [DeviceType], [Browser], [OperatingSystem], [TrafficSource], [ReferrerDomain], [IpAddress], [SessionId], [UtmSource], [UtmCampaign]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260408083333_Updated_Country_Length_In_ClickEvent', N'9.0.6');

DROP INDEX [IX_ClickEvents_Analytics_Covering] ON [ClickEvents];

CREATE NONCLUSTERED INDEX IX_ClickEvents_Analytics_Covering
ON dbo.ClickEvents (ShortUrlId ASC, ClickedAt ASC)
INCLUDE
(
    Browser,
    City,
    Country,
    DeviceType,
    IpAddress,
    OperatingSystem,
    ReferrerDomain,
    SessionId,
    TrafficSource,
    UtmCampaign,
    UtmMedium,
    UtmSource,
    UtmTerm,
    UtmContent
)
WITH (ONLINE = OFF, FILLFACTOR = 90, SORT_IN_TEMPDB = ON);

ALTER TABLE [ClickEvents] ADD [UserId] uniqueidentifier NULL;

DECLARE @BatchSize INT = 10000;
DECLARE @Rows      INT = 1;

WHILE @Rows > 0
BEGIN
    UPDATE TOP (@BatchSize) ce
    SET    ce.UserId = su.UserId
    FROM   dbo.ClickEvents ce
    JOIN   dbo.ShortUrls   su
           ON ce.ShortUrlId = su.Id
    WHERE  ce.UserId IS NULL
      AND  su.UserId IS NOT NULL;

    SET @Rows = @@ROWCOUNT;

    IF @Rows > 0
        WAITFOR DELAY '00:00:00.050';
END

CREATE NONCLUSTERED INDEX IX_ClickEvents_UserId_ClickedAt
ON dbo.ClickEvents (UserId ASC, ClickedAt ASC)
INCLUDE
(
    ShortUrlId,
    SessionId,
    Country,
    City,
    Browser,
    OperatingSystem,
    DeviceType,
    ReferrerDomain,
    TrafficSource,
    UtmCampaign,
    UtmMedium,
    UtmSource
)
WHERE UserId IS NOT NULL
WITH (ONLINE = OFF, FILLFACTOR = 90, SORT_IN_TEMPDB = ON);

CREATE NONCLUSTERED INDEX IX_ShortUrls_UserId_OwnerType1
ON dbo.ShortUrls (UserId ASC)
INCLUDE
(
    Id,
    ShortCode,
    OriginalUrl,
    IsActive,
    ExpiresAt,
    CreatedAt,
    TotalClicks
)
WHERE UserId IS NOT NULL AND OwnerType = 1
WITH (ONLINE = OFF, FILLFACTOR = 100, SORT_IN_TEMPDB = ON);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260408110503_AnalyticsPerformanceIndexes', N'9.0.6');

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'IsOAuthUser');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Users] ADD DEFAULT CAST(0 AS bit) FOR [IsOAuthUser];

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DeletedAt', N'DeletedBy', N'Email', N'GoogleId', N'GoogleProfilePicture', N'IsActive', N'IsEmailConfirmed', N'LastLoginAt', N'PasswordHash', N'Permissions', N'SubscriptionPlanId', N'UpdatedAt', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([Id], [CreatedAt], [DeletedAt], [DeletedBy], [Email], [GoogleId], [GoogleProfilePicture], [IsActive], [IsEmailConfirmed], [LastLoginAt], [PasswordHash], [Permissions], [SubscriptionPlanId], [UpdatedAt], [Username])
VALUES ('d27b9c92-747d-4b9d-bc65-083656729e24', '2025-12-12T11:32:20Z', NULL, NULL, 'taymour@gmail.com', NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), NULL, '$2a$10$0EjZyXu5orwyTJ2GZyVRWeRSL0bnR.FrPx0aFi5GORCoDWLVNR6Ca', CAST(-1 AS bigint), CAST(3 AS tinyint), '2025-12-12T11:32:20Z', 'taymour');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DeletedAt', N'DeletedBy', N'Email', N'GoogleId', N'GoogleProfilePicture', N'IsActive', N'IsEmailConfirmed', N'LastLoginAt', N'PasswordHash', N'Permissions', N'SubscriptionPlanId', N'UpdatedAt', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'Bio', N'Company', N'Country', N'Location', N'Name', N'PhoneNumber', N'ProfilePictureUrl', N'TimeZone', N'UpdatedAt', N'Website') AND [object_id] = OBJECT_ID(N'[UserProfiles]'))
    SET IDENTITY_INSERT [UserProfiles] ON;
INSERT INTO [UserProfiles] ([UserId], [Bio], [Company], [Country], [Location], [Name], [PhoneNumber], [ProfilePictureUrl], [TimeZone], [UpdatedAt], [Website])
VALUES ('d27b9c92-747d-4b9d-bc65-083656729e24', NULL, NULL, NULL, NULL, N'Abdelrahman Taymour', NULL, NULL, NULL, '2025-12-12T11:32:20.0000000Z', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'Bio', N'Company', N'Country', N'Location', N'Name', N'PhoneNumber', N'ProfilePictureUrl', N'TimeZone', N'UpdatedAt', N'Website') AND [object_id] = OBJECT_ID(N'[UserProfiles]'))
    SET IDENTITY_INSERT [UserProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'LockedUntil', N'LockoutReason', N'TwoFactorSecret', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[UserSecurity]'))
    SET IDENTITY_INSERT [UserSecurity] ON;
INSERT INTO [UserSecurity] ([UserId], [LockedUntil], [LockoutReason], [TwoFactorSecret], [UpdatedAt])
VALUES ('d27b9c92-747d-4b9d-bc65-083656729e24', NULL, NULL, NULL, '2025-12-12T11:32:20.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'LockedUntil', N'LockoutReason', N'TwoFactorSecret', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[UserSecurity]'))
    SET IDENTITY_INSERT [UserSecurity] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'MonthlyResetDate') AND [object_id] = OBJECT_ID(N'[UserUsage]'))
    SET IDENTITY_INSERT [UserUsage] ON;
INSERT INTO [UserUsage] ([UserId], [MonthlyResetDate])
VALUES ('d27b9c92-747d-4b9d-bc65-083656729e24', '2026-01-12T11:32:20.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'MonthlyResetDate') AND [object_id] = OBJECT_ID(N'[UserUsage]'))
    SET IDENTITY_INSERT [UserUsage] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418091935_Seeded_Admin_User', N'9.0.6');

COMMIT;
GO

