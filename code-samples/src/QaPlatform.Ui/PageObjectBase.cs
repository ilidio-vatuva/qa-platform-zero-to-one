using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using QaPlatform.Core.Stability;

namespace QaPlatform.Ui;

/// <summary>
/// Base class for all page objects.
///
/// <para>
/// Strict separation of concerns:
/// <list type="bullet">
///   <item>Page objects expose <strong>intent</strong> (<c>SubmitOnboarding(...)</c>),
///     never selectors.</item>
///   <item>Selectors live in one file per page (see <c>Selectors/</c> folder),
///     so a UI refactor is a one-file diff.</item>
///   <item>Waits are explicit and bounded \u2014 no <c>Thread.Sleep</c>, ever.</item>
/// </list>
/// </para>
///
/// <para>
/// See /architecture/system-architecture.md#ui-layer-selenium--pom.
/// </para>
/// </summary>
public abstract class PageObjectBase
{
    private static readonly TimeSpan DefaultWait = TimeSpan.FromSeconds(10);

    protected IWebDriver Driver { get; }

    protected PageObjectBase(IWebDriver driver)
    {
        Driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    /// <summary>
    /// Finds an element once it satisfies a condition. Default: clickable.
    /// Throws <see cref="TimeoutException"/> with the locator in the message.
    /// </summary>
    protected IWebElement WaitForElement(By by, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(by);
        var wait = new WebDriverWait(Driver, timeout ?? DefaultWait);
        return wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(by);
                return (el.Displayed && el.Enabled) ? el : null;
            }
            catch (NoSuchElementException) { return null; }
            catch (StaleElementReferenceException) { return null; }
        }) ?? throw new TimeoutException($"Element not interactable: {by}");
    }

    /// <summary>
    /// Polls for a UI condition using our shared <see cref="Wait.Until"/>
    /// helper \u2014 same diagnostic contract as the API layer.
    /// </summary>
    protected void WaitUntil(Func<bool> condition, string description, TimeSpan? timeout = null) =>
        Wait.Until(condition, timeout ?? DefaultWait, description);
}
