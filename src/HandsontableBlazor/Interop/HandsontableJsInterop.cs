using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
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

    static IDictionary<string,RendererCallback>     _rendererDict = new Dictionary<string,RendererCallback>();


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
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "alter", 
            alterAction.ToString(), visualIndex, amount, source, keepEmptyRows);
    }

    public async Task RegisterRenderer(string rendererName, RendererCallback rendererCallback)
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

 
    public async Task AddHookAfterChange(AfterChangeHook hook)
    {
        await AddHook<AfterChangeArgs>("afterChange", hook);
    }

    public async Task AddHookAfterSelection(AfterSelectionHook hook)
    {
        await AddHook<AfterSelectionArgs>("afterSelection", hook);
    }

    public async Task AddHook<HookArgsT>(string hookName, Delegate hook)
        where HookArgsT : BaseHookArgs
    {
        var proxyObjectRef = DotNetObjectReference.Create(new HookProxy<HookArgsT>(hook));
        await _handsontableJsReference.InvokeVoidAsync("addHook", hookName, proxyObjectRef);
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


    public class HookProxy<ArgT> 
        where ArgT : class
    {
        private readonly Delegate _hook;
        private readonly Type _argType;

        public HookProxy (Delegate hook)
        {
            _argType = typeof(ArgT);
            _hook = hook;
        }

        [JSInvokable]
        public async Task HookCallback(JsonDocument jdoc)
        {
            var arg = Activator.CreateInstance(_argType, jdoc);
            var task = _hook.DynamicInvoke(arg) as Task;
            task!.GetAwaiter().GetResult();
            await Task.CompletedTask;
        }
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
        private RendererCallback _callback;

        public RendererCallbackProxy (RendererCallback callback)
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
