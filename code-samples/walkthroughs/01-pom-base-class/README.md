# 01 — POM Base Class

> Why the UI layer separates **intent** from **selectors**, and why every wait is explicit and bounded.

**Code:** [src/QaPlatform.Ui/PageObjectBase.cs](../../src/QaPlatform.Ui/PageObjectBase.cs) · [src/QaPlatform.Ui/Selectors/OnboardingPageSelectors.cs](../../src/QaPlatform.Ui/Selectors/OnboardingPageSelectors.cs) · [src/QaPlatform.Ui/Pages/OnboardingPage.cs](../../src/QaPlatform.Ui/Pages/OnboardingPage.cs)

---

## The pattern

Three files, three responsibilities, **no overlap allowed**:

| File | Owns | Allowed to know about |
|---|---|---|
| `PageObjectBase` | Waits, driver lifecycle | Selenium primitives only |
| `OnboardingPageSelectors` | Every locator for this page | `By` instances |
| `OnboardingPage` | The user's intent | The two files above |

A test never imports a selector. It says:

```csharp
new OnboardingPage(driver, baseUrl)
    .Open()
    .SubmitOnboarding(elementId: "el-001", vendor: "vendor-a", region: "eu-west")
    .ShouldShowSuccess();
```

That fluent surface is the *only* contract a test sees. The driver, the locators, the waits — all hidden.

---

## Why this matters

### 1. UI refactors stay one-file diffs

When a designer reshuffles the onboarding form — adds an icon, changes a `<select>` to a custom dropdown — the change lives in [OnboardingPageSelectors.cs](../../src/QaPlatform.Ui/Selectors/OnboardingPageSelectors.cs). One file. One PR. Reviewable in 30 seconds.

If selectors were sprinkled across page methods (let alone across tests), that same designer change would touch a dozen files and trigger a multi-day clean-up. We've seen it happen on other teams. We chose not to.

### 2. Tests read like product specs

A test using `SubmitOnboarding(elementId, vendor, region)` documents itself. A test using `driver.FindElement(By.CssSelector("..."))` documents the page — which is the wrong layer.

This pays off most when a *non-author* reads the test six months later trying to understand what failed.

### 3. No `Thread.Sleep`. Ever.

`PageObjectBase.WaitForElement` polls until the element is both *displayed* and *enabled*, throwing a `TimeoutException` with the locator in the message if it doesn't happen.

```csharp
protected IWebElement WaitForElement(By by, TimeSpan? timeout = null)
```

There is no `Sleep(2000)` waiting "just in case." When a test fails, the failure points at the actual condition that didn't hold — not at a guess about how slow staging happens to be today.

This is enforced socially, not technically: `Thread.Sleep` in a PR was a block.

---

## What this pattern is **not**

- **Not a DSL.** The team rejected building a Gherkin/SpecFlow layer on top of C#. Adding a new layer to an already-known language was a tax on every joiner with negligible upside.
- **Not auto-healing.** No "smart" locator that tries five strategies. Selectors that break should break loudly — silent recovery hides drift.
- **Not async-aware.** Selenium itself is synchronous in C#; tests are too. The async story lives in the API client (see [walkthrough 02](../02-api-client-wrapper/README.md)).

---

## See it in the test layer

The matching skippable test, opted in via `QA_UI_BASEURL`:

→ [tests/QaPlatform.UiTests/OnboardingPageTests.cs](../../tests/QaPlatform.UiTests/OnboardingPageTests.cs)

---

**Next:** [02 — API Client Wrapper](../02-api-client-wrapper/README.md)
