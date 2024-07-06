using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace HandsontableBlazor.PlayrightTests;

[TestClass]
public class ExampleTest : PageTest
{
    [TestMethod]
    public async Task HasTitle()
    {
        await Page.GotoAsync("http://localhost:5158/");

        // Expect a title "to contain" a substring.
        await Expect(Page).ToHaveTitleAsync(new Regex("Handsontable"));
    }

    [TestMethod]
    public async Task GetStartedLink()
    {
        await Page.GotoAsync("http://localhost:5158/");

        // Click the get started link.
        var checkbox = Page.GetByText("OnAfterChange");
        await checkbox.SetCheckedAsync(true);
    } 
}
