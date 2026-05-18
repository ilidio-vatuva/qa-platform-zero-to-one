namespace QaPlatform.Core.Configuration;

/// <summary>
/// The set of environments tests can target. Mirrors
/// the tiers described in architecture/environments.md
/// (local Docker, dev, staging).
/// </summary>
public enum TestEnvironment
{
    Local,
    Dev,
    Staging
}
