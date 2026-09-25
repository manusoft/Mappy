# Changelog

### 4.0.1 

**Fixed**

* `DateOnly` and `DateOnly?` mapping.
* `TimeOnly` and `TimeOnly?` mapping.
* Constructor mapping for immutable classes and positional records.
* Constructor mapping where a source enum maps to a `string` destination parameter.
* Optional constructor parameters.
* Better handling when `null` is supplied to a non-nullable constructor parameter.
* Complex nested objects continue to be mapped through a mapping plan rather than being directly reused.

**Regression tests added**

* `EmployeeListResponse` with `DateOnly?`.
* `SpeciesLookupItem` record constructor.
* Enum → string constructor mapping.
* `TimeOnly`.
* Existing nested-object and circular-reference tests remain intact.

---

## 4.0.0

### Added
- Cached mapping plans for source/destination type pairs.
- Compiled property getters and setters.
- `MappingException` with source, destination, and member context.
- Strongly typed property rename configuration.
- Strongly typed ignore configuration.
- Explicit type converters.
- `MapTo` for mapping into existing instances.
- Constructor mapping for immutable destination types and records.
- Circular/shared reference preservation.
- Improved arrays and generic collection mapping.
- `MappingOptions` for reference handling, private members, and null handling.
- BenchmarkDotNet benchmark project.

### Changed
- Mapping no longer relies on `PropertyInfo.GetValue`/`SetValue` during normal property execution.
- Async APIs remain available, while the core mapping path stays synchronous.
- Nullable warnings are no longer globally suppressed.
- Package version is 4.0.0.

### Compatibility
The existing extension APIs remain available:

```csharp
source.Map<TDestination>();
source.MapAsync<TDestination>();
source.MapCollection<TDestination>();
source.MapCollectionAsync<TDestination>();
```
