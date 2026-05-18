using System.Collections.Concurrent;

namespace QaPlatform.Core.Configuration;

/// <summary>
/// Environment-aware configuration resolver.
///
/// Resolution order (highest precedence first):
///   1. Process environment variables (QA_*)
///   2. Per-environment defaults (Local / Dev / Staging)
///   3. Hard-coded fallback for the key, if any
///
/// Tests never read raw env vars directly. Always go through
/// <see cref="EnvironmentConfig"/> so the layering stays honest
/// and a local override is a one-line change.
/// </summary>
public sealed class EnvironmentConfig
{
    private static readonly IReadOnlyDictionary<TestEnvironment, IReadOnlyDictionary<string, string>> Defaults =
        new Dictionary<TestEnvironment, IReadOnlyDictionary<string, string>>
        {
            [TestEnvironment.Local] = new Dictionary<string, string>
            {
                ["ApiBaseUrl"] = "http://localhost:5080",
                ["UiBaseUrl"] = "http://localhost:5081",
                ["SeleniumGridUrl"] = "http://localhost:4444/wd/hub",
                ["DefaultTimeoutSeconds"] = "10"
            },
            [TestEnvironment.Dev] = new Dictionary<string, string>
            {
                ["ApiBaseUrl"] = "https://api.dev.example.internal",
                ["UiBaseUrl"] = "https://app.dev.example.internal",
                ["SeleniumGridUrl"] = "http://selenium-grid.dev.example.internal:4444/wd/hub",
                ["DefaultTimeoutSeconds"] = "20"
            },
            [TestEnvironment.Staging] = new Dictionary<string, string>
            {
                ["ApiBaseUrl"] = "https://api.staging.example.internal",
                ["UiBaseUrl"] = "https://app.staging.example.internal",
                ["SeleniumGridUrl"] = "http://selenium-grid.staging.example.internal:4444/wd/hub",
                ["DefaultTimeoutSeconds"] = "30"
            }
        };

    private readonly ConcurrentDictionary<string, string> _cache = new();

    public EnvironmentConfig(TestEnvironment environment)
    {
        Environment = environment;
    }

    public TestEnvironment Environment { get; }

    /// <summary>
    /// Reads <c>QA_ENVIRONMENT</c> from the process env, defaulting to <see cref="TestEnvironment.Local"/>.
    /// </summary>
    public static EnvironmentConfig FromEnvironment()
    {
        var raw = System.Environment.GetEnvironmentVariable("QA_ENVIRONMENT");
        if (string.IsNullOrWhiteSpace(raw) ||
            !Enum.TryParse<TestEnvironment>(raw, ignoreCase: true, out var parsed))
        {
            parsed = TestEnvironment.Local;
        }
        return new EnvironmentConfig(parsed);
    }

    /// <summary>
    /// Resolve a configuration key. Throws if the key is unknown — tests should
    /// fail fast at startup rather than silently default to a wrong value.
    /// </summary>
    public string Get(string key)
    {
        return _cache.GetOrAdd(key, k =>
        {
            var envVar = System.Environment.GetEnvironmentVariable($"QA_{k.ToUpperInvariant()}");
            if (!string.IsNullOrWhiteSpace(envVar))
            {
                return envVar;
            }

            if (Defaults[Environment].TryGetValue(k, out var defaulted))
            {
                return defaulted;
            }

            throw new KeyNotFoundException(
                $"Configuration key '{k}' has no value for environment '{Environment}'. " +
                $"Set QA_{k.ToUpperInvariant()} or add a default in EnvironmentConfig.");
        });
    }

    public int GetInt(string key) => int.Parse(Get(key));

    public Uri GetUri(string key) => new(Get(key));
}
