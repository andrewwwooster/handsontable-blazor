using Microsoft.JSInterop;
using static HandsontableBlazor.Hooks;
using static HandsontableBlazor.Renderer;

namespace HandsontableBlazor.Interop;

// This class provides an example of how JavaScript functionality can be wrapped
// in a .NET class for easy consumption. The associated JavaScript module is
// loaded on demand when first needed.
//
// This class can be registered as scoped DI service and then injected into Blazor
// components for use.


public class HandsontableJsInterop : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Lazy<Task<IJSObjectReference>> _handsontableModuleTask;
    private IJSObjectReference _handsontableJsReference = null!;

    static IDictionary<string,Func<RendererArgs, Task>>     _rendererDict = new Dictionary<string,Func<RendererArgs, Task>>();
    private IDictionary<Tuple<string,Delegate>, IHookProxy>  _hookProxyDict = new Dictionary<Tuple<string,Delegate>, IHookProxy>();


    public HandsontableJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _handsontableModuleTask = new (() => _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/HandsontableBlazor/handsontableJsInterop.js").AsTask());
    }

    public async Task NewHandsontable (string elemId, ConfigurationOptions? configurationOptions) 
    {
        try
        {
            var module = await _handsontableModuleTask.Value;
            var thisObjectReference = DotNetObjectReference.Create(this);
            _handsontableJsReference = await module.InvokeAsync<IJSObjectReference>(
                "newHandsontable", elemId, configurationOptions, thisObjectReference);
        }
        catch (JSException ex)
        {
            var msg = ex.Message;
            throw;
        }

    }

    public async Task NewJQuery (string elemId, ConfigurationOptions? configurationOptions) 
    {
        try
        {
            var module = await _handsontableModuleTask.Value;
            _handsontableJsReference = await module.InvokeAsync<IJSObjectReference>(
                "newHandsontable", elemId, configurationOptions, DotNetObjectReference.Create(this));
        }
        catch (JSException ex)
        {
            var msg = ex.Message;
            throw;
        }
    }

    public enum AlterActionEnum {
        insert_row_above,
        insert_row_below,
        remove_row,
        insert_col_start,
        insert_col_end,
        remove_col
    };

    public async Task Alter(
        AlterActionEnum alterAction, 
        int visualIndex, 
        int amount = 1, string? source = null, bool keepEmptyRows = false)
    {
        var alterActionStr = alterAction.ToString();
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "alter", 
            alterActionStr, visualIndex, amount, source, keepEmptyRows);
    }

    public async Task<JQueryJsInterop> GetCell (int visualRow, int visualColumn, bool topmost = false)
    {
        var htmlTableCellElement = await _handsontableJsReference.InvokeAsync<IJSObjectReference>(
            "invokeMethod", "getCell", visualRow, visualColumn, topmost);
        return new JQueryJsInterop(htmlTableCellElement);
    }

    public async Task SetCellMeta (int visualRow, int visualColumn, string key, object? value)
    {
        await _handsontableJsReference.InvokeVoidAsync(
            "invokeMethod", "setCellMeta", visualRow, visualColumn, key, value);
    }

    public async Task SetCellMetaObject (int visualRow, int visualColumn, IDictionary<string,object?> prop)
    {
        await _handsontableJsReference.InvokeVoidAsync(
            "invokeMethod", "setCellMetaObject", visualRow, visualColumn, prop);
    }

    public async Task SetDataAtCell (int visualRow, int visualColumn, string? value, string? source = null)
    {
        await _handsontableJsReference.InvokeVoidAsync(
            "invokeMethod", "setDataAtCell", visualRow, visualColumn, value, source);
    }


    /**
    * Set data at cell.
    * @param {Array} changes An array of arrays in form of [row, col, value]. 
    *                Where row is visual row index {int}, col {int} is the visual column index, 
    *                and value {string} is a new value.
    */
    public async Task SetDataAtCell (IList<IList<object?>> changes, string? source = null)
    {
        await _handsontableJsReference.InvokeVoidAsync(
            "invokeMethod", "setDataAtCell", changes, null, null, source);
    }

    /**
    * Set data at cell.
    * @param {Array} changes An array of arrays in form of [row, col, value]. 
    *                Where row is visual row index {int}, prop {string} is the column property, 
    *                and value {string} is a new value.
    */
    public async Task SetDataAtRowProp (IList<IList<object?>> changes, string? source = null)
    {
        await _handsontableJsReference.InvokeVoidAsync(
            "invokeMethod", "setDataAtRowProp", changes, null, null, source);
    }

    public async Task<IDictionary<string,object>> GetCellMeta (int visualRow, int visualColumn)
    {
        var cellProperties = await _handsontableJsReference.InvokeAsync<Dictionary<string,object>>(
            "invokeMethod", "getCellMeta", visualRow, visualColumn);
        return cellProperties;
    }

    public async Task<IList<CellRange>?> GetSelectedRange()
    {
        var selecteds =  await _handsontableJsReference.InvokeAsync<IList<IList<int>>>("invokeMethod", "getSelected");
        if (selecteds == null) return null;

        var cellRanges = new List<CellRange>();
        foreach (var selected in selecteds)
        {
            cellRanges.Add(new CellRange(selected[0], selected[1], selected[2], selected[3]));
        }
        return cellRanges;
    }


    public async Task<CellRange?> GetSelectedRangeLast()
    {
        var selected =  await _handsontableJsReference.InvokeAsync<IList<int>>("invokeMethod", "getSelectedLast");
        if (selected == null) return null;
        var cellRange = new CellRange(selected[0], selected[1], selected[2], selected[3]);
        return cellRange;
    }

    public async Task<int> CountCols()
    {
        return await _handsontableJsReference.InvokeAsync<int>("countCols");
    }

    public async Task<int> CountColHeaders()
    {
        return await _handsontableJsReference.InvokeAsync<int>("countColHeaders");
    }

    public async Task<int> CountRows()
    {
        return await _handsontableJsReference.InvokeAsync<int>("countRows");
    }

    public async Task<int> CountRowHeaders()
    {
        return await _handsontableJsReference.InvokeAsync<int>("countRowHeaders");
    }

    public async Task<int> GetRowHeight(int visualRow)
    {
        return await _handsontableJsReference.InvokeAsync<int>("getRowHeight", visualRow);
    }

    public async Task<int> GetColWidth(int visualColumn)
    {
        return await _handsontableJsReference.InvokeAsync<int>("getColWidth", visualColumn);
    }

    public async Task<bool> HasRowHeaders()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("hasRowHeaders");
    }

    public async Task<bool> HasColHeaders()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("hasColHeaders");
    }

    /**
    * Property to column.
    * @returns Visual column index.
    */
    public async Task<int> PropToCol(string prop)
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "propToCol", prop);
    }

    public async Task<int> ToPhysicalColumn (int visualColumn)
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "toPhysicalColumn", visualColumn);
    }
    
    public async Task<int> ToPhysicalRow (int visualRow)
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "toPhysicalRow", visualRow);
    }

    public async Task<int> ToVisualColumn (int physicalColumn)
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "toVisualColumn", physicalColumn);
    }

    public async Task<int> ToVisualRow (int physicalRow)
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "toVisualRow", physicalRow);
    }

    public async Task RegisterRenderer(string rendererName, Func<RendererArgs, Task> rendererCallback)
    {
        var module = await _handsontableModuleTask.Value;
        var dotNetHelper = DotNetObjectReference.Create(this);
        _rendererDict.Add(rendererName, rendererCallback);
        await module.InvokeVoidAsync("registerRenderer", rendererName, dotNetHelper);
    }

    public async ValueTask DisposeAsync()
    {
        if (_handsontableModuleTask.IsValueCreated)
        {
            // @TODO Displose of _handsontableJsReference IJSObjectReference?

            var module = await _handsontableModuleTask.Value;
            await module.DisposeAsync();
        }
    }

 
    public async Task AddHookAfterChange(Func<AfterChangeArgs, Task> hook)
    {
        await AddHook("afterChange", hook);
    }

    public async Task AddHookAfterSelection(Func<AfterSelectionArgs, Task> hook)
    {
        await AddHook("afterSelection", hook);
    }

    public async Task AddHookAfterSelectionEnd(Func<AfterSelectionEndArgs, Task> hook)
    {
        await AddHook("afterSelectionEnd", hook);
    }

    public async Task AddHook<HookArgsT>(string hookName, Func<HookArgsT, Task> hook)
        where HookArgsT : IHookArgs
    {
        var hookProxy = new HookProxy<HookArgsT>(hookName, hook);
        _hookProxyDict[hookProxy.GetKey()] = hookProxy;
        await _handsontableJsReference.InvokeVoidAsync("addHook", hookProxy);
    }

    public async Task RemoveHook<HookArgsT>(string hookName, Func<HookArgsT, Task> hook)
        where HookArgsT : BaseHookArgs
    {
        var hookKey = IHookProxy.CreateKey(hookName, hook);
        var hookProxy = _hookProxyDict[hookKey]; 
        await _handsontableJsReference.InvokeVoidAsync("removeHook", hookProxy);
        _hookProxyDict.Remove(hookKey);
    }

    [JSInvokable]
    public async Task OnRendererCallback(
        string rendererName, 
        IJSObjectReference hotInstance, 
        IJSObjectReference td, 
        int row, int col, 
        object prop, object value,
        IDictionary<string,object> cellProperties )
    {
        var args = new RendererArgs{
            HotInstance = hotInstance,
            Td = new JQueryJsInterop(td),
            Row = row,
            Column = col,
            Prop = prop.ToString()!,
            Value = value,
            CellProperties = cellProperties!
        };
        var renderer = _rendererDict[rendererName];
        await renderer(args);
    }


    /// <summary>
    /// @ToDo Move to top level namespace?
    /// Options:
    ///     1) Pass this RendererProxy to the JavaScript as DotNetReference
    ///         Dot net still needs to know signature.
    ///     2) Avoid dotNet classes in JavaScript space:
    ///         - Register renderer in table here.
    ///         - Lookup renderer before executing
    ///     
    /// </summary>
    public class RendererCallbackProxy
    {
        private Func<RendererArgs, Task> _callback;

        public RendererCallbackProxy (Func<RendererArgs, Task> callback)
        {
            _callback = callback;
        }

        [JSInvokable]
        public async Task OnRendererCallback(
            IJSObjectReference hotInstance, 
            IJSObjectReference td, 
            int row, int col, 
            object prop, object value,
            IDictionary<string,object> cellProperties )
        {
            var args = new RendererArgs{
                HotInstance = hotInstance,
                Td = new JQueryJsInterop(td),
                Row = row,
                Column = col,
                Prop = prop.ToString()!,
                Value = value,
                CellProperties = cellProperties!
            };
            await _callback(args);
        }
    }
}
