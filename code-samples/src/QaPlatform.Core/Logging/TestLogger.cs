using System.Collections.Concurrent;
using System.Text.Json;

namespace QaPlatform.Core.Logging;

/// <summary>
/// Structured per-test logger.
///
/// Why this exists: parallel shards interleave stdout in ways that make
/// post-mortem analysis miserable. Each test gets a dedicated logger keyed
/// by test name; output is emitted as one JSON object per line so logs can
/// be split by test downstream without parsing prose.
///
/// In production this would push to a structured backend (e.g. Seq, ELK).
/// Here it writes to stdout — enough for the sample to be runnable.
/// </summary>
public sealed class TestLogger
{
    private static readonly ConcurrentDictionary<string, TestLogger> Instances = new();
    private readonly string _testName;

    private TestLogger(string testName)
    {
        _testName = testName;
    }

    public static TestLogger For(string testName) =>
        Instances.GetOrAdd(testName, name => new TestLogger(name));

    public void Info(string message, object? data = null) => Emit("INFO", message, data);
    public void Warn(string message, object? data = null) => Emit("WARN", message, data);
    public void Error(string message, object? data = null) => Emit("ERROR", message, data);

    private void Emit(string level, string message, object? data)
    {
        var payload = new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            level,
            test = _testName,
            message,
            data
        };
        // One JSON object per line — survives parallel writes from multiple threads
        // because Console.WriteLine is internally synchronised.
        Console.WriteLine(JsonSerializer.Serialize(payload));
    }
}
