using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

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
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private string? _elemId;

    public HandsontableJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _moduleTask = new (() => _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Handsontable.Blazor/handsontableJsInterop.js").AsTask());
    }

    public async Task NewHandsontable (string elemId, IList<IList<object>>? data) 
    {
        _elemId = elemId;
        try
        {
            var module = await _moduleTask.Value;
            await module.InvokeAsync<string>(
                "newHandsontable", elemId, data, DotNetObjectReference.Create(this));
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
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("invokeMethod", _elemId, "alter", alterAction.ToString(), visualIndex);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }

    IList<Func<AfterChangeArgs, Task>>    _afterChangeList = new List<Func<AfterChangeArgs, Task>>();


    public async Task AddHookAfterChange(Func<AfterChangeArgs, Task> afterChange)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("enableHook", _elemId, "afterChange");
        _afterChangeList.Add(afterChange);
    }

    [JSInvokable]
    public async Task OnAfterChangeCallback(IList<IList<object>> cellUpdates, string source)
    {
        foreach (var afterChange in _afterChangeList)
        {
            var args = new AfterChangeArgs { Data = cellUpdates, Source = source };
            await afterChange(args);
        }
    }

    public class Change<T> where T : IConvertible
    {
        public Change(IList<object> args)
        {
            Row = (int) args[0];
            Prop = (string) args[1];
            OldVal = (T) Convert.ChangeType(args[2], typeof(T));
            NewVal = (T) Convert.ChangeType(args[3], typeof(T));
        }

        public int Row { get; }
        public string Prop { get; }
        public T? OldVal { get; }
        public T? NewVal { get; }
    }


    public class AfterChangeArgs {
        public required IList<IList<object>> Data { get; set; }
        public required string Source { get; set; }
    }
}
