using Xunit;

// Riptide.Message pools its instances in static, process-wide state that isn't thread-safe; running
// test classes concurrently (xUnit's default) corrupts that pool and produces spurious
// NullReferenceExceptions deep inside unrelated Riptide internals. Every test here that touches a
// real Message/Connection must run on one thread.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
