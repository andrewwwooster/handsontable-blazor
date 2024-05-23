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
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private string? _elemId;

    public HandsontableJsInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new (() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Handsontable.Blazor/handsontableJsInterop.js").AsTask());
    }

    public async Task NewHandsontable (string elemId, IList<IList<object>>? data) 
    {
        _elemId = elemId;
        try
        {
            var module = await _moduleTask.Value;
            await module.InvokeAsync<string>(
                "newHandsontable", elemId, data);
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
}
