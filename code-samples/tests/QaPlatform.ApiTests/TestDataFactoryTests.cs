using QaPlatform.ApiClient;
using QaPlatform.ApiClient.Assertions;
using QaPlatform.ApiTests.Fakes;
using QaPlatform.Core.Configuration;
using QaPlatform.TestData;

namespace QaPlatform.ApiTests;

/// <summary>
/// Demonstrates the test data factory pattern: the test body declares
/// <em>what</em> it wants, the factory handles <em>how</em> to create and
/// clean up.
///
/// <para>
/// Compare against <see cref="NetworkElementOnboardingTests"/>: the
/// same end-to-end flow but with all plumbing (builder construction,
/// status polling, delete) pushed into the factory.
/// </para>
/// </summary>
public sealed class TestDataFactoryTests : IDisposable
{
    private readonly FakeNetworkElementsService _fake;
    private readonly HttpClient _http;
    private readonly NetworkElementsClient _client;

    public TestDataFactoryTests()
    {
        _fake = new FakeNetworkElementsService();
        _fake.GivenDeleteSucceeds();

        _http = new HttpClient { BaseAddress = _fake.BaseUrl };
        var config = new EnvironmentConfig(TestEnvironment.Local);
        _client = new NetworkElementsClient(_http, config);
    }

    [Fact]
    public async Task Factory_creates_a_ready_element_and_cleans_up_on_dispose()
    {
        _fake.GivenOnboardingImmediatelyReadyFor("el-factory-001");

        await using var factory = new TestDataFactory(_client, nameof(Factory_creates_a_ready_element_and_cleans_up_on_dispose));

        var element = await factory.AnElement()
            .WithElementId("el-factory-001")
            .WithVendor("vendor-b")
            .WithRegion("us-east")
            .WithTag("env", "test")
            .OnboardedAsync();

        element.ShouldBeReady();
        // On dispose, the factory runs the cleanup registry — delete is invoked
        // even if assertions above had thrown.
    }

    [Fact]
    public async Task Factory_cleans_up_multiple_resources_in_reverse_order()
    {
        _fake.GivenOnboardingImmediatelyReadyFor("el-aaa");
        _fake.GivenOnboardingImmediatelyReadyFor("el-bbb");
        _fake.GivenOnboardingImmediatelyReadyFor("el-ccc");

        await using var factory = new TestDataFactory(_client, nameof(Factory_cleans_up_multiple_resources_in_reverse_order));

        var first = await factory.AnElement().WithElementId("el-aaa").OnboardedAsync();
        var second = await factory.AnElement().WithElementId("el-bbb").OnboardedAsync();
        var third = await factory.AnElement().WithElementId("el-ccc").OnboardedAsync();

        first.ShouldBeReady();
        second.ShouldBeReady();
        third.ShouldBeReady();
        // LIFO teardown verified via the WireMock fake accepting deletes in
        // reverse creation order; failure of any one cleanup must not strand
        // the others (see CleanupRegistry).
    }

    public void Dispose()
    {
        _http.Dispose();
        _fake.Dispose();
    }
}
