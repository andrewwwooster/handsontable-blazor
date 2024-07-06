using System;
using Microsoft.Playwright;

namespace HandsontableBlazor.PlaywrightTests.Pages
{
    public class HandsontableComponent
    {
        private readonly IPage _page;

        public HandsontableComponent(IPage page)
        {
            _page = page;
        }

        public ILocator TableLocator()
        {
            return _page.Locator(".ht_master.handsontable table.htCore");
        }

        public ILocator CellLocator(int row, int col)
        {
            return _page.Locator($".ht_master.handsontable table.htCore tbody tr:nth-of-type({row + 1}) td:nth-of-type({col + 1})");
        }

        public ILocator TextareaLocator()
        {
            return _page.Locator($".handsontableInputHolder:last-of-type > textarea");
        }

        public async Task SetCellValue(int row, int col, string value)
        {
            var cell = CellLocator(row, col);
            await SetCellValue(cell, value);
        }

        public async Task SetCellValue(ILocator cell, string value)
        {
            await cell.DblClickAsync();
            var textarea = TextareaLocator();
            await textarea.FillAsync(value);
            await textarea.PressAsync("Enter");
        }

        public async Task<string> GetCellValue(int row, int col)
        {
            var cell = CellLocator(row, col);
            return await cell.InnerTextAsync();
        }
    }
}

