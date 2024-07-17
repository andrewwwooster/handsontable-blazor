using System.Text.Json;
using System.Text.Json.Serialization;
using HandsontableBlazor.Interop;
using Microsoft.JSInterop;
using static HandsontableBlazor.Callbacks;


namespace HandsontableBlazor;

/**
* Specify all hook arguments.  Handsontable JavaScript hook callbacks contain multiple
* arguments.  However, for Blazor we pass these arguments in a single *Args class.
*/
public class Hooks2
{
    private IJSObjectReference _handsontableJsReference = null!;
    /**
    * We track each hook proxy so that we can remove them letter.
    * Key is the hook name, and hook function.  Value is the hook proxy.
    */
    private IDictionary<Tuple<string?,Delegate>, ICallbackProxy>  _hookProxyDict { get; } = new Dictionary<Tuple<string?,Delegate>, ICallbackProxy>();

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
        public required string HookName { get; set; } = hookName;
    }


    /**
    * Register a hook callback.
    */
    public async Task AddHook<HookArgsT>(string hookName, Func<HookArgsT, Task> hook)
        where HookArgsT : ICallbackArgs
    {
        var hookProxy = new VoidAsyncCallbackProxy<HookArgsT>(hook, hookName);
        _hookProxyDict[hookProxy.GetKey()] = hookProxy;
        await _handsontableJsReference.InvokeVoidAsync("addHook", hookProxy);
    }
    
    /**
    * Add a synchronous hook.  This is used when the caller needs a return value.
    * The return value is typically a boolean from a Before* callback, which will 
    * determine whether or not Handsontable will proceed with the action.
    */
    public async Task AddSyncHook<HookArgsT,HookResultT>(string hookName, Func<HookArgsT, HookResultT> hook)
        where HookArgsT : ICallbackArgs
    {
        var hookProxy = new SyncCallbackProxy<HookArgsT,HookResultT>(hook, hookName);
        _hookProxyDict[hookProxy.GetKey()] = hookProxy;
        await _handsontableJsReference.InvokeVoidAsync("addHook", hookProxy);
    }



    // **********************************************************************************
    // Base classes for hook families.
    // **********************************************************************************

    /**
    * Common base class for Create Autofill classes.
    */
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



    // **********************************************************************************
    // Hook Definitions
    // Arg: For each hook there is an *Args classes derived from BaseHookArgs that
    // defines the hook arguments.  By default we leave the hook arguments a serialzied
    // JsonDocuments.  We define specific typed arguments on an as needed basis.
    // AddHook: After the Args are defined we define a AddHook methids for registering 
    // the hook.
    // See https://handsontable.com/docs/javascript-data-grid/api/hooks/
    // **********************************************************************************

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteraddchild
    */
    public class AfterAddChildArgs : BaseHookArgs
    {
        public AfterAddChildArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteraddchild
    */
    public async Task AddHookAfterAddChild(Func<AfterAddChildArgs, Task> hook)
    {
        await AddHook("afterAddChild", hook);
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
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterautofill
    */
    public async Task AddHookAfterAutofill(Func<AfterAutofillArgs, Task> hook)
    {
        await AddHook("afterAutofill", hook);
    }


    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterbeginediting
    */
    public class AfterBeginEditingArgs : BaseHookArgs
    {
        public int VisualRow { get; private set; }
        public int VisualColumn { get; private set; }

        public AfterBeginEditingArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        {
            VisualRow = jdoc.RootElement[0].Deserialize<int>()!;
            VisualColumn = jdoc.RootElement[1].Deserialize<int>()!;
        }    
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterbeginediting
    */
    public async Task AddHookAfterBeginEditing(Func<AfterBeginEditingArgs, Task> hook)
    {
        await AddHook("afterBeginEditing", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercellmetareset
    */
    public class AfterCellMetaResetArgs : BaseHookArgs
    {
        public AfterCellMetaResetArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercellmetareset
    */
    public async Task AddHookAfterCellMetaReset(Func<AfterCellMetaResetArgs, Task> hook)
    {
        await AddHook("afterCellMetaReset", hook);
    }


    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterchange
    */
    public class AfterChangeArgs : BaseHookArgs
    {
        public object[][] Data { get; private set; }
        public string Source { get; private set; }

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
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterchange
    */
    public async Task AddHookAfterChange(Func<AfterChangeArgs, Task> hook)
    {
        await AddHook("afterChange", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumncollapse
    */
    public class AfterColumnCollapseArgs : BaseHookArgs
    {
        public AfterColumnCollapseArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumncollapse
    */
    public async Task AddHookAfterColumnCollapse(Func<AfterColumnCollapseArgs, Task> hook)
    {
        await AddHook("afterColumnCollapse", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnexpand
    */
    public class AfterColumnExpandArgs : BaseHookArgs
    {
        public AfterColumnExpandArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnexpand
    */
    public async Task AddHookAfterColumnExpand(Func<AfterColumnExpandArgs, Task> hook)
    {
        await AddHook("afterColumnExpand", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnfreeze
    */
    public class AfterColumnFreezeArgs : BaseHookArgs
    {
        public AfterColumnFreezeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnfreeze
    */
    public async Task AddHookAfterColumnFreeze(Func<AfterColumnFreezeArgs, Task> hook)
    {
        await AddHook("afterColumnFreeze", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnmove
    */
    public class AfterColumnMoveArgs : BaseHookArgs
    {
        public AfterColumnMoveArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnmove
    */
    public async Task AddHookAfterColumnMove(Func<AfterColumnMoveArgs, Task> hook)
    {
        await AddHook("afterColumnMove", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnresize
    */
    public class AfterColumnResizeArgs : BaseHookArgs
    {
        public AfterColumnResizeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnresize
    */
    public async Task AddHookAfterColumnResize(Func<AfterColumnResizeArgs, Task> hook)
    {
        await AddHook("afterColumnResize", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnsequencechange
    */
    public class AfterColumnSequenceChangeArgs : BaseHookArgs
    {
        public AfterColumnSequenceChangeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnsequencechange
    */
    public async Task AddHookAfterColumnSequenceChange(Func<AfterColumnSequenceChangeArgs, Task> hook)
    {
        await AddHook("afterColumnSequenceChange", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnsort
    */
    public class AfterColumnSortArgs : BaseHookArgs
    {
        public AfterColumnSortArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnsort
    */
    public async Task AddHookAfterColumnSort(Func<AfterColumnSortArgs, Task> hook)
    {
        await AddHook("afterColumnSort", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnunfreeze
    */
    public class AfterColumnUnfreezeArgs : BaseHookArgs
    {
        public AfterColumnUnfreezeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercolumnunfreeze
    */
    public async Task AddHookAfterColumnUnfreeze(Func<AfterColumnUnfreezeArgs, Task> hook)
    {
        await AddHook("afterColumnUnfreeze", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercontextmenudefaultoptions
    */
    public class AfterContextMenuDefaultOptionsArgs : BaseHookArgs
    {
        public AfterContextMenuDefaultOptionsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercontextmenudefaultoptions
    */
    public async Task AddHookAfterContextMenuDefaultOptions(Func<AfterContextMenuDefaultOptionsArgs, Task> hook)
    {
        await AddHook("afterContextMenuDefaultOptions", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercontextmenuhide
    */
    public class AfterContextMenuHideArgs : BaseHookArgs
    {
        public AfterContextMenuHideArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercontextmenuhide
    */
    public async Task AddHookAfterContextMenuHide(Func<AfterContextMenuHideArgs, Task> hook)
    {
        await AddHook("afterContextMenuHide", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercontextmenushow
    */
    public class AfterContextMenuShowArgs : BaseHookArgs
    {
        public AfterContextMenuShowArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercontextmenushow
    */
    public async Task AddHookAfterContextMenuShow(Func<AfterContextMenuShowArgs, Task> hook)
    {
        await AddHook("afterContextMenuShow", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercopy
    */
    public class AfterCopyArgs : BaseHookArgs
    {
        public AfterCopyArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercopy
    */
    public async Task AddHookAfterCopy(Func<AfterCopyArgs, Task> hook)
    {
        await AddHook("afterCopy", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercopylimit
    */
    public class AfterCopyLimitArgs : BaseHookArgs
    {
        public AfterCopyLimitArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercopylimit
    */
    public async Task AddHookAfterCopyLimit(Func<AfterCopyLimitArgs, Task> hook)
    {
        await AddHook("afterCopyLimit", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercreatecol
    */
    public class AfterCreateColArgs : BaseCreateIndexArgs
    {
        public AfterCreateColArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercreatecol
    */
    public async Task AddHookAfterCreateCol(Func<AfterCreateColArgs, Task> hook)
    {
        await AddHook("afterCreateCol", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercreaterow
    */
    public class AfterCreateRowArgs : BaseCreateIndexArgs
    {
        public AfterCreateRowArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercreaterow
    */
    public async Task AddHookAfterCreateRow(Func<AfterCreateRowArgs, Task> hook)
    {
        await AddHook("afterCreateRow", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercut
    */
    public class AfterCutArgs : BaseHookArgs
    {
        public AfterCutArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftercut
    */
    public async Task AddHookAfterCut(Func<AfterCutArgs, Task> hook)
    {
        await AddHook("afterCut", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdeselect
    */
    public class AfterDeselectArgs : BaseHookArgs
    {
        public AfterDeselectArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdeselect
    */
    public async Task AddHookAfterDeselect(Func<AfterDeselectArgs, Task> hook)
    {
        await AddHook("afterDeselect", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdestroy
    */
    public class AfterDestroyArgs : BaseHookArgs
    {
        public AfterDestroyArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdestroy
    */
    public async Task AddHookAfterDestroy(Func<AfterDestroyArgs, Task> hook)
    {
        await AddHook("afterDestroy", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdetachchild
    */
    public class AfterDetachChildArgs : BaseHookArgs
    {
        public AfterDetachChildArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdetachchild
    */
    public async Task AddHookAfterDetachChild(Func<AfterDetachChildArgs, Task> hook)
    {
        await AddHook("afterDetachChild", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdocumentkeydown
    */
    public class AfterDocumentKeyDownArgs : BaseHookArgs
    {
        public AfterDocumentKeyDownArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdocumentkeydown
    */
    public async Task AddHookAfterDocumentKeyDown(Func<AfterDocumentKeyDownArgs, Task> hook)
    {
        await AddHook("afterDocumentKeyDown", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdrawselection
    */
    public class AfterDrawSelectionArgs : BaseHookArgs
    {
        public AfterDrawSelectionArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdrawselection
    */
    public async Task AddHookAfterDrawSelection(Func<AfterDrawSelectionArgs, Task> hook)
    {
        await AddHook("afterDrawSelection", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdropdownmenudefaultoptions
    */
    public class AfterDropdownMenuDefaultOptionsArgs : BaseHookArgs
    {
        public AfterDropdownMenuDefaultOptionsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdropdownmenudefaultoptions
    */
    public async Task AddHookAfterDropdownMenuDefaultOptions(Func<AfterDropdownMenuDefaultOptionsArgs, Task> hook)
    {
        await AddHook("afterDropdownMenuDefaultOptions", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdropdownmenuhide
    */
    public class AfterDropdownMenuHideArgs : BaseHookArgs
    {
        public AfterDropdownMenuHideArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdropdownmenuhide
    */
    public async Task AddHookAfterDropdownMenuHide(Func<AfterDropdownMenuHideArgs, Task> hook)
    {
        await AddHook("afterDropdownMenuHide", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdropdownmenushow
    */
    public class AfterDropdownMenuShowArgs : BaseHookArgs
    {
        public AfterDropdownMenuShowArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterdropdownmenushow
    */
    public async Task AddHookAfterDropdownMenuShow(Func<AfterDropdownMenuShowArgs, Task> hook)
    {
        await AddHook("afterDropdownMenuShow", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterfilter
    */
    public class AfterFilterArgs : BaseHookArgs
    {
        public AfterFilterArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterfilter
    */
    public async Task AddHookAfterFilter(Func<AfterFilterArgs, Task> hook)
    {
        await AddHook("afterFilter", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterformulasvaluesupdate
    */
    public class AfterFormulasValuesUpdateArgs : BaseHookArgs
    {
        public AfterFormulasValuesUpdateArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterformulasvaluesupdate
    */
    public async Task AddHookAfterFormulasValuesUpdate(Func<AfterFormulasValuesUpdateArgs, Task> hook)
    {
        await AddHook("afterFormulasValuesUpdate", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftergetcellmeta
    */
    public class AfterGetCellMetaArgs : BaseHookArgs
    {
        public AfterGetCellMetaArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftergetcellmeta
    */
    public async Task AddHookAfterGetCellMeta(Func<AfterGetCellMetaArgs, Task> hook)
    {
        await AddHook("afterGetCellMeta", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftergetcolheader
    */
    public class AfterGetColHeaderArgs : BaseHookArgs
    {
        public AfterGetColHeaderArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftergetcolheader
    */
    public async Task AddHookAfterGetColHeader(Func<AfterGetColHeaderArgs, Task> hook)
    {
        await AddHook("afterGetColHeader", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftergetcolumnheaderrenderers
    */
    public class AfterGetColumnHeaderRenderersArgs : BaseHookArgs
    {
        public AfterGetColumnHeaderRenderersArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftergetcolumnheaderrenderers
    */
    public async Task AddHookAfterGetColumnHeaderRenderers(Func<AfterGetColumnHeaderRenderersArgs, Task> hook)
    {
        await AddHook("afterGetColumnHeaderRenderers", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftergetrowheader
    */
    public class AfterGetRowHeaderArgs : BaseHookArgs
    {
        public AfterGetRowHeaderArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftergetrowheader
    */
    public async Task AddHookAfterGetRowHeader(Func<AfterGetRowHeaderArgs, Task> hook)
    {
        await AddHook("afterGetRowHeader", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftergetrowheaderrenderers
    */
    public class AfterGetRowHeaderRenderersArgs : BaseHookArgs
    {
        public AfterGetRowHeaderRenderersArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftergetrowheaderrenderers
    */
    public async Task AddHookAfterGetRowHeaderRenderers(Func<AfterGetRowHeaderRenderersArgs, Task> hook)
    {
        await AddHook("afterGetRowHeaderRenderers", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterhidecolumns
    */
    public class AfterHideColumnsArgs : BaseHookArgs
    {
        public AfterHideColumnsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterhidecolumns
    */
    public async Task AddHookAfterHideColumns(Func<AfterHideColumnsArgs, Task> hook)
    {
        await AddHook("afterHideColumns", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterhiderows
    */
    public class AfterHideRowsArgs : BaseHookArgs
    {
        public AfterHideRowsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterhiderows
    */
    public async Task AddHookAfterHideRows(Func<AfterHideRowsArgs, Task> hook)
    {
        await AddHook("afterHideRows", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterinit
    */
    public class AfterInitArgs : BaseHookArgs
    {
        public AfterInitArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterinit
    */
    public async Task AddHookAfterInit(Func<AfterInitArgs, Task> hook)
    {
        await AddHook("afterInit", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterlanguagechange
    */
    public class AfterLanguageChangeArgs : BaseHookArgs
    {
        public AfterLanguageChangeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterlanguagechange
    */
    public async Task AddHookAfterLanguageChange(Func<AfterLanguageChangeArgs, Task> hook)
    {
        await AddHook("afterLanguageChange", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterlisten
    */
    public class AfterListenArgs : BaseHookArgs
    {
        public AfterListenArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterlisten
    */
    public async Task AddHookAfterListen(Func<AfterListenArgs, Task> hook)
    {
        await AddHook("afterListen", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterloaddata
    */
    public class AfterLoadDataArgs : BaseHookArgs
    {
        public AfterLoadDataArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterloaddata
    */
    public async Task AddHookAfterLoadData(Func<AfterLoadDataArgs, Task> hook)
    {
        await AddHook("afterLoadData", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftermergecells
    */
    public class AfterMergeCellsArgs : BaseHookArgs
    {
        public AfterMergeCellsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftermergecells
    */
    public async Task AddHookAfterMergeCells(Func<AfterMergeCellsArgs, Task> hook)
    {
        await AddHook("afterMergeCells", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftermodifytransformend
    */
    public class AfterModifyTransformEndArgs : BaseHookArgs
    {
        public AfterModifyTransformEndArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftermodifytransformend
    */
    public async Task AddHookAfterModifyTransformEnd(Func<AfterModifyTransformEndArgs, Task> hook)
    {
        await AddHook("afterModifyTransformEnd", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftermodifytransformfocus
    */
    public class AfterModifyTransformFocusArgs : BaseHookArgs
    {
        public AfterModifyTransformFocusArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftermodifytransformfocus
    */
    public async Task AddHookAfterModifyTransformFocus(Func<AfterModifyTransformFocusArgs, Task> hook)
    {
        await AddHook("afterModifyTransformFocus", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftermodifytransformstart
    */
    public class AfterModifyTransformStartArgs : BaseHookArgs
    {
        public AfterModifyTransformStartArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftermodifytransformstart
    */
    public async Task AddHookAfterModifyTransformStart(Func<AfterModifyTransformStartArgs, Task> hook)
    {
        await AddHook("afterModifyTransformStart", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftermomentumscroll
    */
    public class AfterMomentumScrollArgs : BaseHookArgs
    {
        public AfterMomentumScrollArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftermomentumscroll
    */
    public async Task AddHookAfterMomentumScroll(Func<AfterMomentumScrollArgs, Task> hook)
    {
        await AddHook("afterMomentumScroll", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afternamedexpressionadded
    */
    public class AfterNamedExpressionAddedArgs : BaseHookArgs
    {
        public AfterNamedExpressionAddedArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afternamedexpressionadded
    */
    public async Task AddHookAfterNamedExpressionAdded(Func<AfterNamedExpressionAddedArgs, Task> hook)
    {
        await AddHook("afterNamedExpressionAdded", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afternamedexpressionremoved
    */
    public class AfterNamedExpressionRemovedArgs : BaseHookArgs
    {
        public AfterNamedExpressionRemovedArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afternamedexpressionremoved
    */
    public async Task AddHookAfterNamedExpressionRemoved(Func<AfterNamedExpressionRemovedArgs, Task> hook)
    {
        await AddHook("afterNamedExpressionRemoved", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellcontextmenu
    */
    public class AfterOnCellContextMenuArgs : BaseHookArgs
    {
        public AfterOnCellContextMenuArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellcontextmenu
    */
    public async Task AddHookAfterOnCellContextMenu(Func<AfterOnCellContextMenuArgs, Task> hook)
    {
        await AddHook("afterOnCellContextMenu", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellcornerdblclick
    */
    public class AfterOnCellCornerDblClickArgs : BaseHookArgs
    {
        public AfterOnCellCornerDblClickArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellcornerdblclick
    */
    public async Task AddHookAfterOnCellCornerDblClick(Func<AfterOnCellCornerDblClickArgs, Task> hook)
    {
        await AddHook("afterOnCellCornerDblClick", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellcornermousedown
    */
    public class AfterOnCellCornerMouseDownArgs : BaseHookArgs
    {
        public AfterOnCellCornerMouseDownArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellcornermousedown
    */
    public async Task AddHookAfterOnCellCornerMouseDown(Func<AfterOnCellCornerMouseDownArgs, Task> hook)
    {
        await AddHook("afterOnCellCornerMouseDown", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellmousedown
    */
    public class AfterOnCellMouseDownArgs : BaseHookArgs
    {
        public AfterOnCellMouseDownArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellmousedown
    */
    public async Task AddHookAfterOnCellMouseDown(Func<AfterOnCellMouseDownArgs, Task> hook)
    {
        await AddHook("afterOnCellMouseDown", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellmouseout
    */
    public class AfterOnCellMouseOutArgs : BaseHookArgs
    {
        public AfterOnCellMouseOutArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellmouseout
    */
    public async Task AddHookAfterOnCellMouseOut(Func<AfterOnCellMouseOutArgs, Task> hook)
    {
        await AddHook("afterOnCellMouseOut", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellmouseover
    */
    public class AfterOnCellMouseOverArgs : BaseHookArgs
    {
        public AfterOnCellMouseOverArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellmouseover
    */
    public async Task AddHookAfterOnCellMouseOver(Func<AfterOnCellMouseOverArgs, Task> hook)
    {
        await AddHook("afterOnCellMouseOver", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellmouseup
    */
    public class AfterOnCellMouseUpArgs : BaseHookArgs
    {
        public AfterOnCellMouseUpArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteroncellmouseup
    */
    public async Task AddHookAfterOnCellMouseUp(Func<AfterOnCellMouseUpArgs, Task> hook)
    {
        await AddHook("afterOnCellMouseUp", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterpaste
    */
    public class AfterPasteArgs : BaseHookArgs
    {
        public AfterPasteArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterpaste
    */
    public async Task AddHookAfterPaste(Func<AfterPasteArgs, Task> hook)
    {
        await AddHook("afterPaste", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterpluginsinitialized
    */
    public class AfterPluginsInitializedArgs : BaseHookArgs
    {
        public AfterPluginsInitializedArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterpluginsinitialized
    */
    public async Task AddHookAfterPluginsInitialized(Func<AfterPluginsInitializedArgs, Task> hook)
    {
        await AddHook("afterPluginsInitialized", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterredo
    */
    public class AfterRedoArgs : BaseHookArgs
    {
        public AfterRedoArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterredo
    */
    public async Task AddHookAfterRedo(Func<AfterRedoArgs, Task> hook)
    {
        await AddHook("afterRedo", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterredostackchange
    */
    public class AfterRedoStackChangeArgs : BaseHookArgs
    {
        public AfterRedoStackChangeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterredostackchange
    */
    public async Task AddHookAfterRedoStackChange(Func<AfterRedoStackChangeArgs, Task> hook)
    {
        await AddHook("afterRedoStackChange", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterrefreshdimensions
    */
    public class AfterRefreshDimensionsArgs : BaseHookArgs
    {
        public AfterRefreshDimensionsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterrefreshdimensions
    */
    public async Task AddHookAfterRefreshDimensions(Func<AfterRefreshDimensionsArgs, Task> hook)
    {
        await AddHook("afterRefreshDimensions", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterremovecellmeta
    */
    public class AfterRemoveCellMetaArgs : BaseHookArgs
    {
        public AfterRemoveCellMetaArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterremovecellmeta
    */
    public async Task AddHookAfterRemoveCellMeta(Func<AfterRemoveCellMetaArgs, Task> hook)
    {
        await AddHook("afterRemoveCellMeta", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterremovecol
    */
    public class AfterRemoveColArgs : BaseCreateIndexArgs
    {
        public AfterRemoveColArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterremovecol
    */
    public async Task AddHookAfterRemoveCol(Func<AfterRemoveColArgs, Task> hook)
    {
        await AddHook("afterRemoveCol", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterremoverow
    */
    public class AfterRemoveRowArgs : BaseCreateIndexArgs
    {
        public AfterRemoveRowArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterremoverow
    */
    public async Task AddHookAfterRemoveRow(Func<AfterRemoveRowArgs, Task> hook)
    {
        await AddHook("afterRemoveRow", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterrender
    */
    public class AfterRenderArgs : BaseHookArgs
    {
        public AfterRenderArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterrender
    */
    public async Task AddHookAfterRender(Func<AfterRenderArgs, Task> hook)
    {
        await AddHook("afterRender", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterrenderer
    */
    public class AfterRendererArgs : BaseHookArgs
    {
        public AfterRendererArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterrenderer
    */
    public async Task AddHookAfterRenderer(Func<AfterRendererArgs, Task> hook)
    {
        await AddHook("afterRenderer", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterrowmove
    */
    public class AfterRowMoveArgs : BaseHookArgs
    {
        public AfterRowMoveArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterrowmove
    */
    public async Task AddHookAfterRowMove(Func<AfterRowMoveArgs, Task> hook)
    {
        await AddHook("afterRowMove", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterrowresize
    */
    public class AfterRowResizeArgs : BaseHookArgs
    {
        public AfterRowResizeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterrowresize
    */
    public async Task AddHookAfterRowResize(Func<AfterRowResizeArgs, Task> hook)
    {
        await AddHook("afterRowResize", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterrowsequencechange
    */
    public class AfterRowSequenceChangeArgs : BaseHookArgs
    {
        public AfterRowSequenceChangeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterrowsequencechange
    */
    public async Task AddHookAfterRowSequenceChange(Func<AfterRowSequenceChangeArgs, Task> hook)
    {
        await AddHook("afterRowSequenceChange", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterscroll
    */
    public class AfterScrollArgs : BaseHookArgs
    {
        public AfterScrollArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterscroll
    */
    public async Task AddHookAfterScroll(Func<AfterScrollArgs, Task> hook)
    {
        await AddHook("afterScroll", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterscrollhorizontally
    */
    public class AfterScrollHorizontallyArgs : BaseHookArgs
    {
        public AfterScrollHorizontallyArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterscrollhorizontally
    */
    public async Task AddHookAfterScrollHorizontally(Func<AfterScrollHorizontallyArgs, Task> hook)
    {
        await AddHook("afterScrollHorizontally", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterscrollvertically
    */
    public class AfterScrollVerticallyArgs : BaseHookArgs
    {
        public AfterScrollVerticallyArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterscrollvertically
    */
    public async Task AddHookAfterScrollVertically(Func<AfterScrollVerticallyArgs, Task> hook)
    {
        await AddHook("afterScrollVertically", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselectcolumns
    */
    public class AfterSelectColumnsArgs : BaseHookArgs
    {
        public AfterSelectColumnsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselectcolumns
    */
    public async Task AddHookAfterSelectColumns(Func<AfterSelectColumnsArgs, Task> hook)
    {
        await AddHook("afterSelectColumns", hook);
    }



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
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselection
    */
    public async Task AddHookAfterSelection(Func<AfterSelectionArgs, Task> hook)
    {
        await AddHook("afterSelection", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselectionbyprop
    */
    public class AfterSelectionByPropArgs : BaseHookArgs
    {
        public AfterSelectionByPropArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselectionbyprop
    */
    public async Task AddHookAfterSelectionByProp(Func<AfterSelectionByPropArgs, Task> hook)
    {
        await AddHook("afterSelectionByProp", hook);
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
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselectionend
    */
    public async Task AddHookAfterSelectionEnd(Func<AfterSelectionEndArgs, Task> hook)
    {
        await AddHook("afterSelectionEnd", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselectionendbyprop
    */
    public class AfterSelectionEndByPropArgs : BaseHookArgs
    {
        public AfterSelectionEndByPropArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselectionendbyprop
    */
    public async Task AddHookAfterSelectionEndByProp(Func<AfterSelectionEndByPropArgs, Task> hook)
    {
        await AddHook("afterSelectionEndByProp", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselectionfocusset
    */
    public class AfterSelectionFocusSetArgs : BaseHookArgs
    {
        public AfterSelectionFocusSetArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselectionfocusset
    */
    public async Task AddHookAfterSelectionFocusSet(Func<AfterSelectionFocusSetArgs, Task> hook)
    {
        await AddHook("afterSelectionFocusSet", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselectrows
    */
    public class AfterSelectRowsArgs : BaseHookArgs
    {
        public AfterSelectRowsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterselectrows
    */
    public async Task AddHookAfterSelectRows(Func<AfterSelectRowsArgs, Task> hook)
    {
        await AddHook("afterSelectRows", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersetcellmeta
    */
    public class AfterSetCellMetaArgs : BaseHookArgs
    {
        public AfterSetCellMetaArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersetcellmeta
    */
    public async Task AddHookAfterSetCellMeta(Func<AfterSetCellMetaArgs, Task> hook)
    {
        await AddHook("afterSetCellMeta", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersetdataatcell
    */
    public class AfterSetDataAtCellArgs : BaseHookArgs
    {
        public AfterSetDataAtCellArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersetdataatcell
    */
    public async Task AddHookAfterSetDataAtCell(Func<AfterSetDataAtCellArgs, Task> hook)
    {
        await AddHook("afterSetDataAtCell", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersetdataatrowprop
    */
    public class AfterSetDataAtRowPropArgs : BaseHookArgs
    {
        public AfterSetDataAtRowPropArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersetdataatrowprop
    */
    public async Task AddHookAfterSetDataAtRowProp(Func<AfterSetDataAtRowPropArgs, Task> hook)
    {
        await AddHook("afterSetDataAtRowProp", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersetsourcedataatcell
    */
    public class AfterSetSourceDataAtCellArgs : BaseHookArgs
    {
        public AfterSetSourceDataAtCellArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersetsourcedataatcell
    */
    public async Task AddHookAfterSetSourceDataAtCell(Func<AfterSetSourceDataAtCellArgs, Task> hook)
    {
        await AddHook("afterSetSourceDataAtCell", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersheetadded
    */
    public class AfterSheetAddedArgs : BaseHookArgs
    {
        public AfterSheetAddedArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersheetadded
    */
    public async Task AddHookAfterSheetAdded(Func<AfterSheetAddedArgs, Task> hook)
    {
        await AddHook("afterSheetAdded", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersheetremoved
    */
    public class AfterSheetRemovedArgs : BaseHookArgs
    {
        public AfterSheetRemovedArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersheetremoved
    */
    public async Task AddHookAfterSheetRemoved(Func<AfterSheetRemovedArgs, Task> hook)
    {
        await AddHook("afterSheetRemoved", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersheetrenamed
    */
    public class AfterSheetRenamedArgs : BaseHookArgs
    {
        public AfterSheetRenamedArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftersheetrenamed
    */
    public async Task AddHookAfterSheetRenamed(Func<AfterSheetRenamedArgs, Task> hook)
    {
        await AddHook("afterSheetRenamed", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftertrimrow
    */
    public class AfterTrimRowArgs : BaseHookArgs
    {
        public AfterTrimRowArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftertrimrow
    */
    public async Task AddHookAfterTrimRow(Func<AfterTrimRowArgs, Task> hook)
    {
        await AddHook("afterTrimRow", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterundo
    */
    public class AfterUndoArgs : BaseHookArgs
    {
        public AfterUndoArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterundo
    */
    public async Task AddHookAfterUndo(Func<AfterUndoArgs, Task> hook)
    {
        await AddHook("afterUndo", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterundostackchange
    */
    public class AfterUndoStackChangeArgs : BaseHookArgs
    {
        public AfterUndoStackChangeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterundostackchange
    */
    public async Task AddHookAfterUndoStackChange(Func<AfterUndoStackChangeArgs, Task> hook)
    {
        await AddHook("afterUndoStackChange", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterunhidecolumns
    */
    public class AfterUnhideColumnsArgs : BaseHookArgs
    {
        public AfterUnhideColumnsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterunhidecolumns
    */
    public async Task AddHookAfterUnhideColumns(Func<AfterUnhideColumnsArgs, Task> hook)
    {
        await AddHook("afterUnhideColumns", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterunhiderows
    */
    public class AfterUnhideRowsArgs : BaseHookArgs
    {
        public AfterUnhideRowsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterunhiderows
    */
    public async Task AddHookAfterUnhideRows(Func<AfterUnhideRowsArgs, Task> hook)
    {
        await AddHook("afterUnhideRows", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterunlisten
    */
    public class AfterUnlistenArgs : BaseHookArgs
    {
        public AfterUnlistenArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterunlisten
    */
    public async Task AddHookAfterUnlisten(Func<AfterUnlistenArgs, Task> hook)
    {
        await AddHook("afterUnlisten", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterunmergecells
    */
    public class AfterUnmergeCellsArgs : BaseHookArgs
    {
        public AfterUnmergeCellsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterunmergecells
    */
    public async Task AddHookAfterUnmergeCells(Func<AfterUnmergeCellsArgs, Task> hook)
    {
        await AddHook("afterUnmergeCells", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteruntrimrow
    */
    public class AfterUntrimRowArgs : BaseHookArgs
    {
        public AfterUntrimRowArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afteruntrimrow
    */
    public async Task AddHookAfterUntrimRow(Func<AfterUntrimRowArgs, Task> hook)
    {
        await AddHook("afterUntrimRow", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterupdatedata
    */
    public class AfterUpdateDataArgs : BaseHookArgs
    {
        public AfterUpdateDataArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterupdatedata
    */
    public async Task AddHookAfterUpdateData(Func<AfterUpdateDataArgs, Task> hook)
    {
        await AddHook("afterUpdateData", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterupdatesettings
    */
    public class AfterUpdateSettingsArgs : BaseHookArgs
    {
        public AfterUpdateSettingsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterupdatesettings
    */
    public async Task AddHookAfterUpdateSettings(Func<AfterUpdateSettingsArgs, Task> hook)
    {
        await AddHook("afterUpdateSettings", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftervalidate
    */
    public class AfterValidateArgs : BaseHookArgs
    {
        public AfterValidateArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#aftervalidate
    */
    public async Task AddHookAfterValidate(Func<AfterValidateArgs, Task> hook)
    {
        await AddHook("afterValidate", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterviewportcolumncalculatoroverride
    */
    public class AfterViewportColumnCalculatorOverrideArgs : BaseHookArgs
    {
        public AfterViewportColumnCalculatorOverrideArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterviewportcolumncalculatoroverride
    */
    public async Task AddHookAfterViewportColumnCalculatorOverride(Func<AfterViewportColumnCalculatorOverrideArgs, Task> hook)
    {
        await AddHook("afterViewportColumnCalculatorOverride", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterviewportrowcalculatoroverride
    */
    public class AfterViewportRowCalculatorOverrideArgs : BaseHookArgs
    {
        public AfterViewportRowCalculatorOverrideArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterviewportrowcalculatoroverride
    */
    public async Task AddHookAfterViewportRowCalculatorOverride(Func<AfterViewportRowCalculatorOverrideArgs, Task> hook)
    {
        await AddHook("afterViewportRowCalculatorOverride", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterviewrender
    */
    public class AfterViewRenderArgs : BaseHookArgs
    {
        public AfterViewRenderArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#afterviewrender
    */
    public async Task AddHookAfterViewRender(Func<AfterViewRenderArgs, Task> hook)
    {
        await AddHook("afterViewRender", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeaddchild
    */
    public class BeforeAddChildArgs : BaseHookArgs
    {
        public BeforeAddChildArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeaddchild
    */
    public async Task AddHookBeforeAddChild(Func<BeforeAddChildArgs, bool> hook)
    {
        await AddSyncHook("beforeAddChild", hook);
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
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeautofill
    */
    public async Task AddHookBeforeAutofill(Func<BeforeAutofillArgs, bool> hook)
    {
        await AddSyncHook("beforeAutofill", hook);
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
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforebeginediting
    */
    public async Task AddHookBeforeBeginEditing(Func<BeforeBeginEditingArgs, bool> hook)
    {
        await AddSyncHook("beforeBeginEditing", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecellalignment
    */
    public class BeforeCellAlignmentArgs : BaseHookArgs
    {
        public BeforeCellAlignmentArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecellalignment
    */
    public async Task AddHookBeforeCellAlignment(Func<BeforeCellAlignmentArgs, bool> hook)
    {
        await AddSyncHook("beforeCellAlignment", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforechange
    */
    public class BeforeChangeArgs : BaseHookArgs
    {
        public BeforeChangeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforechange
    */
    public async Task AddHookBeforeChange(Func<BeforeChangeArgs, bool> hook)
    {
        await AddSyncHook("beforeChange", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforechangerender
    */
    public class BeforeChangeRenderArgs : BaseHookArgs
    {
        public BeforeChangeRenderArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforechangerender
    */
    public async Task AddHookBeforeChangeRender(Func<BeforeChangeRenderArgs, bool> hook)
    {
        await AddSyncHook("beforeChangeRender", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumncollapse
    */
    public class BeforeColumnCollapseArgs : BaseHookArgs
    {
        public BeforeColumnCollapseArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumncollapse
    */
    public async Task AddHookBeforeColumnCollapse(Func<BeforeColumnCollapseArgs, bool> hook)
    {
        await AddSyncHook("beforeColumnCollapse", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnexpand
    */
    public class BeforeColumnExpandArgs : BaseHookArgs
    {
        public BeforeColumnExpandArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnexpand
    */
    public async Task AddHookBeforeColumnExpand(Func<BeforeColumnExpandArgs, bool> hook)
    {
        await AddSyncHook("beforeColumnExpand", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnfreeze
    */
    public class BeforeColumnFreezeArgs : BaseHookArgs
    {
        public BeforeColumnFreezeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnfreeze
    */
    public async Task AddHookBeforeColumnFreeze(Func<BeforeColumnFreezeArgs, bool> hook)
    {
        await AddSyncHook("beforeColumnFreeze", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnmove
    */
    public class BeforeColumnMoveArgs : BaseHookArgs
    {
        public BeforeColumnMoveArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnmove
    */
    public async Task AddHookBeforeColumnMove(Func<BeforeColumnMoveArgs, bool> hook)
    {
        await AddSyncHook("beforeColumnMove", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnresize
    */
    public class BeforeColumnResizeArgs : BaseHookArgs
    {
        public BeforeColumnResizeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnresize
    */
    public async Task AddHookBeforeColumnResize(Func<BeforeColumnResizeArgs, bool> hook)
    {
        await AddSyncHook("beforeColumnResize", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnsort
    */
    public class BeforeColumnSortArgs : BaseHookArgs
    {
        public BeforeColumnSortArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnsort
    */
    public async Task AddHookBeforeColumnSort(Func<BeforeColumnSortArgs, bool> hook)
    {
        await AddSyncHook("beforeColumnSort", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnunfreeze
    */
    public class BeforeColumnUnfreezeArgs : BaseHookArgs
    {
        public BeforeColumnUnfreezeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnunfreeze
    */
    public async Task AddHookBeforeColumnUnfreeze(Func<BeforeColumnUnfreezeArgs, bool> hook)
    {
        await AddSyncHook("beforeColumnUnfreeze", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnwrap
    */
    public class BeforeColumnWrapArgs : BaseHookArgs
    {
        public BeforeColumnWrapArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecolumnwrap
    */
    public async Task AddHookBeforeColumnWrap(Func<BeforeColumnWrapArgs, bool> hook)
    {
        await AddSyncHook("beforeColumnWrap", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecontextmenusetitems
    */
    public class BeforeContextMenuSetItemsArgs : BaseHookArgs
    {
        public BeforeContextMenuSetItemsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecontextmenusetitems
    */
    public async Task AddHookBeforeContextMenuSetItems(Func<BeforeContextMenuSetItemsArgs, bool> hook)
    {
        await AddSyncHook("beforeContextMenuSetItems", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecontextmenushow
    */
    public class BeforeContextMenuShowArgs : BaseHookArgs
    {
        public BeforeContextMenuShowArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecontextmenushow
    */
    public async Task AddHookBeforeContextMenuShow(Func<BeforeContextMenuShowArgs, bool> hook)
    {
        await AddSyncHook("beforeContextMenuShow", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecopy
    */
    public class BeforeCopyArgs : BaseHookArgs
    {
        public BeforeCopyArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecopy
    */
    public async Task AddHookBeforeCopy(Func<BeforeCopyArgs, bool> hook)
    {
        await AddSyncHook("beforeCopy", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecreatecol
    */
    public class BeforeCreateColArgs : BaseHookArgs
    {
        public BeforeCreateColArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecreatecol
    */
    public async Task AddHookBeforeCreateCol(Func<BeforeCreateColArgs, bool> hook)
    {
        await AddSyncHook("beforeCreateCol", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecreaterow
    */
    public class BeforeCreateRowArgs : BaseHookArgs
    {
        public BeforeCreateRowArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecreaterow
    */
    public async Task AddHookBeforeCreateRow(Func<BeforeCreateRowArgs, bool> hook)
    {
        await AddSyncHook("beforeCreateRow", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecut
    */
    public class BeforeCutArgs : BaseHookArgs
    {
        public BeforeCutArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforecut
    */
    public async Task AddHookBeforeCut(Func<BeforeCutArgs, bool> hook)
    {
        await AddSyncHook("beforeCut", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforedetachchild
    */
    public class BeforeDetachChildArgs : BaseHookArgs
    {
        public BeforeDetachChildArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforedetachchild
    */
    public async Task AddHookBeforeDetachChild(Func<BeforeDetachChildArgs, bool> hook)
    {
        await AddSyncHook("beforeDetachChild", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforedrawborders
    */
    public class BeforeDrawBordersArgs : BaseHookArgs
    {
        public BeforeDrawBordersArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforedrawborders
    */
    public async Task AddHookBeforeDrawBorders(Func<BeforeDrawBordersArgs, bool> hook)
    {
        await AddSyncHook("beforeDrawBorders", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforedropdownmenusetitems
    */
    public class BeforeDropdownMenuSetItemsArgs : BaseHookArgs
    {
        public BeforeDropdownMenuSetItemsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforedropdownmenusetitems
    */
    public async Task AddHookBeforeDropdownMenuSetItems(Func<BeforeDropdownMenuSetItemsArgs, bool> hook)
    {
        await AddSyncHook("beforeDropdownMenuSetItems", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforedropdownmenushow
    */
    public class BeforeDropdownMenuShowArgs : BaseHookArgs
    {
        public BeforeDropdownMenuShowArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforedropdownmenushow
    */
    public async Task AddHookBeforeDropdownMenuShow(Func<BeforeDropdownMenuShowArgs, bool> hook)
    {
        await AddSyncHook("beforeDropdownMenuShow", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforefilter
    */
    public class BeforeFilterArgs : BaseHookArgs
    {
        public BeforeFilterArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforefilter
    */
    public async Task AddHookBeforeFilter(Func<BeforeFilterArgs, bool> hook)
    {
        await AddSyncHook("beforeFilter", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforegetcellmeta
    */
    public class BeforeGetCellMetaArgs : BaseHookArgs
    {
        public BeforeGetCellMetaArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforegetcellmeta
    */
    public async Task AddHookBeforeGetCellMeta(Func<BeforeGetCellMetaArgs, bool> hook)
    {
        await AddSyncHook("beforeGetCellMeta", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforehidecolumns
    */
    public class BeforeHideColumnsArgs : BaseHookArgs
    {
        public BeforeHideColumnsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforehidecolumns
    */
    public async Task AddHookBeforeHideColumns(Func<BeforeHideColumnsArgs, bool> hook)
    {
        await AddSyncHook("beforeHideColumns", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforehiderows
    */
    public class BeforeHideRowsArgs : BaseHookArgs
    {
        public BeforeHideRowsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforehiderows
    */
    public async Task AddHookBeforeHideRows(Func<BeforeHideRowsArgs, bool> hook)
    {
        await AddSyncHook("beforeHideRows", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforehighlightingcolumnheader
    */
    public class BeforeHighlightingColumnHeaderArgs : BaseHookArgs
    {
        public BeforeHighlightingColumnHeaderArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforehighlightingcolumnheader
    */
    public async Task AddHookBeforeHighlightingColumnHeader(Func<BeforeHighlightingColumnHeaderArgs, bool> hook)
    {
        await AddSyncHook("beforeHighlightingColumnHeader", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforehighlightingrowheader
    */
    public class BeforeHighlightingRowHeaderArgs : BaseHookArgs
    {
        public BeforeHighlightingRowHeaderArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforehighlightingrowheader
    */
    public async Task AddHookBeforeHighlightingRowHeader(Func<BeforeHighlightingRowHeaderArgs, bool> hook)
    {
        await AddSyncHook("beforeHighlightingRowHeader", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeinit
    */
    public class BeforeInitArgs : BaseHookArgs
    {
        public BeforeInitArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeinit
    */
    public async Task AddHookBeforeInit(Func<BeforeInitArgs, bool> hook)
    {
        await AddSyncHook("beforeInit", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeinitwalkontable
    */
    public class BeforeInitWalkontableArgs : BaseHookArgs
    {
        public BeforeInitWalkontableArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeinitwalkontable
    */
    public async Task AddHookBeforeInitWalkontable(Func<BeforeInitWalkontableArgs, bool> hook)
    {
        await AddSyncHook("beforeInitWalkontable", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforekeydown
    */
    public class BeforeKeyDownArgs : BaseHookArgs
    {
        public BeforeKeyDownArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforekeydown
    */
    public async Task AddHookBeforeKeyDown(Func<BeforeKeyDownArgs, bool> hook)
    {
        await AddSyncHook("beforeKeyDown", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforelanguagechange
    */
    public class BeforeLanguageChangeArgs : BaseHookArgs
    {
        public BeforeLanguageChangeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforelanguagechange
    */
    public async Task AddHookBeforeLanguageChange(Func<BeforeLanguageChangeArgs, bool> hook)
    {
        await AddSyncHook("beforeLanguageChange", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeloaddata
    */
    public class BeforeLoadDataArgs : BaseHookArgs
    {
        public BeforeLoadDataArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeloaddata
    */
    public async Task AddHookBeforeLoadData(Func<BeforeLoadDataArgs, bool> hook)
    {
        await AddSyncHook("beforeLoadData", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforemergecells
    */
    public class BeforeMergeCellsArgs : BaseHookArgs
    {
        public BeforeMergeCellsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforemergecells
    */
    public async Task AddHookBeforeMergeCells(Func<BeforeMergeCellsArgs, bool> hook)
    {
        await AddSyncHook("beforeMergeCells", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeoncellcontextmenu
    */
    public class BeforeOnCellContextMenuArgs : BaseHookArgs
    {
        public BeforeOnCellContextMenuArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeoncellcontextmenu
    */
    public async Task AddHookBeforeOnCellContextMenu(Func<BeforeOnCellContextMenuArgs, bool> hook)
    {
        await AddSyncHook("beforeOnCellContextMenu", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeoncellmousedown
    */
    public class BeforeOnCellMouseDownArgs : BaseHookArgs
    {
        public BeforeOnCellMouseDownArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeoncellmousedown
    */
    public async Task AddHookBeforeOnCellMouseDown(Func<BeforeOnCellMouseDownArgs, bool> hook)
    {
        await AddSyncHook("beforeOnCellMouseDown", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeoncellmouseout
    */
    public class BeforeOnCellMouseOutArgs : BaseHookArgs
    {
        public BeforeOnCellMouseOutArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeoncellmouseout
    */
    public async Task AddHookBeforeOnCellMouseOut(Func<BeforeOnCellMouseOutArgs, bool> hook)
    {
        await AddSyncHook("beforeOnCellMouseOut", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeoncellmouseover
    */
    public class BeforeOnCellMouseOverArgs : BaseHookArgs
    {
        public BeforeOnCellMouseOverArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeoncellmouseover
    */
    public async Task AddHookBeforeOnCellMouseOver(Func<BeforeOnCellMouseOverArgs, bool> hook)
    {
        await AddSyncHook("beforeOnCellMouseOver", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeoncellmouseup
    */
    public class BeforeOnCellMouseUpArgs : BaseHookArgs
    {
        public BeforeOnCellMouseUpArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeoncellmouseup
    */
    public async Task AddHookBeforeOnCellMouseUp(Func<BeforeOnCellMouseUpArgs, bool> hook)
    {
        await AddSyncHook("beforeOnCellMouseUp", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforepaste
    */
    public class BeforePasteArgs : BaseHookArgs
    {
        public BeforePasteArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforepaste
    */
    public async Task AddHookBeforePaste(Func<BeforePasteArgs, bool> hook)
    {
        await AddSyncHook("beforePaste", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeredo
    */
    public class BeforeRedoArgs : BaseHookArgs
    {
        public BeforeRedoArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeredo
    */
    public async Task AddHookBeforeRedo(Func<BeforeRedoArgs, bool> hook)
    {
        await AddSyncHook("beforeRedo", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeredostackchange
    */
    public class BeforeRedoStackChangeArgs : BaseHookArgs
    {
        public BeforeRedoStackChangeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeredostackchange
    */
    public async Task AddHookBeforeRedoStackChange(Func<BeforeRedoStackChangeArgs, bool> hook)
    {
        await AddSyncHook("beforeRedoStackChange", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforerefreshdimensions
    */
    public class BeforeRefreshDimensionsArgs : BaseHookArgs
    {
        public BeforeRefreshDimensionsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforerefreshdimensions
    */
    public async Task AddHookBeforeRefreshDimensions(Func<BeforeRefreshDimensionsArgs, bool> hook)
    {
        await AddSyncHook("beforeRefreshDimensions", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeremovecellclassnames
    */
    public class BeforeRemoveCellClassNamesArgs : BaseHookArgs
    {
        public BeforeRemoveCellClassNamesArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeremovecellclassnames
    */
    public async Task AddHookBeforeRemoveCellClassNames(Func<BeforeRemoveCellClassNamesArgs, bool> hook)
    {
        await AddSyncHook("beforeRemoveCellClassNames", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeremovecellmeta
    */
    public class BeforeRemoveCellMetaArgs : BaseHookArgs
    {
        public BeforeRemoveCellMetaArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeremovecellmeta
    */
    public async Task AddHookBeforeRemoveCellMeta(Func<BeforeRemoveCellMetaArgs, bool> hook)
    {
        await AddSyncHook("beforeRemoveCellMeta", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeremovecol
    */
    public class BeforeRemoveColArgs : BaseHookArgs
    {
        public BeforeRemoveColArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeremovecol
    */
    public async Task AddHookBeforeRemoveCol(Func<BeforeRemoveColArgs, bool> hook)
    {
        await AddSyncHook("beforeRemoveCol", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeremoverow
    */
    public class BeforeRemoveRowArgs : BaseHookArgs
    {
        public BeforeRemoveRowArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeremoverow
    */
    public async Task AddHookBeforeRemoveRow(Func<BeforeRemoveRowArgs, bool> hook)
    {
        await AddSyncHook("beforeRemoveRow", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforerender
    */
    public class BeforeRenderArgs : BaseHookArgs
    {
        public BeforeRenderArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforerender
    */
    public async Task AddHookBeforeRender(Func<BeforeRenderArgs, bool> hook)
    {
        await AddSyncHook("beforeRender", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforerenderer
    */
    public class BeforeRendererArgs : BaseHookArgs
    {
        public BeforeRendererArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforerenderer
    */
    public async Task AddHookBeforeRenderer(Func<BeforeRendererArgs, bool> hook)
    {
        await AddSyncHook("beforeRenderer", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforerowmove
    */
    public class BeforeRowMoveArgs : BaseHookArgs
    {
        public BeforeRowMoveArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforerowmove
    */
    public async Task AddHookBeforeRowMove(Func<BeforeRowMoveArgs, bool> hook)
    {
        await AddSyncHook("beforeRowMove", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforerowresize
    */
    public class BeforeRowResizeArgs : BaseHookArgs
    {
        public BeforeRowResizeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforerowresize
    */
    public async Task AddHookBeforeRowResize(Func<BeforeRowResizeArgs, bool> hook)
    {
        await AddSyncHook("beforeRowResize", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforerowwrap
    */
    public class BeforeRowWrapArgs : BaseHookArgs
    {
        public BeforeRowWrapArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforerowwrap
    */
    public async Task AddHookBeforeRowWrap(Func<BeforeRowWrapArgs, bool> hook)
    {
        await AddSyncHook("beforeRowWrap", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeselectcolumns
    */
    public class BeforeSelectColumnsArgs : BaseHookArgs
    {
        public BeforeSelectColumnsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeselectcolumns
    */
    public async Task AddHookBeforeSelectColumns(Func<BeforeSelectColumnsArgs, bool> hook)
    {
        await AddSyncHook("beforeSelectColumns", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeselectionfocusset
    */
    public class BeforeSelectionFocusSetArgs : BaseHookArgs
    {
        public BeforeSelectionFocusSetArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeselectionfocusset
    */
    public async Task AddHookBeforeSelectionFocusSet(Func<BeforeSelectionFocusSetArgs, bool> hook)
    {
        await AddSyncHook("beforeSelectionFocusSet", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeselectionhighlightset
    */
    public class BeforeSelectionHighlightSetArgs : BaseHookArgs
    {
        public BeforeSelectionHighlightSetArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeselectionhighlightset
    */
    public async Task AddHookBeforeSelectionHighlightSet(Func<BeforeSelectionHighlightSetArgs, bool> hook)
    {
        await AddSyncHook("beforeSelectionHighlightSet", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeselectrows
    */
    public class BeforeSelectRowsArgs : BaseHookArgs
    {
        public BeforeSelectRowsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeselectrows
    */
    public async Task AddHookBeforeSelectRows(Func<BeforeSelectRowsArgs, bool> hook)
    {
        await AddSyncHook("beforeSelectRows", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforesetcellmeta
    */
    public class BeforeSetCellMetaArgs : BaseHookArgs
    {
        public BeforeSetCellMetaArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforesetcellmeta
    */
    public async Task AddHookBeforeSetCellMeta(Func<BeforeSetCellMetaArgs, bool> hook)
    {
        await AddSyncHook("beforeSetCellMeta", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforesetrangeend
    */
    public class BeforeSetRangeEndArgs : BaseHookArgs
    {
        public BeforeSetRangeEndArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforesetrangeend
    */
    public async Task AddHookBeforeSetRangeEnd(Func<BeforeSetRangeEndArgs, bool> hook)
    {
        await AddSyncHook("beforeSetRangeEnd", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforesetrangestart
    */
    public class BeforeSetRangeStartArgs : BaseHookArgs
    {
        public BeforeSetRangeStartArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforesetrangestart
    */
    public async Task AddHookBeforeSetRangeStart(Func<BeforeSetRangeStartArgs, bool> hook)
    {
        await AddSyncHook("beforeSetRangeStart", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforesetrangestartonly
    */
    public class BeforeSetRangeStartOnlyArgs : BaseHookArgs
    {
        public BeforeSetRangeStartOnlyArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforesetrangestartonly
    */
    public async Task AddHookBeforeSetRangeStartOnly(Func<BeforeSetRangeStartOnlyArgs, bool> hook)
    {
        await AddSyncHook("beforeSetRangeStartOnly", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforestretchingcolumnwidth
    */
    public class BeforeStretchingColumnWidthArgs : BaseHookArgs
    {
        public BeforeStretchingColumnWidthArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforestretchingcolumnwidth
    */
    public async Task AddHookBeforeStretchingColumnWidth(Func<BeforeStretchingColumnWidthArgs, bool> hook)
    {
        await AddSyncHook("beforeStretchingColumnWidth", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforetouchscroll
    */
    public class BeforeTouchScrollArgs : BaseHookArgs
    {
        public BeforeTouchScrollArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforetouchscroll
    */
    public async Task AddHookBeforeTouchScroll(Func<BeforeTouchScrollArgs, bool> hook)
    {
        await AddSyncHook("beforeTouchScroll", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforetrimrow
    */
    public class BeforeTrimRowArgs : BaseHookArgs
    {
        public BeforeTrimRowArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforetrimrow
    */
    public async Task AddHookBeforeTrimRow(Func<BeforeTrimRowArgs, bool> hook)
    {
        await AddSyncHook("beforeTrimRow", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeundo
    */
    public class BeforeUndoArgs : BaseHookArgs
    {
        public BeforeUndoArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeundo
    */
    public async Task AddHookBeforeUndo(Func<BeforeUndoArgs, bool> hook)
    {
        await AddSyncHook("beforeUndo", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeundostackchange
    */
    public class BeforeUndoStackChangeArgs : BaseHookArgs
    {
        public BeforeUndoStackChangeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeundostackchange
    */
    public async Task AddHookBeforeUndoStackChange(Func<BeforeUndoStackChangeArgs, bool> hook)
    {
        await AddSyncHook("beforeUndoStackChange", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeunhidecolumns
    */
    public class BeforeUnhideColumnsArgs : BaseHookArgs
    {
        public BeforeUnhideColumnsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeunhidecolumns
    */
    public async Task AddHookBeforeUnhideColumns(Func<BeforeUnhideColumnsArgs, bool> hook)
    {
        await AddSyncHook("beforeUnhideColumns", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeunhiderows
    */
    public class BeforeUnhideRowsArgs : BaseHookArgs
    {
        public BeforeUnhideRowsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeunhiderows
    */
    public async Task AddHookBeforeUnhideRows(Func<BeforeUnhideRowsArgs, bool> hook)
    {
        await AddSyncHook("beforeUnhideRows", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeunmergecells
    */
    public class BeforeUnmergeCellsArgs : BaseHookArgs
    {
        public BeforeUnmergeCellsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeunmergecells
    */
    public async Task AddHookBeforeUnmergeCells(Func<BeforeUnmergeCellsArgs, bool> hook)
    {
        await AddSyncHook("beforeUnmergeCells", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeuntrimrow
    */
    public class BeforeUntrimRowArgs : BaseHookArgs
    {
        public BeforeUntrimRowArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeuntrimrow
    */
    public async Task AddHookBeforeUntrimRow(Func<BeforeUntrimRowArgs, bool> hook)
    {
        await AddSyncHook("beforeUntrimRow", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeupdatedata
    */
    public class BeforeUpdateDataArgs : BaseHookArgs
    {
        public BeforeUpdateDataArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeupdatedata
    */
    public async Task AddHookBeforeUpdateData(Func<BeforeUpdateDataArgs, bool> hook)
    {
        await AddSyncHook("beforeUpdateData", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforevalidate
    */
    public class BeforeValidateArgs : BaseHookArgs
    {
        public BeforeValidateArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforevalidate
    */
    public async Task AddHookBeforeValidate(Func<BeforeValidateArgs, bool> hook)
    {
        await AddSyncHook("beforeValidate", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforevaluerender
    */
    public class BeforeValueRenderArgs : BaseHookArgs
    {
        public BeforeValueRenderArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforevaluerender
    */
    public async Task AddHookBeforeValueRender(Func<BeforeValueRenderArgs, bool> hook)
    {
        await AddSyncHook("beforeValueRender", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeviewportscroll
    */
    public class BeforeViewportScrollArgs : BaseHookArgs
    {
        public BeforeViewportScrollArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeviewportscroll
    */
    public async Task AddHookBeforeViewportScroll(Func<BeforeViewportScrollArgs, bool> hook)
    {
        await AddSyncHook("beforeViewportScroll", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeviewportscrollhorizontally
    */
    public class BeforeViewportScrollHorizontallyArgs : BaseHookArgs
    {
        public BeforeViewportScrollHorizontallyArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeviewportscrollhorizontally
    */
    public async Task AddHookBeforeViewportScrollHorizontally(Func<BeforeViewportScrollHorizontallyArgs, bool> hook)
    {
        await AddSyncHook("beforeViewportScrollHorizontally", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeviewportscrollvertically
    */
    public class BeforeViewportScrollVerticallyArgs : BaseHookArgs
    {
        public BeforeViewportScrollVerticallyArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeviewportscrollvertically
    */
    public async Task AddHookBeforeViewportScrollVertically(Func<BeforeViewportScrollVerticallyArgs, bool> hook)
    {
        await AddSyncHook("beforeViewportScrollVertically", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeviewrender
    */
    public class BeforeViewRenderArgs : BaseHookArgs
    {
        public BeforeViewRenderArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#beforeviewrender
    */
    public async Task AddHookBeforeViewRender(Func<BeforeViewRenderArgs, bool> hook)
    {
        await AddSyncHook("beforeViewRender", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#construct
    */
    public class ConstructArgs : BaseHookArgs
    {
        public ConstructArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#construct
    */
    public async Task AddHookConstruct(Func<ConstructArgs, Task> hook)
    {
        await AddHook("construct", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#globalbucket
    */
    public class GlobalBucketArgs : BaseHookArgs
    {
        public GlobalBucketArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#globalbucket
    */
    public async Task AddHookGlobalBucket(Func<GlobalBucketArgs, Task> hook)
    {
        await AddHook("globalBucket", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#init
    */
    public class InitArgs : BaseHookArgs
    {
        public InitArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#init
    */
    public async Task AddHookInit(Func<InitArgs, Task> hook)
    {
        await AddHook("init", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyautocolumnsizeseed
    */
    public class ModifyAutoColumnSizeSeedArgs : BaseHookArgs
    {
        public ModifyAutoColumnSizeSeedArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyautocolumnsizeseed
    */
    public async Task AddHookModifyAutoColumnSizeSeed(Func<ModifyAutoColumnSizeSeedArgs, Task> hook)
    {
        await AddHook("modifyAutoColumnSizeSeed", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyautofillrange
    */
    public class ModifyAutofillRangeArgs : BaseHookArgs
    {
        public ModifyAutofillRangeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyautofillrange
    */
    public async Task AddHookModifyAutofillRange(Func<ModifyAutofillRangeArgs, Task> hook)
    {
        await AddHook("modifyAutofillRange", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifycolheader
    */
    public class ModifyColHeaderArgs : BaseHookArgs
    {
        public ModifyColHeaderArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifycolheader
    */
    public async Task AddHookModifyColHeader(Func<ModifyColHeaderArgs, Task> hook)
    {
        await AddHook("modifyColHeader", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifycolumnheaderheight
    */
    public class ModifyColumnHeaderHeightArgs : BaseHookArgs
    {
        public ModifyColumnHeaderHeightArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifycolumnheaderheight
    */
    public async Task AddHookModifyColumnHeaderHeight(Func<ModifyColumnHeaderHeightArgs, Task> hook)
    {
        await AddHook("modifyColumnHeaderHeight", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifycolumnheadervalue
    */
    public class ModifyColumnHeaderValueArgs : BaseHookArgs
    {
        public ModifyColumnHeaderValueArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifycolumnheadervalue
    */
    public async Task AddHookModifyColumnHeaderValue(Func<ModifyColumnHeaderValueArgs, Task> hook)
    {
        await AddHook("modifyColumnHeaderValue", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifycolwidth
    */
    public class ModifyColWidthArgs : BaseHookArgs
    {
        public ModifyColWidthArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifycolwidth
    */
    public async Task AddHookModifyColWidth(Func<ModifyColWidthArgs, Task> hook)
    {
        await AddHook("modifyColWidth", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifycopyablerange
    */
    public class ModifyCopyableRangeArgs : BaseHookArgs
    {
        public ModifyCopyableRangeArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifycopyablerange
    */
    public async Task AddHookModifyCopyableRange(Func<ModifyCopyableRangeArgs, Task> hook)
    {
        await AddHook("modifyCopyableRange", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifydata
    */
    public class ModifyDataArgs : BaseHookArgs
    {
        public ModifyDataArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifydata
    */
    public async Task AddHookModifyData(Func<ModifyDataArgs, Task> hook)
    {
        await AddHook("modifyData", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyfiltersmultiselectvalue
    */
    public class ModifyFiltersMultiSelectValueArgs : BaseHookArgs
    {
        public ModifyFiltersMultiSelectValueArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyfiltersmultiselectvalue
    */
    public async Task AddHookModifyFiltersMultiSelectValue(Func<ModifyFiltersMultiSelectValueArgs, Task> hook)
    {
        await AddHook("modifyFiltersMultiSelectValue", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyfocusedelement
    */
    public class ModifyFocusedElementArgs : BaseHookArgs
    {
        public ModifyFocusedElementArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyfocusedelement
    */
    public async Task AddHookModifyFocusedElement(Func<ModifyFocusedElementArgs, Task> hook)
    {
        await AddHook("modifyFocusedElement", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyfocusontabnavigation
    */
    public class ModifyFocusOnTabNavigationArgs : BaseHookArgs
    {
        public ModifyFocusOnTabNavigationArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyfocusontabnavigation
    */
    public async Task AddHookModifyFocusOnTabNavigation(Func<ModifyFocusOnTabNavigationArgs, Task> hook)
    {
        await AddHook("modifyFocusOnTabNavigation", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifygetcellcoords
    */
    public class ModifyGetCellCoordsArgs : BaseHookArgs
    {
        public ModifyGetCellCoordsArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifygetcellcoords
    */
    public async Task AddHookModifyGetCellCoords(Func<ModifyGetCellCoordsArgs, Task> hook)
    {
        await AddHook("modifyGetCellCoords", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyrowdata
    */
    public class ModifyRowDataArgs : BaseHookArgs
    {
        public ModifyRowDataArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyrowdata
    */
    public async Task AddHookModifyRowData(Func<ModifyRowDataArgs, Task> hook)
    {
        await AddHook("modifyRowData", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyrowheader
    */
    public class ModifyRowHeaderArgs : BaseHookArgs
    {
        public ModifyRowHeaderArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyrowheader
    */
    public async Task AddHookModifyRowHeader(Func<ModifyRowHeaderArgs, Task> hook)
    {
        await AddHook("modifyRowHeader", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyrowheaderwidth
    */
    public class ModifyRowHeaderWidthArgs : BaseHookArgs
    {
        public ModifyRowHeaderWidthArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyrowheaderwidth
    */
    public async Task AddHookModifyRowHeaderWidth(Func<ModifyRowHeaderWidthArgs, Task> hook)
    {
        await AddHook("modifyRowHeaderWidth", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyrowheight
    */
    public class ModifyRowHeightArgs : BaseHookArgs
    {
        public ModifyRowHeightArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifyrowheight
    */
    public async Task AddHookModifyRowHeight(Func<ModifyRowHeightArgs, Task> hook)
    {
        await AddHook("modifyRowHeight", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifysourcedata
    */
    public class ModifySourceDataArgs : BaseHookArgs
    {
        public ModifySourceDataArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifysourcedata
    */
    public async Task AddHookModifySourceData(Func<ModifySourceDataArgs, Task> hook)
    {
        await AddHook("modifySourceData", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifytransformend
    */
    public class ModifyTransformEndArgs : BaseHookArgs
    {
        public ModifyTransformEndArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifytransformend
    */
    public async Task AddHookModifyTransformEnd(Func<ModifyTransformEndArgs, Task> hook)
    {
        await AddHook("modifyTransformEnd", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifytransformfocus
    */
    public class ModifyTransformFocusArgs : BaseHookArgs
    {
        public ModifyTransformFocusArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifytransformfocus
    */
    public async Task AddHookModifyTransformFocus(Func<ModifyTransformFocusArgs, Task> hook)
    {
        await AddHook("modifyTransformFocus", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifytransformstart
    */
    public class ModifyTransformStartArgs : BaseHookArgs
    {
        public ModifyTransformStartArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#modifytransformstart
    */
    public async Task AddHookModifyTransformStart(Func<ModifyTransformStartArgs, Task> hook)
    {
        await AddHook("modifyTransformStart", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#persistentstateload
    */
    public class PersistentStateLoadArgs : BaseHookArgs
    {
        public PersistentStateLoadArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#persistentstateload
    */
    public async Task AddHookPersistentStateLoad(Func<PersistentStateLoadArgs, Task> hook)
    {
        await AddHook("persistentStateLoad", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#persistentstatereset
    */
    public class PersistentStateResetArgs : BaseHookArgs
    {
        public PersistentStateResetArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#persistentstatereset
    */
    public async Task AddHookPersistentStateReset(Func<PersistentStateResetArgs, Task> hook)
    {
        await AddHook("persistentStateReset", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#persistentstatesave
    */
    public class PersistentStateSaveArgs : BaseHookArgs
    {
        public PersistentStateSaveArgs(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#persistentstatesave
    */
    public async Task AddHookPersistentStateSave(Func<PersistentStateSaveArgs, Task> hook)
    {
        await AddHook("persistentStateSave", hook);
    }


   /**
    * See https://handsontable.com/docs/javascript-data-grid/api/hooks/#
    */
    public class Args : BaseHookArgs
    {
        public Args(string hookName, JsonDocument jdoc) 
            : base(hookName, jdoc)
        { }
    }

    /**
    * https://handsontable.com/docs/javascript-data-grid/api/hooks/#
    */
    public async Task AddHook(Func<Args, Task> hook)
    {
        await AddHook("", hook);
    }
}