using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static Handsontable.Blazor.HandsontableHooks;
using static Handsontable.Blazor.HandsontableRenderer;

namespace Handsontable.Blazor;

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
    private readonly Lazy<Task<IJSObjectReference>> _jqueryModuleTask;
    private IJSObjectReference _handsontableJsReference = null!;

    public HandsontableJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _handsontableModuleTask = new (() => _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Handsontable.Blazor/handsontableJsInterop.js").AsTask());
        _jqueryModuleTask = new (() => _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "https://ajax.googleapis.com/ajax/libs/jquery/3.7.1/jquery.min.js").AsTask());
            
    }

    public async Task NewHandsontable (string elemId, ConfigurationOptions? configurationOptions) 
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

    public async Task Alter(AlterActionEnum alterAction, int visualIndex)
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "alter", alterAction.ToString(), visualIndex);
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
            var module = await _handsontableModuleTask.Value;
            await module.DisposeAsync();
        }
    }
    IList<AfterChangeHook>    _afterChangeHookList = new List<AfterChangeHook>();
    IDictionary<string,RendererCallback>     _rendererDict = new Dictionary<string,RendererCallback>();

    public async Task AddHookAfterChange(AfterChangeHook afterChangeHook)
    {
        await _handsontableJsReference.InvokeVoidAsync("enableHook", "afterChange");
        _afterChangeHookList.Add(afterChangeHook);
    }

    [JSInvokable]
    public async Task OnAfterChangeCallback(IList<IList<object>> cellUpdates, string source)
    {
        foreach (var afterChangeHook in _afterChangeHookList)
        {
            var args = new AfterChangeArgs { Data = cellUpdates, Source = source };
            await afterChangeHook(args);
        }
    }

    [JSInvokable]
    public async Task OnRendererCallback(
        string rendererName, 
        IJSObjectReference hotInstance, 
        IJSObjectReference td, 
        int row, int col, 
        string prop, object value,
        IDictionary<string,object> cellProperties )
    {
        var args = new RendererArgs{
            HotInstance = hotInstance,
            Td = td,
            Row = row,
            Column = col,
            Prop = prop,
            Value = value,
            CellProperties = cellProperties!
        };
        var renderer = _rendererDict[rendererName];
        await renderer(args);
    }
}
