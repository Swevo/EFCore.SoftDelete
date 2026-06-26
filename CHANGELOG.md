# Changelog

## [1.0.0] - 2025-01-01

### Added
- `[SoftDelete]` attribute — marks a partial entity class for soft-delete field generation
- `ISoftDeleteEntity` interface — generated into consuming project; implemented by all `[SoftDelete]` entities
- Generated properties: `IsDeleted` (`bool`, default `false`) and `DeletedAt` (`DateTimeOffset?`)
- `SoftDeleteInterceptor` — `SaveChangesInterceptor` that intercepts `EntityState.Deleted` on `ISoftDeleteEntity` instances and converts to `EntityState.Modified` with `IsDeleted = true`
- `SoftDeleteInterceptorExtensions.AddSoftDeleteInterceptor()` — extension for `DbContextOptionsBuilder`
- `SoftDeleteModelBuilderExtensions.AddSoftDeleteQueryFilters()` — adds EF Core global query filters for all `ISoftDeleteEntity` entity types; call at end of `OnModelCreating`
- `SDEL001` diagnostic — compile-time error when `[SoftDelete]` is applied to a non-partial class
