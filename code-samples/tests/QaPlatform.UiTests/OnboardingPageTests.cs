using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using Xunit;
using QaPlatform.Ui.Pages;

namespace QaPlatform.UiTests;

/// <summary>
/// UI tests are <strong>skipped by default</strong>.
///
/// <para>
/// They require a Selenium endpoint (local Chrome driver or a Selenium Grid)
/// and a running UI under test. Enable them by setting:
/// </para>
/// <list type="bullet">
///   <item><c>QA_UI_BASEURL</c> \u2014 the base URL of the UI to test.</item>
///   <item><c>QA_SELENIUM_URL</c> (optional) \u2014 a remote Selenium Grid endpoint.
///     If unset, a local Chrome instance is used.</item>
/// </list>
/// <para>
/// Without those, every test in this class is reported <c>Skipped</c> \u2014
/// <c>dotnet test</c> stays green on a clean clone, as documented in
/// <c>code-samples/README.md</c>.
/// </para>
/// </summary>
[Trait("Category", "UI")]
public sealed class OnboardingPageTests : IDisposable
{
    private readonly IWebDriver? _driver;
    private readonly Uri? _uiBaseUrl;

    public OnboardingPageTests()
    {
        var uiBase = Environment.GetEnvironmentVariable("QA_UI_BASEURL");
        if (string.IsNullOrWhiteSpace(uiBase))
        {
            // Driver stays null \u2014 the SkippableFact below will skip.
            return;
        }

        _uiBaseUrl = new Uri(uiBase);

        var seleniumUrl = Environment.GetEnvironmentVariable("QA_SELENIUM_URL");
        if (string.IsNullOrWhiteSpace(seleniumUrl))
        {
            _driver = new ChromeDriver();
        }
        else
        {
            _driver = new RemoteWebDriver(new Uri(seleniumUrl), new ChromeOptions());
        }
    }

    [SkippableFact]
    public void Operator_can_onboard_a_new_element_via_the_UI()
    {
        Skip.If(_driver is null, "UI tests are disabled. Set QA_UI_BASEURL to enable.");

        var page = new OnboardingPage(_driver!, _uiBaseUrl!).Open();

        page
            .SubmitOnboarding(
                elementId: "el-ui-001",
                vendor: "vendor-a",
                region: "eu-west")
            .ShouldShowSuccess();
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
}
