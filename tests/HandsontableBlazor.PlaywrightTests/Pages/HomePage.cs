using System;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace HandsontableBlazor.PlaywrightTests.Pages
{
    public class HomePage
    {
        private readonly IPage _page;

        public HomePage(IPage page)
        {
            _page = page;
        }

        public async Task GotoAsync() 
        {
            await _page.GotoAsync("http://localhost:5158/");
        }

        public ILocator CheckboxLocator(string label)
        {
            return _page.GetByRole(AriaRole.Checkbox, new PageGetByRoleOptions { Name = label });
        }

        public ILocator EventLogLocator()
        {
            return _page.Locator("#event-log");
        }
    }
}

