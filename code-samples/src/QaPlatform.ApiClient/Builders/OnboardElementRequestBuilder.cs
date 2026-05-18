using QaPlatform.ApiClient.Models;

namespace QaPlatform.ApiClient.Builders;

/// <summary>
/// Builder for <see cref="OnboardElementRequest"/>.
///
/// <para>
/// Why a builder and not a constructor: onboarding payloads accumulate optional
/// fields over time (tags, region overrides, vendor-specific hints). A builder
/// keeps test bodies readable when only one or two fields matter, while still
/// producing a fully-valid request with sensible defaults.
/// </para>
/// </summary>
public sealed class OnboardElementRequestBuilder
{
    private string _elementId = $"el-{Guid.NewGuid():N}";
    private string _vendor = "vendor-a";
    private string _region = "eu-west";
    private readonly Dictionary<string, string> _tags = new();

    public OnboardElementRequestBuilder WithElementId(string id)
    {
        _elementId = id;
        return this;
    }

    public OnboardElementRequestBuilder WithVendor(string vendor)
    {
        _vendor = vendor;
        return this;
    }

    public OnboardElementRequestBuilder WithRegion(string region)
    {
        _region = region;
        return this;
    }

    public OnboardElementRequestBuilder WithTag(string key, string value)
    {
        _tags[key] = value;
        return this;
    }

    public OnboardElementRequest Build() =>
        new(_elementId, _vendor, _region, _tags);
}
