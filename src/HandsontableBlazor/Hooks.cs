using System.Text.Json;
using System.Text.Json.Serialization;

namespace HandsontableBlazor;

/**
* Specify all hook arguments.  Handsontable JavaScript hook callbacks contain multiple
* arguments.  However, for Blazor we pass these arguments in a single *Args class.
*/
public class Hooks 
{
    /**
    * Common base class for all hooks.
    */
    public abstract class BaseHookArgs(string hookName, JsonDocument jdoc) 
        : Callbacks.BaseCallbackArgs(jdoc)
    {
        /**
        * JsonPropertyOrder ensures that hook name appears first when serialized.
        */
        [JsonPropertyOrder(-1)]
        public string HookName { get; private set; } = hookName;
    };

    /**
    * Common base class for Create Row/Column classes.
    */
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

    /**
    * Common base class for Remove Row/Column classes.
    */
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

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteraddchild
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeaddchild
    */
    public class AddChildArgs : BaseHookArgs
    {
        public Object Parent { get; private set; }
        public Object Element { get; private set; }
        public  int? Index { get; private set; }

        public AddChildArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        {
            Parent = jdoc.RootElement[0].Deserialize<object>()!;
            Element = jdoc.RootElement[1].Deserialize<object>()!;
            Index = jdoc.RootElement[2].Deserialize<int?>()!;
        }    
    }

    public abstract class BaseAutofillArgs : BaseHookArgs
    {
        public IList<IList<object>> FillData { get; private set; }
        public CellRange SourceRange { get; private set; }
        public CellRange TargetRange { get; private set; }
        public string Direction { get; private set; }
        public BaseAutofillArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        {
            FillData = jdoc.RootElement[0].Deserialize<IList<IList<object>>>()!;
            SourceRange = jdoc.RootElement[1].Deserialize<CellRange>()!;
            TargetRange = jdoc.RootElement[2].Deserialize<CellRange>()!;
            Direction = jdoc.RootElement[3].Deserialize<string>()!;
        }    
    }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterautofill
    */
    public class AfterAutofillArgs : BaseAutofillArgs
    {
        public AfterAutofillArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterbeginediting
    */
    public class AfterBeginEditingArgs : BaseHookArgs
    {
        public int VisualRow {get; private set;}
        public int VisualColumn {get; private set;}

        public AfterBeginEditingArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        {
            VisualRow = jdoc.RootElement[0].Deserialize<int>()!;
            VisualColumn = jdoc.RootElement[1].Deserialize<int>()!;
        }    
    }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterchange
    */
    public class AfterChangeArgs : BaseHookArgs
    {
        public object[][] Data {get; private set;}
        public string Source {get; private set;}

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
            private IList<object> _args { get; } = args;

            public int Row => (int) _args[0];
            public string Prop => (string) _args[1];
            public object? OldVal => _args[2];
            public object? NewVal => _args[3];
        }
    }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercreatecol
    */
   public class AfterCreateColArgs(string hookName, JsonDocument jdoc) 
        : BaseCreateIndexArgs(hookName, jdoc)
    { }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercreaterow
    */
    public class AfterCreateRowArgs(string hookName, JsonDocument jdoc) 
        : BaseCreateIndexArgs(hookName, jdoc)
    { }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterremovecol
    */
    public class AfterRemoveColArgs(string hookName, JsonDocument jdoc) 
        : BaseRemoveIndexArgs(hookName, jdoc)
    { }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterremoverow
    */
    public class AfterRemoveRowArgs(string hookName, JsonDocument jdoc) 
        : BaseRemoveIndexArgs(hookName, jdoc)
    { }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselection
    */
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

        public int Row { get; private set; }
        public int Column { get; private set; }
        public int Row2 { get; private set; }
        public int Column2 { get; private set; }
        public IDictionary<string, object> PreventScrolling { get; private set; }
        public int SelectionLayerLevel { get; private set; }
    }

     /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselectionend
    */
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

        public int Row { get; private set; }
        public int Column { get; private set; }
        public int Row2 { get; private set; }
        public int Column2 { get; private set; }
        public int SelectionLayerLevel { get; private set; }
    }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforebeginediting
    */
    public class BeforeBeginEditingArgs : BaseHookArgs
    {
        public int VisualRow { get; private set; }
        public int VisualColumn { get; private set; }
        public object? InitialValue { get; private set; }
        public IDictionary<string,object?> Event { get; private set; }
        public bool FullEditMode { get; private set; }

        public BeforeBeginEditingArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        {
            VisualRow = jdoc.RootElement[0].Deserialize<int>();
            VisualColumn = jdoc.RootElement[1].Deserialize<int>();
            InitialValue = jdoc.RootElement[2].Deserialize<object?>();
            Event = jdoc.RootElement[3].Deserialize<IDictionary<string,object?>>()!;
            FullEditMode = jdoc.RootElement[4].Deserialize<bool>();
        }    
    }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeautofill
    */
    public class BeforeAutofillArgs : BaseAutofillArgs
    {
        public BeforeAutofillArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecreatecol
    */
    public class BeforeCreateColArgs(string hookName, JsonDocument jdoc) 
        : BaseCreateIndexArgs(hookName, jdoc)
    { }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecreaterow
    */
    public class BeforeCreateRowArgs(string hookName, JsonDocument jdoc) 
        : BaseCreateIndexArgs(hookName, jdoc)
    { }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeremovecol
    */
    public class BeforeRemoveColArgs(string hookName, JsonDocument jdoc) 
        : BaseRemoveIndexArgs(hookName, jdoc)
    { }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeremoverow
    */
    public class BeforeRemoveRowArgs(string hookName, JsonDocument jdoc) 
        : BaseRemoveIndexArgs(hookName, jdoc)
    { }
}