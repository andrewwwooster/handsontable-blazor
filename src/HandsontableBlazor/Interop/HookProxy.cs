using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using static HandsontableBlazor.Hooks;

namespace HandsontableBlazor.Interop;

internal interface IHookProxy : IDisposable
{
    string Id {get;}
    string HookName {get;}

    public static Tuple<string,Delegate> CreateKey(string hookName, Delegate hook)
    {
        return Tuple.Create(hookName,  hook);
    }        
}



internal abstract class HookProxy<HookArgsT, HookResultT> : IHookProxy
    where HookArgsT : IHookArgs
{
    public string Id {get; private set;} = Guid.NewGuid().ToString();
    public string HookName {get; private set;}
    public DotNetObjectReference<HookProxy<HookArgsT,HookResultT>> ObjectReference { get; private set; }

    public bool IsAsync { get; private set; }

    [JsonIgnore]
    protected readonly Type _argType;

    [JsonIgnore]
    protected readonly Func<HookArgsT, HookResultT> _hook;


    public HookProxy (string hookName, Func<HookArgsT, HookResultT> hook, bool isAsync)
    {
        _argType = typeof(HookArgsT);
        HookName = hookName;
        IsAsync = isAsync;
        _hook = hook;
        ObjectReference = DotNetObjectReference.Create(this);
    }

    public Tuple<string,Delegate> GetKey()
    {
        return IHookProxy.CreateKey(HookName, (Delegate) _hook);
    }

    protected HookArgsT CreateHookArgsT (JsonDocument jdoc)
    {
        try
        {
            var args = (HookArgsT) Activator.CreateInstance(_argType, [HookName, jdoc])!;
            return args;
        }
        catch (Exception ex)
        {
            var rawText = jdoc.RootElement.GetRawText();
            throw new InvalidOperationException($"Error in CreateHookArgsT\n  Hook: {HookName}\n  Inner Exception: {ex.GetType().Name}\n  Message: {ex.Message}\n  Raw JSON: {rawText}", ex);
        }
    }

    public void Dispose()
    {
        ObjectReference.Dispose();
    }
}


internal class AsyncHookProxy<HookArgsT> : HookProxy<HookArgsT,Task>
    where HookArgsT : IHookArgs
{
    public AsyncHookProxy (string hookName, Func<HookArgsT, Task> hook)
        : base(hookName, hook, true)
    { }

    [JSInvokable]
    public async Task HookCallback(JsonDocument jdoc)
    {
        var args = CreateHookArgsT(jdoc);
        await _hook.Invoke(args);
    }
}


internal class SyncHookProxy<HookArgsT,HookResultT> : HookProxy<HookArgsT,HookResultT>
    where HookArgsT : IHookArgs
{
    public SyncHookProxy (string hookName, Func<HookArgsT, HookResultT> hook)
        : base(hookName, hook, false)
    { }

    [JSInvokable]
    public HookResultT HookCallback(JsonDocument jdoc)
    {
        var args = CreateHookArgsT(jdoc);
        return _hook.Invoke(args);
    }
}
