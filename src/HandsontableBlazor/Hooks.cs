using System.Text.Json;
using System.Text.Json.Serialization;

namespace HandsontableBlazor;

public class Hooks 
{
    public abstract class BaseHookArgs(string hookName, JsonDocument jdoc) 
        : Callbacks.BaseCallbackArgs(jdoc)
    {
        [JsonPropertyOrder(-1)]
        public required string HookName { get; set; } = hookName;
    };

    public abstract class BaseCreateIndexArgs : BaseHookArgs
    {
        public BaseCreateIndexArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        {
            Index = jdoc.RootElement[0].Deserialize<int>();
            Amount = jdoc.RootElement[1].Deserialize<int>();
            Source = jdoc.RootElement[2].Deserialize<string>();
        }

        public int Index { get; set; }
        public int Amount { get; set; }
        public string? Source { get; set; }        
    }

    public abstract class BaseRemoveIndexArgs : BaseHookArgs
    {
        public BaseRemoveIndexArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        {
            Index = jdoc.RootElement[0].Deserialize<int>();
            Amount = jdoc.RootElement[1].Deserialize<int>();
            PhysicalColumns = jdoc.RootElement[2].Deserialize<List<int>>()!;
            Source = jdoc.RootElement[3].Deserialize<string>();
        }

        public int Index { get; set; }
        public int Amount { get; set; }
        public IList<int> PhysicalColumns { get; set; }
        public string? Source { get; set; }        
    }

    public class AfterChangeArgs : BaseHookArgs
    {
        public required object[][] Data { get; set; }
        public required string Source { get; set; }

        public AfterChangeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        {
            Data = jdoc.RootElement[0].Deserialize<object[][]>()!;
            Source = jdoc.RootElement[1].Deserialize<string>()!;
        }

        public IEnumerable<DataChange> GetDataChanges() 
        {
            return Data.Select(args => new DataChange(args));
        }

        public class DataChange(IList<object> args)
        {
            private readonly IList<object> _args = args;

            public int Row => (int) _args[0];
            public string Prop => (string) _args[1];
            public object? OldVal => _args[2];
            public object? NewVal => _args[3];
        }
    }

    public class AfterCreateColArgs(string hookName, JsonDocument jdoc) 
        : BaseCreateIndexArgs(hookName, jdoc)
    { }

    public class AfterCreateRowArgs(string hookName, JsonDocument jdoc) 
        : BaseCreateIndexArgs(hookName, jdoc)
    { }

    public class AfterRemoveColArgs(string hookName, JsonDocument jdoc) 
        : BaseRemoveIndexArgs(hookName, jdoc)
    { }

    public class AfterRemoveRowArgs(string hookName, JsonDocument jdoc) 
        : BaseRemoveIndexArgs(hookName, jdoc)
    { }

    public class AfterSelectionArgs : BaseHookArgs
    {
        public AfterSelectionArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        {
            Row = jdoc.RootElement[0].Deserialize<int>();
            Column = jdoc.RootElement[1].Deserialize<int>();
            Row2 = jdoc.RootElement[2].Deserialize<int>();
            Column2 = jdoc.RootElement[3].Deserialize<int>();
            PreventScrolling = jdoc.RootElement[4].Deserialize<IDictionary<string, object>>()!;
            SelectionLayerLevel = jdoc.RootElement[5].Deserialize<int>();
        }

        public int Row { get; set; }
        public int Column { get; set; }
        public int Row2 { get; set; }
        public int Column2 { get; set; }
        public required IDictionary<string, object> PreventScrolling { get; set; }
        public int SelectionLayerLevel { get; set; }
        
    }

    public class AfterSelectionEndArgs : BaseHookArgs
    {
        public AfterSelectionEndArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        {
            Row = jdoc.RootElement[0].Deserialize<int>();
            Column = jdoc.RootElement[1].Deserialize<int>();
            Row2 = jdoc.RootElement[2].Deserialize<int>();
            Column2 = jdoc.RootElement[3].Deserialize<int>();
            SelectionLayerLevel = jdoc.RootElement[4].Deserialize<int>();
        }

        public int Row { get; set; }
        public int Column { get; set; }
        public int Row2 { get; set; }
        public int Column2 { get; set; }
        public int SelectionLayerLevel { get; set; }        
    }

    public class BeforeCreateColArgs(string hookName, JsonDocument jdoc) 
        : BaseCreateIndexArgs(hookName, jdoc)
    { }

    public class BeforeCreateRowArgs(string hookName, JsonDocument jdoc) 
        : BaseCreateIndexArgs(hookName, jdoc)
    { }

    public class BeforeRemoveColArgs(string hookName, JsonDocument jdoc) 
        : BaseRemoveIndexArgs(hookName, jdoc)
    { }

    public class BeforeRemoveRowArgs(string hookName, JsonDocument jdoc) 
        : BaseRemoveIndexArgs(hookName, jdoc)
    { }
}