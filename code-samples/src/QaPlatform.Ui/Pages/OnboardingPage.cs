using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using QaPlatform.Ui.Selectors;

namespace QaPlatform.Ui.Pages;

/// <summary>
/// Page object for the Network Element onboarding form.
///
/// <para>
/// Exposes intent only \u2014 callers say <c>SubmitOnboarding(...)</c>, never
/// "click this button, fill that field". Selectors live in
/// <see cref="OnboardingPageSelectors"/>.
/// </para>
/// </summary>
public sealed class OnboardingPage : PageObjectBase
{
    private readonly Uri _baseUrl;

    public OnboardingPage(IWebDriver driver, Uri baseUrl) : base(driver)
    {
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
    }

    /// <summary>Navigate to the onboarding page and wait for it to be ready.</summary>
    public OnboardingPage Open()
    {
        Driver.Navigate().GoToUrl(new Uri(_baseUrl, "/onboarding"));
        WaitForElement(OnboardingPageSelectors.SubmitButton);
        return this;
    }

    /// <summary>
    /// Fill the form and submit. Returns this page so the caller can chain
    /// an assertion (<see cref="ShouldShowSuccess"/>).
    /// </summary>
    public OnboardingPage SubmitOnboarding(string elementId, string vendor, string region)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);
        ArgumentException.ThrowIfNullOrWhiteSpace(vendor);
        ArgumentException.ThrowIfNullOrWhiteSpace(region);

        var idInput = WaitForElement(OnboardingPageSelectors.ElementIdInput);
        idInput.Clear();
        idInput.SendKeys(elementId);

        new SelectElement(WaitForElement(OnboardingPageSelectors.VendorSelect)).SelectByValue(vendor);
        new SelectElement(WaitForElement(OnboardingPageSelectors.RegionSelect)).SelectByValue(region);

        WaitForElement(OnboardingPageSelectors.SubmitButton).Click();
        return this;
    }

    /// <summary>Asserts the success banner appeared within the default wait.</summary>
    public void ShouldShowSuccess() =>
        WaitForElement(OnboardingPageSelectors.SuccessBanner);
}
