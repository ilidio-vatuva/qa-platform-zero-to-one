using OpenQA.Selenium;

namespace QaPlatform.Ui.Selectors;

/// <summary>
/// Selectors for the Network Element onboarding page.
///
/// <para>
/// <strong>This is the only place selectors for this page may live.</strong>
/// If the UI is refactored, this file is the diff. Page objects must not
/// declare their own <see cref="By"/> instances inline \u2014 doing so spreads
/// volatility across the suite and breaks the one-file-diff guarantee.
/// </para>
/// </summary>
internal static class OnboardingPageSelectors
{
    // Prefer stable, semantic locators (data-test-id) over brittle CSS chains.
    public static readonly By ElementIdInput = By.CssSelector("[data-test-id='onboarding-element-id']");
    public static readonly By VendorSelect   = By.CssSelector("[data-test-id='onboarding-vendor']");
    public static readonly By RegionSelect   = By.CssSelector("[data-test-id='onboarding-region']");
    public static readonly By SubmitButton   = By.CssSelector("[data-test-id='onboarding-submit']");
    public static readonly By SuccessBanner  = By.CssSelector("[data-test-id='onboarding-success']");
    public static readonly By ErrorBanner    = By.CssSelector("[data-test-id='onboarding-error']");
}
