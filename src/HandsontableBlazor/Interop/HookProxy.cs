using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using static HandsontableBlazor.Hooks;

namespace HandsontableBlazor.Interop;

internal interface IHookProxy : IDisposable
{
    string Id {get;}
    string HookName {get;}
    Task<bool> HookCallback(JsonDocument jdoc);

    public static Tuple<string,Delegate> CreateKey(string hookName, Delegate hook)
    {
        return Tuple.Create(hookName,  hook);
    }        
}

internal class HookProxy<HookArgsT> : IHookProxy
    where HookArgsT : IHookArgs
{
    public string Id {get; private set;} = Guid.NewGuid().ToString();
    public string HookName {get; private set;}
    public DotNetObjectReference<HookProxy<HookArgsT>> ObjectReference { get; private set; }

    [JsonIgnore]
    private readonly Type _argType;

    [JsonIgnore]
    private readonly Func<HookArgsT, Task> _hook;

    public HookProxy (string hookName, Func<HookArgsT, Task> hook)
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

    [JSInvokable]
    public async Task<bool> HookCallback(JsonDocument jdoc)
    {
        var args = (HookArgsT) Activator.CreateInstance(_argType, [HookName, jdoc])!;
        if (_hook is Func<HookArgsT, Task<bool>> hook)
        {
            var result = await hook.Invoke(args);
            return result;
        }
        else
        {
            await _hook.Invoke(args);
            return true;
        }
    }

    public void Dispose()
    {
        ObjectReference.Dispose();
    }
}