# ADR 001: Multi-Package NuGet Structure for BreezSDK Extensions

**Date**: 2026-01-24
**Status**: Accepted
**Deciders**: Solution Architecture Team

## Context

The original BreezSDK integration was tightly coupled to Umbraco CMS, making it impossible to use in other .NET application types (console apps, ASP.NET Core web APIs, Blazor applications, MAUI apps, etc.). The goal is to transform this into a production-ready, platform-agnostic package suite that any .NET 8+ application can consume.

Key challenges identified:
1. **Dependency bloat**: A single package would force all consumers to pull in Umbraco, Entity Framework, and platform-specific dependencies they don't need
2. **Platform diversity**: Different application types need different integrations (ASP.NET Core health checks, Umbraco composers, etc.)
3. **Persistence flexibility**: Enterprise deployments need SQL Server, cloud-native apps prefer PostgreSQL, single-instance apps work well with SQLite
4. **Versioning independence**: Platform packages may need updates independent of core functionality

## Decision

We will adopt a **multi-package architecture** with the following structure:

### Core Package (Required)
- **Breez.Sdk.Liquid.Extensions.Core**
  - Platform-agnostic foundation
  - Domain entities, abstractions, and interfaces
  - In-memory payment repository (default)
  - Configuration and validation
  - Exception hierarchy
  - OpenTelemetry instrumentation
  - Minimal dependencies: Polly, OpenTelemetry.Api, Microsoft.Extensions.*

### Platform Integration Packages (Optional)
- **Breez.Sdk.Liquid.Extensions.AspNetCore**
  - Health check implementation
  - Webhook validation middleware
  - Minimal API endpoints
  - WebApplicationBuilder extensions

- **Breez.Sdk.Liquid.Extensions.Umbraco**
  - Umbraco Composer for automatic registration
  - Umbraco Component lifecycle integration
  - UmbracoBuilder extensions

### Persistence Packages (Optional, Choose One)
- **Breez.Sdk.Liquid.Extensions.Sqlite**
  - EF Core SQLite provider
  - Suitable for single-instance deployments

- **Breez.Sdk.Liquid.Extensions.SqlServer**
  - EF Core SQL Server provider
  - Suitable for enterprise deployments

- **Breez.Sdk.Liquid.Extensions.PostgreSql**
  - EF Core PostgreSQL (Npgsql) provider
  - Suitable for cloud-native deployments

### Package Dependency Graph

```
                         ┌─────────────────────────────────┐
                         │  Breez.Sdk.Liquid.Extensions.   │
                         │           Core                  │
                         └─────────────────────────────────┘
                                        │
           ┌────────────────────────────┼────────────────────────────┐
           │                            │                            │
           ▼                            ▼                            ▼
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│     AspNetCore      │    │      Umbraco        │    │   Persistence       │
│  (health, webhooks) │    │    (composers)      │    │  (Sqlite, SqlServer,│
└─────────────────────┘    └─────────────────────┘    │   PostgreSql)       │
                                                      └─────────────────────┘
```

## Alternatives Considered

### Alternative 1: Single Monolithic Package
**Rejected because:**
- Forces all dependencies on all consumers
- Console app would pull Umbraco and ASP.NET Core
- Version updates would affect all consumers
- Bloats deployment size

### Alternative 2: Three-Package Split (Core, Web, Persistence)
**Rejected because:**
- Still bundles ASP.NET Core with Umbraco
- Doesn't address database provider separation
- Enterprise requirements often need different database providers

### Alternative 3: Micropackage Architecture (10+ packages)
**Rejected because:**
- Over-engineering for current scope
- Complex dependency management
- Cognitive overhead for consumers
- Diminishing returns on granularity

## Consequences

### Positive
1. **Minimal footprint**: Consumers only install what they need
2. **Platform flexibility**: Any .NET 8+ application type supported
3. **Independent versioning**: Platform packages can evolve separately
4. **Clear separation of concerns**: Each package has a single purpose
5. **Testing isolation**: Each package can be tested independently
6. **Future extensibility**: New persistence providers easy to add

### Negative
1. **More packages to maintain**: 7 packages vs 1
2. **Coordination required**: Cross-package changes need careful versioning
3. **Documentation overhead**: Multiple READMEs and quickstarts needed
4. **NuGet complexity**: Multiple packages to publish and version

### Mitigations
- Centralized version management via Directory.Build.props
- Automated CI/CD for consistent publishing
- Metapackage consideration for common scenarios (future)
- Comprehensive quickstart documentation

## References

- [NuGet Package Design Best Practices](https://docs.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices)
- [.NET Library Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/library-guidance/)
- [Constitution Article IX.2](../../.specify/memory/constitution.md) - Maximum Project Limit justification
