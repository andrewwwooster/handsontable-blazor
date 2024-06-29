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

    [JsonIgnore]
    protected readonly Type _argType;

    [JsonIgnore]
    private readonly Func<HookArgsT, HookResultT> _hook;


    public HookProxy (string hookName, Func<HookArgsT, HookResultT> hook)
    {
        _argType = typeof(HookArgsT);
        HookName = hookName;
        _hook = hook;
        ObjectReference = DotNetObjectReference.Create(this);
    }

    public Tuple<string,Delegate> GetKey()
    {
        return IHookProxy.CreateKey(HookName, (Delegate) _hook);
    }

    public void Dispose()
    {
        ObjectReference.Dispose();
    }
}


internal class AsyncHookProxy<HookArgsT> : HookProxy<HookArgsT,Task>
    where HookArgsT : IHookArgs
{
    [JsonIgnore]
    private readonly Func<HookArgsT, Task> _hook;

    public AsyncHookProxy (string hookName, Func<HookArgsT, Task> hook)
        : base(hookName, hook)
    {
        _hook = hook;
    }

    [JSInvokable]
    public async Task HookCallback(JsonDocument jdoc)
    {
        var args = (HookArgsT) Activator.CreateInstance(_argType, [HookName, jdoc])!;
        await _hook.Invoke(args);
    }
}


internal class SyncHookProxy<HookArgsT> : HookProxy<HookArgsT,bool>
    where HookArgsT : IHookArgs
{
    [JsonIgnore]
    private readonly Func<HookArgsT, bool> _hook;

    public SyncHookProxy (string hookName, Func<HookArgsT, bool> hook)
        : base(hookName, hook)
    {
        _hook = hook;
    }

    [JSInvokable]
    public bool HookCallback(JsonDocument jdoc)
    {
        var args = (HookArgsT) Activator.CreateInstance(_argType, [HookName, jdoc])!;
        return _hook.Invoke(args);
    }
}
