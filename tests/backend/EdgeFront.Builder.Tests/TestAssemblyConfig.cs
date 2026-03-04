// Disable parallel test execution within this assembly.
// WebApplicationFactory creates a real ASP.NET Core host which registers EF Core providers
// (SQL Server) in a static global cache. Parallel execution with unit tests that use
// SQLite causes EF Core's internal service provider to see both providers simultaneously.
// Sequential execution ensures each test class has exclusive use of the provider cache.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
