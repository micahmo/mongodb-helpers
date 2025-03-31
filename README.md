# mongodb-helpers

Wrappers and helpers for using MongoDB in C#.

### DatabaseEngine.cs

Contains a single database instance for use throughout your app. Set the static `ConnectionString` property first, then call `TestConnectionAsync()` to see if it's a valid string. Then access `DatabaseInstance` going forward.

### MognDBExtensions.cs

Provides extension methods on `IMongoCollection` for updates/lookups that would otherwise be complex to construct. The wrapper methods were modeled on [LiteDB](https://github.com/litedb-org/LiteDB).