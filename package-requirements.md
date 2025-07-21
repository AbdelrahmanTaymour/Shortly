# Required NuGet Packages for Enhanced Shortly Project

## Core Infrastructure Packages
```bash
# Redis Caching
dotnet add Shortly.Infrastructure package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add Shortly.Infrastructure package StackExchange.Redis

# Database & Entity Framework
dotnet add Shortly.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Shortly.Infrastructure package Microsoft.EntityFrameworkCore.Tools
dotnet add Shortly.Infrastructure package Microsoft.EntityFrameworkCore.Design

# Background Services
dotnet add Shortly.Infrastructure package Microsoft.Extensions.Hosting.Abstractions
```

## API & Web Packages
```bash
# Rate Limiting
dotnet add Shortly.API package AspNetCoreRateLimit

# API Documentation
dotnet add Shortly.API package Swashbuckle.AspNetCore
dotnet add Shortly.API package Swashbuckle.AspNetCore.Annotations

# Health Checks
dotnet add Shortly.API package Microsoft.Extensions.Diagnostics.HealthChecks
dotnet add Shortly.API package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore

# CORS
dotnet add Shortly.API package Microsoft.AspNetCore.Cors
```

## Analytics & Tracking Packages
```bash
# GeoLocation
dotnet add Shortly.Core package MaxMind.GeoIP2

# Device Detection
dotnet add Shortly.Core package UAParser

# Export Capabilities
dotnet add Shortly.Core package EPPlus
dotnet add Shortly.Core package CsvHelper
```

## Security Packages
```bash
# Password Hashing
dotnet add Shortly.Core package BCrypt.Net-Next

# Input Validation
dotnet add Shortly.Core package System.ComponentModel.Annotations

# URL Validation
dotnet add Shortly.Core package System.Text.RegularExpressions
```

## Payment & Subscription
```bash
# Stripe Integration
dotnet add Shortly.Core package Stripe.net
```

## Email Services
```bash
# Email Sending
dotnet add Shortly.Infrastructure package MailKit
dotnet add Shortly.Infrastructure package MimeKit
```

## File Storage
```bash
# AWS S3 (if using cloud storage)
dotnet add Shortly.Infrastructure package AWSSDK.S3

# Azure Storage (alternative)
dotnet add Shortly.Infrastructure package Azure.Storage.Blobs
```

## Logging & Monitoring
```bash
# Advanced Logging
dotnet add Shortly.API package Serilog.AspNetCore
dotnet add Shortly.API package Serilog.Sinks.Console
dotnet add Shortly.API package Serilog.Sinks.File

# Application Insights (Azure)
dotnet add Shortly.API package Microsoft.ApplicationInsights.AspNetCore
```

## Testing Packages (for future development)
```bash
# Unit Testing
dotnet add Shortly.Tests package Microsoft.NET.Test.Sdk
dotnet add Shortly.Tests package xunit
dotnet add Shortly.Tests package xunit.runner.visualstudio
dotnet add Shortly.Tests package Moq

# Integration Testing
dotnet add Shortly.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add Shortly.Tests package Microsoft.EntityFrameworkCore.InMemory
```

## Installation Commands
Run these commands in the project root directory:

```bash
# Install all packages at once
cd Shortly.Infrastructure
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add package StackExchange.Redis
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.Extensions.Hosting.Abstractions
dotnet add package MailKit
dotnet add package MimeKit
dotnet add package AWSSDK.S3

cd ../Shortly.API
dotnet add package AspNetCoreRateLimit
dotnet add package Swashbuckle.AspNetCore.Annotations
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Cors
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Microsoft.ApplicationInsights.AspNetCore

cd ../Shortly.Core
dotnet add package MaxMind.GeoIP2
dotnet add package UAParser
dotnet add package EPPlus
dotnet add package CsvHelper
dotnet add package BCrypt.Net-Next
dotnet add package Stripe.net
```