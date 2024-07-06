using System.Text.RegularExpressions;
using HandsontableBlazor.PlaywrightTests.Pages;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace HandsontableBlazor.PlaywrightTests;

[TestClass]
public class HookTests : PageTest
{
    static HookTests () {
        /**
         * Set to 1 inorder to watch browser.
         */
        // Environment.SetEnvironmentVariable("HEADED", "1");
    }

    [TestMethod]
    public async Task AfterChangeHook()
    {
        var homePage = new HomePage(Page);
        var handsontableComponent = new HandsontableComponent(Page);

        await homePage.GotoAsync();

        // Wait for the Handsontable to be visible.
        await Expect(handsontableComponent.TableLocator()).ToBeVisibleAsync();

        // Click on checekbox.
        await homePage.CheckboxLocator("OnAfterChange").SetCheckedAsync(true);

        await handsontableComponent.SetCellValue(0, 0, "Hello World");

        // Verify hook event in the event log.
        var eventLogLocator = homePage.EventLogLocator();
        await Expect(eventLogLocator).ToHaveValueAsync(new Regex("afterChange"));
    } 
}
