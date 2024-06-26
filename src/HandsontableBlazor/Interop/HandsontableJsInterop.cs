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
    
    public async Task Clear()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "clear");
    }

    public async Task ClearUndo()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "clearUndo");
    }

    public async Task<object> ColToProp(int visualColumn)
    {
        return await _handsontableJsReference.InvokeAsync<object>("invokeMethod", "colToProp", visualColumn);
    }

    public async Task<string> ColToPropString(int visualColumn)
    {
        var result = await ColToProp(visualColumn);
        return result.ToString()!;
    }

    public async Task<int> CountColHeaders()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countColHeaders");
    }

    public async Task<int> CountCols()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countCols");
    }

    public async Task<int> CountEmptyCols()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countEmptyCols");
    }

    public async Task<int> CountEmptyRows()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countEmptyRows");
    }

    public async Task<int> CountRenderedCols()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countRenderedCols");
    }

    public async Task<int> CountRenderedRows()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countRenderedRows");
    }

    public async Task<int> CountSourceCols()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countSourceCols");
    }

    public async Task<int> CountSourceRows()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countSourceRows");
    }


    public async Task<int> CountVisibleCols()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countVisibleCols");
    }

    public async Task<int> CountVisibleRows()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countVisibleRows");
    }


    public async Task<int> CountRowHeaders()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countRowHeaders");
    }

    public async Task<int> CountRows()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countRows");
    }

    public async Task<JQueryJsInterop> GetCell (int visualRow, int visualColumn, bool topmost = false)
    {
        var htmlTableCellElement = await _handsontableJsReference.InvokeAsync<IJSObjectReference>(
            "invokeMethod", "getCell", visualRow, visualColumn, topmost);
        return new JQueryJsInterop(htmlTableCellElement);
    }

    public async Task<IDictionary<string,object>> GetCellMeta (int visualRow, int visualColumn)
    {
        var cellProperties = await _handsontableJsReference.InvokeAsync<Dictionary<string,object>>(
            "invokeMethod", "getCellMeta", visualRow, visualColumn);
        return cellProperties;
    }

    public async Task<int> GetColWidth(int visualColumn)
    {
        return await _handsontableJsReference.InvokeAsync<int>("getColWidth", visualColumn);
    }

    public async Task<int> GetRowHeight(int visualRow)
    {
        return await _handsontableJsReference.InvokeAsync<int>("getRowHeight", visualRow);
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

    public async Task<bool> HasColHeaders()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "hasColHeaders");
    }

    /**
    * Check if hook with a given name was registered.
    * @param {String} hookName Hook name should be name without "on" prefix.  Hook name
    *                   always starts with a lowcase character.
    */
    public async Task<bool> HasHook(string hookName)
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "hasHook", hookName);
    }
    
    public async Task<bool> HasRowHeaders()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "hasRowHeaders");
    }

    public async Task<bool> IsColumnModificationAllowed()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isColumnModificationAllowed");
    }

    public async Task<bool> IsEmptyCol(int visualColumn)
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isEmptyCol", visualColumn);
    }

    public async Task<bool> IsEmptyRow(int visualRow)
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isEmptyRow", visualRow);
    }

    public async Task<bool> IsExecutionSuspended()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isExecutionSuspended");
    }

    public async Task<bool> IsListening()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isListening");
    }

    public async Task<bool> IsLtr()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isLtr");
    }
    public async Task<bool> IsRedoAvailable()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isRedoAvailable");
    }

    public async Task<bool> IsRenderSuspended()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isRenderSuspended");
    }

    public async Task<bool> IsRtl()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isRtl");
    }

    public async Task<bool> IsUndoAvailable()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isUndoAvailable");
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
    /**
    * Property to column.
    * @returns Visual column index.
    */
    public async Task<int> PropToCol(string prop)
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "propToCol", prop);
    }

    public async Task Redo ()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "redo");
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

    public async Task Undo ()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "undo");
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

    public async Task AddHookAfterCreateCol(Func<AfterCreateColArgs, Task> hook)
    {
        await AddHook("afterCreateCol", hook);
    }

    public async Task AddHookAfterCreateRow(Func<AfterCreateRowArgs, Task> hook)
    {
        await AddHook("afterCreateRow", hook);
    }

    public async Task AddHookAfterSelection(Func<AfterSelectionArgs, Task> hook)
    {
        await AddHook("afterSelection", hook);
    }

    public async Task AddHookAfterRemoveCol(Func<AfterRemoveColArgs, Task> hook)
    {
        await AddHook("afterRemoveCol", hook);
    }
    public async Task AddHookAfterRemoveRow(Func<AfterRemoveRowArgs, Task> hook)
    {
        await AddHook("afterRemoveRow", hook);
    }

    public async Task AddHookAfterSelectionEnd(Func<AfterSelectionEndArgs, Task> hook)
    {
        await AddHook("afterSelectionEnd", hook);
    }

    public async Task AddHookBeforeCreateCol(Func<BeforeCreateColArgs, bool> hook)
    {
        await AddSyncHook("beforeCreateCol", hook);
    }

    public async Task AddHookBeforeCreateRow(Func<BeforeCreateRowArgs, bool> hook)
    {
        await AddSyncHook("beforeCreateRow", hook);
    }

    public async Task AddHookBeforeRemoveCol(Func<BeforeRemoveColArgs, bool> hook)
    {
        await AddSyncHook("beforeRemoveCol", hook);
    }

    public async Task AddHookBeforeRemoveRow(Func<BeforeRemoveRowArgs, bool> hook)
    {
        await AddSyncHook("beforeRemoveRow", hook);
    }

    public async Task AddHook<HookArgsT>(string hookName, Func<HookArgsT, Task> hook)
        where HookArgsT : IHookArgs
    {
        var hookProxy = new AsyncHookProxy<HookArgsT>(hookName, hook);
        _hookProxyDict[hookProxy.GetKey()] = hookProxy;
        await _handsontableJsReference.InvokeVoidAsync("addHook", hookProxy);
    }
    
    public async Task AddSyncHook<HookArgsT,HookResultT>(string hookName, Func<HookArgsT, HookResultT> hook)
        where HookArgsT : IHookArgs
    {
        var hookProxy = new SyncHookProxy<HookArgsT,HookResultT>(hookName, hook);
        _hookProxyDict[hookProxy.GetKey()] = hookProxy;
        await _handsontableJsReference.InvokeVoidAsync("addHook", hookProxy);
    }
    

    public async Task RemoveHook<HookArgsT>(string hookName, Func<HookArgsT, object> hook)
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
