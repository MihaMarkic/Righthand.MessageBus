using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Collections.Concurrent;

namespace Benchmarks;

/// <summary>
/// Measures using ConcurrentDictionary four reads against using manual lock
/// </summary>
[SimpleJob(RuntimeMoniker.Net70)]
public class ConcurrentStuff
{
    ConcurrentDictionary<Type, object> concurrentTarget = default!;
    Dictionary<Type, object> target = default!;
    ReaderWriterLockSlim sync = default!;

    [GlobalSetup]
    public void SetUp()
    {
        concurrentTarget = new ConcurrentDictionary<Type, object>();
        target = new Dictionary<Type, object>();
        sync = new ReaderWriterLockSlim();
    }

    [Benchmark(Baseline = true)]
    public void ReadingConcurrent()
    {
        concurrentTarget.TryGetValue(typeof(Action<string>), out var action);
        concurrentTarget.TryGetValue(typeof(Action<string>), out action);
        concurrentTarget.TryGetValue(typeof(Action<string>), out action);
        concurrentTarget.TryGetValue(typeof(Action<string>), out action);
    }
    [Benchmark]
    public void Reading()
    {
        sync.EnterReadLock();
        try
        {
            target.TryGetValue(typeof(Action<string>), out var action);
            target.TryGetValue(typeof(Action<string>), out action);
            target.TryGetValue(typeof(Action<string>), out action);
            target.TryGetValue(typeof(Action<string>), out action);
        }
        finally
        {
            sync.ExitReadLock();
        }
    }
    [Benchmark]
    public void ReadingWithoutTryFinally()
    {
        sync.EnterReadLock();
        target.TryGetValue(typeof(Action<string>), out var action);
        target.TryGetValue(typeof(Action<string>), out action);
        target.TryGetValue(typeof(Action<string>), out action);
        target.TryGetValue(typeof(Action<string>), out action);
        sync.ExitReadLock();
    }
}

