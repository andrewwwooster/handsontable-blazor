using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using static HandsontableBlazor.Hooks;

namespace HandsontableBlazor.Interop;


internal interface ICallbackProxy : IDisposable
{
    string Id {get;}
    string? CallbackName {get;}

    public static Tuple<string?,Delegate> CreateKey(string? hookName, Delegate hook)
    {
        return Tuple.Create(hookName,  hook);
    }        
}


internal abstract class AbstractCallbackProxy<CallbackArgsT, ResultT> : ICallbackProxy
{
    public string Id {get; private set;} = Guid.NewGuid().ToString();
    public string? CallbackName {get; set;}
    public String TypeName { get; }
    public bool IsAsync { get; }
    public DotNetObjectReference<AbstractCallbackProxy<CallbackArgsT,ResultT>> ObjectReference { get; private set; }

    [JsonIgnore]
    protected readonly Type _argType;
    [JsonIgnore]
    protected readonly Func<CallbackArgsT, ResultT> _callback;

    public AbstractCallbackProxy(Func<CallbackArgsT, ResultT> callback, string? callbackName)
    {
        CallbackName = callbackName;
        TypeName = GetType().Name;
        IsAsync = typeof(ResultT).IsAssignableTo(typeof(Task));
        ObjectReference = DotNetObjectReference.Create(this);
        _argType = typeof(CallbackArgsT);
        _callback = callback;
    }

    public Tuple<string?,Delegate> GetKey()
    {
        return ICallbackProxy.CreateKey(CallbackName, (Delegate) _callback);
    }
    /**
     * Create a new instance of the CallbackArgsT object
     * 
     * @param jdoc The JSON document to deserialize
     * @return The deserialized object
     */
    protected CallbackArgsT CreateCallbackArgsT (JsonDocument jdoc)
    {
        if (typeof(CallbackArgsT) == typeof(JsonDocument))
        {
           return (CallbackArgsT) (object) jdoc;
        }
        else if (typeof(Callbacks.ICallbackArgs).IsAssignableFrom(typeof(CallbackArgsT)))
        {
            try 
            {
                object?[] args = [jdoc];
                if (typeof(BaseHookArgs).IsAssignableFrom(typeof(CallbackArgsT)))
                {
                    args = [CallbackName, jdoc];
                }
                var callbackArgs = (CallbackArgsT) Activator.CreateInstance(typeof(CallbackArgsT), args)!;
                return callbackArgs;
            }
            catch (Exception ex)
            {
                var rawText = jdoc.RootElement.GetRawText();
                throw new InvalidOperationException($"Error in CreateCallbackArgT; CallbackArgsT: {_argType}; Inner Exception: {ex.GetType().Name};  Message: {ex.Message};  Raw JSON: {rawText}", ex);
            }
        }
        else
        {
            var args = jdoc.Deserialize<List<CallbackArgsT>>();
            if (args != null && args.Count > 0)
                return args[0];
            else
                return default!;
        }
    }

    public void Dispose()
    {
        ObjectReference.Dispose();
    }
}


internal class VoidAsyncCallbackProxy<CallbackArgsT> 
    : AbstractCallbackProxy<CallbackArgsT, Task>
{
    public VoidAsyncCallbackProxy (Func<CallbackArgsT, Task> callback, string? callbackName = null)
        : base(callback, callbackName)
    { }

    [JSInvokable]
    public async void Callback(JsonDocument jdoc)
    {
        var callbackArgs = CreateCallbackArgsT(jdoc);
        await _callback.Invoke(callbackArgs);
    }
}

internal class AsyncCallbackProxy<CallbackArgsT, ResultT> 
    : AbstractCallbackProxy<CallbackArgsT, Task<ResultT>>
{
    public AsyncCallbackProxy (Func<CallbackArgsT, Task<ResultT>> callback, string? callbackName = null)
        : base(callback, callbackName)
    { }

    [JSInvokable]
    public async Task<ResultT> Callback(JsonDocument jdoc)
    {
        var arg = CreateCallbackArgsT(jdoc);
        return await _callback.Invoke(arg);
    }
}

internal class SyncCallbackProxy<CallbackArgsT, ResultT>
    : AbstractCallbackProxy<CallbackArgsT, ResultT>
{
    public SyncCallbackProxy (Func<CallbackArgsT, ResultT> callback, string? callbackName = null)
        : base(callback, callbackName)
    { }

    [JSInvokable]
    public ResultT Callback(JsonDocument jdoc)
    {
        var arg = CreateCallbackArgsT(jdoc);
        return _callback.Invoke(arg);
    }
}