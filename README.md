# Swevo.EFCore.SoftDelete

[![NuGet](https://img.shields.io/nuget/v/Swevo.EFCore.SoftDelete
[![NuGet Downloads](https://img.shields.io/nuget/dt/Swevo.EFCore.SoftDelete.svg)](https://www.nuget.org/packages/Swevo.EFCore.SoftDelete).svg)](https://www.nuget.org/packages/Swevo.EFCore.SoftDelete/)
[![Build](https://github.com/Swevo/EFCore.SoftDelete/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/EFCore.SoftDelete/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Compile-time soft-delete generation for EF Core entities using Roslyn source generators. Add `[SoftDelete]` to any `partial` entity class and get `IsDeleted` + `DeletedAt` fields, an interceptor that converts hard deletes to soft deletes, and a global query filter — all at build time. Zero reflection, AOT-safe, no runtime overhead.

---

## Installation

```bash
dotnet add package Swevo.EFCore.SoftDelete
```

Requires EF Core 7+.

---

## Quick Start

### 1. Mark your entity

```csharp
using EFCore.SoftDelete;

[SoftDelete]
public partial class Article
{
    public int Id { get; set; }
    public string? Title { get; set; }
}
```

The generator adds these automatically:

```csharp
// Generated:
partial class Article : ISoftDeleteEntity
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
```

### 2. Register the interceptor and query filter

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.AddSoftDeleteInterceptor(); // converts Remove() → soft delete
});
```

```csharp
// AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.AddSoftDeleteQueryFilters(); // hides soft-deleted records
}
```

### 3. Use normally

```csharp
// Soft delete — entity stays in the database with IsDeleted = true
dbContext.Articles.Remove(article);
await dbContext.SaveChangesAsync();

// Queries automatically exclude soft-deleted records
var articles = await dbContext.Articles.ToListAsync(); // only active records

// Include soft-deleted when needed
var all = await dbContext.Articles.IgnoreQueryFilters().ToListAsync();
```

---

## How It Works

| Operation | Behaviour |
|---|---|
| `dbContext.Remove(entity)` | Sets `IsDeleted = true`, `DeletedAt = UtcNow` — row stays in DB |
| `dbContext.Set<T>().ToListAsync()` | Excludes soft-deleted records (global query filter) |
| `dbContext.Set<T>().IgnoreQueryFilters()` | Includes soft-deleted records |
| Non-`[SoftDelete]` entities | Unaffected — hard-deleted as normal |

---

## Generated Types

Emitted into your project's `EFCore.SoftDelete` namespace:

| Type | Description |
|---|---|
| `[SoftDelete]` | Attribute to mark entities |
| `ISoftDeleteEntity` | Interface with `IsDeleted` + `DeletedAt` |
| `SoftDeleteInterceptor` | `SaveChangesInterceptor` — converts deletes to soft deletes |
| `SoftDeleteInterceptorExtensions` | `AddSoftDeleteInterceptor()` on `DbContextOptionsBuilder` |
| `SoftDeleteModelBuilderExtensions` | `AddSoftDeleteQueryFilters()` on `ModelBuilder` |

---

## EF Core Trilogy

Use alongside the other Swevo EF Core packages for a complete entity pipeline:

```csharp
[Auditable]   // Swevo.AutoAudit       — CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
[SoftDelete]  // Swevo.EFCore.SoftDelete — IsDeleted, DeletedAt
public partial class Article
{
    public int Id { get; set; }
    // ...
}
```

---

## Diagnostics

| ID | Severity | Description |
|---|---|---|
| `SDEL001` | Error | Class must be `partial` to use `[SoftDelete]` |

---

## Compatibility

| Dependency | Version |
|---|---|
| EF Core | 7.0+ |
| .NET | net7.0+ |
| C# | 10+ |

---


## Also by the same author

> 🌐 Full suite overview: **[swevo.github.io](https://swevo.github.io/)**

| Package | Description |
|---|---|
| [**AutoLog.Generator**](https://github.com/Swevo/AutoLog.Generator) | Compile-time high-performance logging — `[Log(Level, Message)]` generates `LoggerMessage.Define`. AOT-safe. |
| [**AutoHttpClient.Generator**](https://github.com/Swevo/AutoHttpClient.Generator) | Compile-time typed HTTP client — `[HttpClient]` on an interface generates a strongly-typed client. AOT-safe Refit alternative. |
| [**AutoDispatch.Generator**](https://github.com/Swevo/AutoDispatch.Generator) | Compile-time CQRS dispatcher — `[Handler]` generates a strongly-typed `IDispatcher`. No MediatR, no reflection. |
| [**AutoWire**](https://github.com/Swevo/AutoWire) | Compile-time DI auto-registration — `[Scoped]`/`[Singleton]`/`[Transient]` generates `IServiceCollection` registration code. |
| [**AutoMap.Generator**](https://github.com/Swevo/AutoMap.Generator) | Compile-time object mapping with generated extension methods. AOT-safe AutoMapper alternative. |
## License

MIT © 2025 Justin Bannister
