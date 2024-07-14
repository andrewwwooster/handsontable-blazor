using System.Text.Json;
using Microsoft.JSInterop;
using static HandsontableBlazor.Hooks;
using static HandsontableBlazor.Callbacks;
using static HandsontableBlazor.Renderer;

namespace HandsontableBlazor.Interop;

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
    private IJSObjectReference _handsontableJsReference = null!;

    static IDictionary<string,Func<RendererArgs, Task>>     _rendererDict = new Dictionary<string,Func<RendererArgs, Task>>();
    private IDictionary<Tuple<string?,Delegate>, ICallbackProxy>  _hookProxyDict = new Dictionary<Tuple<string?,Delegate>, ICallbackProxy>();


    public HandsontableJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _handsontableModuleTask = new (() => _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/HandsontableBlazor/handsontableJsInterop.js").AsTask());
    }

    public async Task NewHandsontable (string elemId, ConfigurationOptions? configurationOptions) 
    {
        try
        {
            var module = await _handsontableModuleTask.Value;
            var thisObjectReference = DotNetObjectReference.Create(this);
            _handsontableJsReference = await module.InvokeAsync<IJSInProcessObjectReference>(
                "newHandsontable", elemId, configurationOptions, thisObjectReference);
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
            _handsontableJsReference = await module.InvokeAsync<IJSInProcessObjectReference>(
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

    /**
    * Alter the grid's structure by adding or removing rows and columns at specified positions.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#alter
    */
    public async Task Alter(
        AlterActionEnum alterAction, 
        int visualIndex, 
        int amount = 1, string? source = null, bool keepEmptyRows = false)
    {
        var alterActionStr = alterAction.ToString();
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "alter", 
            alterActionStr, visualIndex, amount, source, keepEmptyRows);
    }
    
    /**
    * Clears the data from the table (the table settings remain intact).
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#clear
    */
    public async Task Clear()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "clear");
    }

    /**
    * Clears the undo buffer.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#clearundo
    */
    public async Task ClearUndo()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "clearUndo");
    }

    /**
    * Returns the property name that corresponds with the given column index. If the data 
    * source is an array of arrays, it returns the column's index.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#coltoprop
    */
    public async Task<object> ColToProp(int visualColumn)
    {
        return await _handsontableJsReference.InvokeAsync<object>("invokeMethod", "colToProp", visualColumn);
    }

    /**
    * Same as ColToProp() but returns the column's name.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#coltoprop
    */
    public async Task<string> ColToPropString(int visualColumn)
    {
        var result = await ColToProp(visualColumn);
        return result.ToString()!;
    }

    /**
    * Returns the number of rendered column headers.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#countcolheaders
    */
    public async Task<int> CountColHeaders()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countColHeaders");
    }

    /**
    * Returns the total number of visible columns in the table.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#countcols
    */
    public async Task<int> CountCols()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countCols");
    }

    /**
    * Returns the number of empty columns. If the optional ending parameter is true, returns 
    * the number of empty columns at right hand edge of the table.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#countemptycols
    */
    public async Task<int> CountEmptyCols()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countEmptyCols");
    }

    /**
    * Returns the number of empty rows. If the optional ending parameter is true, returns 
    * the number of empty rows at the bottom of the table.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#countemptyrows
    */
    public async Task<int> CountEmptyRows()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countEmptyRows");
    }

    /**
    * Returns the number of rendered rows including columns that are partially or fully 
    * rendered outside the table viewport.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#countrenderedcols
    */
    public async Task<int> CountRenderedCols()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countRenderedCols");
    }

    /**
    * Returns the number of rendered rows including rows that are partially or fully rendered 
    * outside the table viewport.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#countrenderedrows
    */
    public async Task<int> CountRenderedRows()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countRenderedRows");
    }

    /**
    * Returns the total number of columns in the data source.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#countsourcecols
    */
    public async Task<int> CountSourceCols()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countSourceCols");
    }

    /**
    * Returns the total number of rows in the data source.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#countsourcerows
    */
    public async Task<int> CountSourceRows()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countSourceRows");
    }

    /**
    * Returns the number of rendered columns that are only visible in the table viewport. The columns 
    * that are partially visible are not counted.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#countvisiblecols
    */
    public async Task<int> CountVisibleCols()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countVisibleCols");
    }

    /**
    * Returns the number of rendered rows that are only visible in the table viewport. The rows that 
    * are partially visible are not counted.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#countvisiblerows
    */
    public async Task<int> CountVisibleRows()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countVisibleRows");
    }

    /**
    * Returns the number of row headers.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#countrowheaders
    */
    public async Task<int> CountRowHeaders()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countRowHeaders");
    }

    /**
    * Returns the total number of visual rows in the table.  
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#countrows
    */
    public async Task<int> CountRows()
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "countRows");
    }

    /**
    * Removes the table from the DOM and destroys the instance of the Handsontable.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#destroy
    */
    public async Task Destroy()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "destroy");
    }

    /**
    * Returns a TD element for the given row and column arguments, if it is rendered on screen. 
    * Returns null if the TD is not rendered on screen (probably because that part of the table 
    * is not visible).
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#getcell
    * @returns {JQueryJsInterop} Returns a jQuery object wrapping the TD element.
    */
    public async Task<JQueryJsInterop> GetCell (int visualRow, int visualColumn, bool topmost = false)
    {
        var htmlTableCellElement = await _handsontableJsReference.InvokeAsync<IJSInProcessObjectReference>(
            "invokeMethodReturnsJQuery", "getCell", visualRow, visualColumn, topmost);
        return new JQueryJsInterop(htmlTableCellElement);
    }

    /**
    * Erases content from cells that have been selected in the table.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#emptyselectedcells
    */
    public async Task EmptySelectedCells (string? source = null)
    {
        await _handsontableJsReference.InvokeAsync<IJSInProcessObjectReference>(
            "invokeMethodReturnsJQuery", "emptySelectedCells", source);
    }

    /**
    * Get all the cells meta settings at least once generated in the table (in order of cell initialization).
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#getcellsmeta
    */
    public async Task<IDictionary<string,object>> GetCellMeta (int visualRow, int visualColumn)
    {
        var cellProperties = await _handsontableJsReference.InvokeAsync<Dictionary<string,object>>(
            "invokeMethod", "getCellMeta", visualRow, visualColumn);
        return cellProperties;
    }

    /**
    * Returns an array of cell meta objects for specified physical row index.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#getcellmetaatrow
    */
    public async Task<IList<IDictionary<string,object>>> GetCellMetaAtRow (int physicalRow)
    {
        var cellMetaList = await _handsontableJsReference.InvokeAsync<List<IDictionary<string,object>>>(
            "invokeMethod", "getCellMetaAtRow", physicalRow);
        return cellMetaList;
    }

    /**
    * Get all the cells meta settings at least once generated in the table (in order of cell initialization).
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#getcellsmeta
    */
    public async Task<IList<IDictionary<string,object>>> GetCellsMeta ()
    {
        var cellMetaList = await _handsontableJsReference.InvokeAsync<List<IDictionary<string,object>>>(
            "invokeMethod", "getCellsMeta");
        return cellMetaList;
    }

    /**
    * Returns the width of the requested column.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#getcolwidth
    */
    public async Task<int> GetColWidth(int visualColumn)
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "getColWidth", visualColumn);
    }

    /**
    * Returns the current data object the same one that was passed by ConfigurationOptions.
    * or loadData method, unless some modifications have been applied 
    * (i.e. Sequence of rows/columns was changed, some row/column was skipped). 
    * If that's the case - use the GetSourceData() method.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#getdata
    */
    public async Task<IList<IList<object>>> GetData(int? visualRow, int? visualColumn, int? visualRow2, int? visualColumn2)
    {
        return await _handsontableJsReference.InvokeAsync<IList<IList<object>>>("invokeMethod", "getData", visualRow, visualColumn, visualRow2, visualColumn2);
    }

    /**
    * Returns a data type defined in the Handsontable settings under the type key (Options#type). If there are cells with different types in the selected range, it returns 'mixed'.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#getdatatype
    */
    public async Task<string> GetDataType(int visualRowFrom, int visualColumnFrom, int visualRowTo, int visualColumnTo)
    {
        return await _handsontableJsReference.InvokeAsync<string>("invokeMethod", "getDataType", visualRowFrom, visualColumnFrom, visualRowTo, visualColumnTo);
    }

    public async Task<int?> GetRowHeight(int visualRow)
    {
        return await _handsontableJsReference.InvokeAsync<int?>("invokeMethod", "getRowHeight", visualRow);
    }

    /**
    * Returns the current selection as an array of CellRange objects.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#getselectedrange
    */
    public async Task<IList<CellRange>?> GetSelectedRange()
    {
        var selecteds =  await _handsontableJsReference.InvokeAsync<IList<IList<int>>>("invokeMethod", "getSelected");
        if (selecteds == null) return null;

        var cellRanges = new List<CellRange>();
        foreach (var selected in selecteds)
        {
            cellRanges.Add(new CellRange(selected[0], selected[1], selected[2], selected[3]));
        }
        return cellRanges;
    }

    /**
    * Returns the last coordinates applied to the table as a CellRange object.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#getselectedrangelast
    */
    public async Task<CellRange?> GetSelectedRangeLast()
    {
        var selected =  await _handsontableJsReference.InvokeAsync<IList<int>>("invokeMethod", "getSelectedLast");
        if (selected == null) return null;
        var cellRange = new CellRange(selected[0], selected[1], selected[2], selected[3]);
        return cellRange;
    }

    public async Task<bool> HasColHeaders()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "hasColHeaders");
    }

    /**
    * Check if hook with a given name was registered.
    * @param {String} hookName Hook name should be name without "on" prefix.  Hook name
    *                   always starts with a lowcase character.
    */
    public async Task<bool> HasHook(string hookName)
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "hasHook", hookName);
    }
    
    /**
    * Returns information about if this table is configured to display row headers.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#hasrowheaders
    */
    public async Task<bool> HasRowHeaders()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "hasRowHeaders");
    }

    /**
    * Checks if your data format and configuration options allow
    * for changing the number of columns.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#iscolumnmodificationallowed
    */
    public async Task<bool> IsColumnModificationAllowed()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isColumnModificationAllowed");
    }

    /**
    * Check if all cells in the the column declared by the column argument are empty.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#isemptycol
    */
    public async Task<bool> IsEmptyCol(int visualColumn)
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isEmptyCol", visualColumn);
    }

    /**
    * Check if all cells in the row declared by the row argument are empty.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#isemptyrow
    */
    public async Task<bool> IsEmptyRow(int visualRow)
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isEmptyRow", visualRow);
    }

    /**
    * Checks if the table indexes recalculation process was suspended.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#isexecutionsuspended
    */
    public async Task<bool> IsExecutionSuspended()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isExecutionSuspended");
    }

    /**
    * Returns true if the current Handsontable instance is listening to keyboard 
    * input on document body.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#islistening
    */
    public async Task<bool> IsListening()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isListening");
    }

    /**
    * Checks if the grid is rendered using the left-to-right layout direction.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#isltr
    */
    public async Task<bool> IsLtr()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isLtr");
    }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#isredoavailable
    */
    public async Task<bool> IsRedoAvailable()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isRedoAvailable");
    }

    /**
    * Checks if the table rendering process was suspended.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#isrendersuspended
    */
    public async Task<bool> IsRenderSuspended()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isRenderSuspended");
    }

    /**
    * Checks if the grid is rendered using the right-to-left layout direction.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#isrtl
    */
    public async Task<bool> IsRtl()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isRtl");
    }

    /**
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#isundoavailable
    */
    public async Task<bool> IsUndoAvailable()
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "isUndoAvailable");
    }

    /**
    * Listen to the keyboard input on document body. This allows Handsontable to 
    * capture keyboard events and respond in the right way.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#listen
    */
    public async Task Listen()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "listen");
    }

    /**
    * Replaces Handsontable's data with a new dataset.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#isundoavailable
    */
    public async Task LoadData (IList<IList<object>> data, string? source = null)
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "loadData", data, source);
    }

    /**
    * Replaces Handsontable's data with a new dataset.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#isundoavailable
    */
    public async Task LoadData (IList<IDictionary<string, object>> data, string? source = null)
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "loadData", data, source);
    }

    /**
    * Populates cells at position with 2D input array (e.g. [[1, 2], [3, 4]]). Use endRow, 
    * endCol when you want to cut input when a certain row is reached.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#populatefromarray
    */
    public async Task<JQueryJsInterop> PopulateFromArray (
        int visualRow, int visualColumn, 
        IList<IList<object>> input, 
        int? visualRowEnd = null, int? visualColumnEnd = null, 
        string? source = "populateFromArray", string? method = "overwrite")
    {
        var tdJsObjectReference = await _handsontableJsReference.InvokeAsync<IJSInProcessObjectReference>("invokeMethodReturnsJQuery", "populateFromArray", 
            visualRow, visualColumn, input, visualRowEnd, visualColumnEnd, source, method);
        var tdJQuery = new JQueryJsInterop(tdJsObjectReference);
        return tdJQuery;
    }

    /**
    * Property to column.
    * @returns Visual column index.
    */
    public async Task<int> PropToCol(string prop)
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "propToCol", prop);
    }

    /**
    * @See https://handsontable.com/docs/javascript-data-grid/api/core/#redo
    */
    public async Task Redo ()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "redo");
    }

    /**
    * Updates dimensions of the table. The method compares previous dimensions with the current 
    * ones and updates accordingly.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#refreshdimensions
    */
    public async Task RefreshDimensions ()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "refreshDimensions");
    }

    /**
    * Remove a property defined by the key argument from the cell meta object for the provided 
    * visualRow and visualColumn coordinates.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#removecellmeta
    */
    public async Task RemoveCellMeta (int visualRow, int visualColumn, string key)
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "removeCellMeta", 
            visualRow, visualColumn, key);
    }

    /**
    * Rerender the table. Calling this method starts the process of recalculating, 
    * redrawing and applying the changes to the DOM. While rendering the table all 
    * cell renderers are recalled.
    *
    * Calling this method manually is not recommended. Handsontable tries to render 
    * itself by choosing the most optimal moments in its lifecycle.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#render
    */
    public async Task Render ()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "render");
    }

    /**
    * Resumes the execution process. In combination with the SuspendExecution 
    * method it allows aggregating the table logic changes after which the cache is 
    * updated. Resuming the state automatically invokes the table cache updating process.
    *
    * The method is intended to be used by advanced users. Suspending the execution 
    * process could cause visual glitches caused by not updated the internal table cache.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#resumeexecution
    */
    public async Task ResumeExecution (bool forceFlushChanges = false)
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "resumeExecution", forceFlushChanges);
    }

    /**
    * Resumes the rendering process. In combination with the SuspendRender
    * method it allows aggregating the table render cycles triggered by API 
    * calls or UI actions (or both) and calls the "render" once in the end. 
    * When the table is in the suspend state, most operations will have no 
    * visual effect until the rendering state is resumed. Resuming the state 
    * automatically invokes the table rendering.
    *
    * The method is intended to be used by advanced users. Suspending the 
    * rendering process could cause visual glitches when wrongly implemented.
    *
    * The method is intended to be used by advanced users. Suspending the execution 
    * process could cause visual glitches caused by not updated the internal table cache.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#resumerender
    */
    public async Task ResumeRender ()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "resumeRender");
    }

    /**
    * Scrolls the viewport to coordinates specified by the currently focused cell.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#scrolltofocusedcell
    */
    public async Task ScrollToFocusedCell (Func<ScrollToFocusedCellArgs,Task> callback)
    {
        var callbackProxy = new VoidAsyncCallbackProxy<ScrollToFocusedCellArgs>(callback);
        await _handsontableJsReference.InvokeVoidAsync("invokeMethodWithCallback", "scrollToFocusedCell", 
            callbackProxy);
    }    

   public class ScrollViewportToOptions
    {
        public int Row { get; set; }
        public int Col { get; set; }

        /**
        * Values: "start", "end"
        */
        public string? VerticalSnap { get; set; } = null;

        /**
        * Values: "start", "end"
        */
        public string? HorizontalSnap { get; set; } = null;

        public bool ConsiderHiddenIndexes { get; set; } = true;
    }

    /**
    * Scroll viewport to coordinates specified by the row and/or col object properties.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#scrollviewportto
    */
    public async Task<bool> ScrollViewportTo (ScrollViewportToOptions options)
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethodWithCallback", "scrollViewportTo", 
            options);
    }    

    public class SelectAllOptions
    {
        public object FocusPosition { get => FocusPositionCellCoords ?? (object) false;} 

        public CellCoords? FocusPositionCellCoords { get; set; }

        public bool DisableHeadersHighlight { get; set; } = true;
    }

    /**
    * Select all cells in the table excluding headers and corner elements.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#selectall
    */
    public async Task SelectAll ( 
        bool includeRowHeaders = false, bool includeColumnHeaders = false, SelectAllOptions? options = null)
    {
        if (options == null)
        {
            await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "selectAll", 
                includeRowHeaders, includeColumnHeaders);
        }
        else
        {
            await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "selectAll", 
                includeRowHeaders, includeColumnHeaders, options);
        }
    }

    /**
    * Select a single cell, or a single range of adjacent cells.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#selectcell
    */
    public async Task<bool> SelectCell (
        int visualRow, int visualColumn, 
        int? visualRowEnd = null, int? visualColumnEnd = null, 
        bool scrollToCell = true, bool changeListener = true)
    {
        visualRowEnd ??= visualRow;
        visualColumnEnd ??= visualColumn;
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "selectCell", 
            visualRow, visualColumn, visualRowEnd, visualColumnEnd, scrollToCell, changeListener);
    }

    /**
    * Select multiple cells or ranges of cells, adjacent or non-adjacent.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#selectcells
    */
    public async Task<bool> SelectCells (
        IList<IList<int>> coords, bool scrollToCell = true, bool changeListener = true)
    {
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "selectCells", 
            coords, scrollToCell, changeListener);
    }

    /**
    * Select column specified by visualColumnStart visual index, column property or a range of columns finishing at visualColumnEnd.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#selectcolumns
    */
    public async Task<bool> SelectColumns (
        int visualColumnStart, 
        int? visualColumnEnd = null, 
        int focusPosition = 0)
    {
        visualColumnEnd ??= visualColumnStart;
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "selectColumns", 
            visualColumnStart, visualColumnEnd, focusPosition);
    }

    /**
    * Select row specified by visualRowStart visual index or a range of rows finishing at visualRowEnd.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#selectrows
    */
    public async Task<bool> SelectRows (
        int visualRowStart, 
        int? visualRowEnd = null, 
        int focusPosition = 0)
    {
        visualRowEnd ??= visualRowStart;
        return await _handsontableJsReference.InvokeAsync<bool>("invokeMethod", "selectRows", 
            visualRowStart, visualRowEnd, focusPosition);
    }

    /**
    * Sets a property defined by the key property to the meta object of a cell corresponding to 
    * visualRow and visualColumn.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#setcellmeta 
    */
    public async Task SetCellMeta (int visualRow, int visualColumn, string key, object? value)
    {
        await _handsontableJsReference.InvokeVoidAsync(
            "invokeMethod", "setCellMeta", visualRow, visualColumn, key, value);
    }

    public async Task SetCellMetaObject (int visualRow, int visualColumn, IDictionary<string,object?> prop)
    {
        await _handsontableJsReference.InvokeVoidAsync(
            "invokeMethod", "setCellMetaObject", visualRow, visualColumn, prop);
    }

    public async Task SetDataAtCell (int visualRow, int visualColumn, string? value, string? source = null)
    {
        await _handsontableJsReference.InvokeVoidAsync(
            "invokeMethod", "setDataAtCell", visualRow, visualColumn, value, source);
    }

    /**
    * Set data at cell.
    * @param {Array} changes An array of arrays in form of [row, col, value]. 
    *                Where row is visual row index {int}, col {int} is the visual column index, 
    *                and value {string} is a new value.
    */
    public async Task SetDataAtCell (IList<IList<object?>> changes, string? source = null)
    {
        await _handsontableJsReference.InvokeVoidAsync(
            "invokeMethod", "setDataAtCell", changes, null, null, source);
    }

    /**
    * Set data at cell.
    * @param {Array} changes An array of arrays in form of [row, col, value]. 
    *                Where row is visual row index {int}, prop {string} is the column property, 
    *                and value {string} is a new value.
    */
    public async Task SetDataAtRowProp (IList<IList<object?>> changes, string? source = null)
    {
        await _handsontableJsReference.InvokeVoidAsync(
            "invokeMethod", "setDataAtRowProp", changes, null, null, source);
    }

    /**
    * Suspends the execution process. It's helpful to wrap the table logic changes such
    * as index changes into one call after which the cache is updated. As a result, 
    * it improves the performance of wrapped operations.
    *
    * The method is intended to be used by advanced users. Suspending the execution 
    * process could cause visual glitches caused by not updated the internal table cache.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#suspendexecution
    */
    public async Task SuspendExecution ()
    {
        await _handsontableJsReference.InvokeVoidAsync( "invokeMethod", "suspendExecution" );
    }

    /**
    * Suspends the rendering process. It's helpful to wrap the table render cycles triggered 
    * by API calls or UI actions (or both) and call the "render" once in the end. As a result, 
    * it improves the performance of wrapped operations. When the table is in the suspend state, 
    * most operations will have no visual effect until the rendering state is resumed. 
    * Resuming the state automatically invokes the table rendering. To make sure that after 
    * executing all operations, the table will be rendered, it's highly recommended to use 
    * the Core#batchRender method or Core#batch, which additionally aggregates the logic 
    * execution that happens behind the table.
    *
    * The method is intended to be used by advanced users. Suspending the rendering process 
    * could cause visual glitches when wrongly implemented.
    *
    * Every SuspendRender() call needs to correspond with one ResumeRender() call. 
    * For example, if you call SuspendRender() 5 times, you need to call ResumeRender() 5 
    * times as well.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#suspendrender
    */
    public async Task SuspendRender ()
    {
        await _handsontableJsReference.InvokeVoidAsync( "invokeMethod", "suspendRender" );
    }

    public async Task<string> ToHtml ()
    {
        return await _handsontableJsReference.InvokeAsync<string>("invokeMethod", "toHTML");
    }

    public async Task<int> ToPhysicalColumn (int visualColumn)
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "toPhysicalColumn", visualColumn);
    }
    
    public async Task<int> ToPhysicalRow (int visualRow)
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "toPhysicalRow", visualRow);
    }
    
    public async Task<JQueryJsInterop> ToTableElement ()
    {
        var domTableJsObjectReference = await _handsontableJsReference.InvokeAsync<IJSInProcessObjectReference>("invokeMethodReturnsJQuery", "toTableElement");
        var domTableJQuery = new JQueryJsInterop(domTableJsObjectReference);
        return domTableJQuery;
    }

    public async Task<int> ToVisualColumn (int physicalColumn)
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "toVisualColumn", physicalColumn);
    }

    public async Task<int> ToVisualRow (int physicalRow)
    {
        return await _handsontableJsReference.InvokeAsync<int>("invokeMethod", "toVisualRow", physicalRow);
    }

    public async Task Undo ()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "undo");
    }

    /**
    * Stop listening to keyboard input on the document body. Calling this method 
    * makes the Handsontable inactive for any keyboard events.
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#unlisten
    */
    public async Task Unlisten()
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "unlisten");
    }

    /**
    * Update configuration option settings.
    * @param {IDictionary<string name, object vale>} settings A settings object 
        (see ConfigurationOptions).  Name should be camelCase.
    *   Only provide the settings that are changed.  
    *   
    */
    public async Task UpdateSettings (IDictionary<string, object> settings)
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "updateSettings", settings);
    }

    public async Task UpdateSettings (ConfigurationOptions options)
    {
        await _handsontableJsReference.InvokeVoidAsync("invokeMethod", "updateSettings", options);
    }


    public async Task ValidateCell (object value, IDictionary<string,object> meta, Func<ValidateArgs,Task> callback, string source)
    {
        var callbackProxy = ToVoidAsyncCallbackProxy(callback);
        await _handsontableJsReference.InvokeVoidAsync("invokeMethodWithCallback", "validateCell", value, meta, callbackProxy, source);
    }

    public async Task ValidateCells (Func<ValidateArgs,Task>? callback)
    {
        var callbackProxy = ToVoidAsyncCallbackProxy(callback);
        await _handsontableJsReference.InvokeVoidAsync("invokeMethodWithCallback", "validateCells", callbackProxy);
    }

    public async Task ValidateColumns (IList<int> visualColumns, Func<ValidateArgs,Task>? callback)
    {
        var callbackProxy = ToVoidAsyncCallbackProxy(callback);
        await _handsontableJsReference.InvokeVoidAsync("invokeMethodWithCallback", "validateColumns", visualColumns, callbackProxy);
    }

    public async Task ValidateRows (IList<int> visualRows, Func<ValidateArgs,Task>? callback)
    {
        var callbackProxy = ToVoidAsyncCallbackProxy(callback);
        await _handsontableJsReference.InvokeVoidAsync("invokeMethodWithCallback", "validateRows", visualRows, callbackProxy);
    }

    VoidAsyncCallbackProxy<CallbackArgsT>? ToVoidAsyncCallbackProxy<CallbackArgsT> (Func<CallbackArgsT,Task>? callback)
    {
        if (callback != null)
        {
            var callbackProxy = new VoidAsyncCallbackProxy<CallbackArgsT>(callback);
            return callbackProxy;
        }
        return null;
    }
    
    public async Task RegisterRenderer(string rendererName, Func<RendererArgs, Task> rendererCallback)
    {
        var module = await _handsontableModuleTask.Value;
        var dotNetHelper = DotNetObjectReference.Create(this);
        _rendererDict.Add(rendererName, rendererCallback);
        await module.InvokeVoidAsync("registerRenderer", rendererName, dotNetHelper);
    }

    /**
    * Removes the hook listener previously registered with AddHook().
    * See https://handsontable.com/docs/javascript-data-grid/api/core/#removehook
    */
    public async Task RemoveHook<HookArgsT>(string hookName, Func<HookArgsT, object> hook)
        where HookArgsT : BaseHookArgs
    {
        var hookKey = ICallbackProxy.CreateKey(hookName, hook);
        var hookProxy = _hookProxyDict[hookKey]; 
        await _handsontableJsReference.InvokeVoidAsync("removeHook", hookProxy);
        _hookProxyDict.Remove(hookKey);
    }

    public async ValueTask DisposeAsync()
    {
        if (_handsontableModuleTask.IsValueCreated)
        {
            var module = await _handsontableModuleTask.Value;
            await module.DisposeAsync();
        }
    }

 
    public async Task AddHookAfterChange(Func<AfterChangeArgs, Task> hook)
    {
        await AddHook("afterChange", hook);
    }

    public async Task AddHookAfterCreateCol(Func<AfterCreateColArgs, Task> hook)
    {
        await AddHook("afterCreateCol", hook);
    }

    public async Task AddHookAfterCreateRow(Func<AfterCreateRowArgs, Task> hook)
    {
        await AddHook("afterCreateRow", hook);
    }

    public async Task AddHookAfterSelection(Func<AfterSelectionArgs, Task> hook)
    {
        await AddHook("afterSelection", hook);
    }

    public async Task AddHookAfterRemoveCol(Func<AfterRemoveColArgs, Task> hook)
    {
        await AddHook("afterRemoveCol", hook);
    }
    public async Task AddHookAfterRemoveRow(Func<AfterRemoveRowArgs, Task> hook)
    {
        await AddHook("afterRemoveRow", hook);
    }

    public async Task AddHookAfterSelectionEnd(Func<AfterSelectionEndArgs, Task> hook)
    {
        await AddHook("afterSelectionEnd", hook);
    }

    public async Task AddHookBeforeCreateCol(Func<BeforeCreateColArgs, bool> hook)
    {
        await AddSyncHook("beforeCreateCol", hook);
    }

    public async Task AddHookBeforeCreateRow(Func<BeforeCreateRowArgs, bool> hook)
    {
        await AddSyncHook("beforeCreateRow", hook);
    }

    public async Task AddHookBeforeRemoveCol(Func<BeforeRemoveColArgs, bool> hook)
    {
        await AddSyncHook("beforeRemoveCol", hook);
    }

    public async Task AddHookBeforeRemoveRow(Func<BeforeRemoveRowArgs, bool> hook)
    {
        await AddSyncHook("beforeRemoveRow", hook);
    }

    public async Task AddHook<HookArgsT>(string hookName, Func<HookArgsT, Task> hook)
        where HookArgsT : ICallbackArgs
    {
        var hookProxy = new VoidAsyncCallbackProxy<HookArgsT>(hook, hookName);
        _hookProxyDict[hookProxy.GetKey()] = hookProxy;
        await _handsontableJsReference.InvokeVoidAsync("addHook", hookProxy);
    }
    
    public async Task AddSyncHook<HookArgsT,HookResultT>(string hookName, Func<HookArgsT, HookResultT> hook)
        where HookArgsT : ICallbackArgs
    {
        var hookProxy = new SyncCallbackProxy<HookArgsT,HookResultT>(hook, hookName);
        _hookProxyDict[hookProxy.GetKey()] = hookProxy;
        await _handsontableJsReference.InvokeVoidAsync("addHook", hookProxy);
    }
    

    [JSInvokable]
    public async Task OnRendererCallback(
        string rendererName, 
        IJSObjectReference hotInstance, 
        IJSObjectReference td, 
        int row, int col, 
        object prop, object value,
        IDictionary<string,object> cellProperties )
    {
        var args = new RendererArgs{
            HotInstance = hotInstance,
            Td = new JQueryJsInterop(td),
            Row = row,
            Column = col,
            Prop = prop.ToString()!,
            Value = value,
            CellProperties = cellProperties!
        };
        var renderer = _rendererDict[rendererName];
        await renderer(args);
    }


    /// <summary>
    /// @ToDo Move to top level namespace?
    /// Options:
    ///     1) Pass this RendererProxy to the JavaScript as DotNetReference
    ///         Dot net still needs to know signature.
    ///     2) Avoid dotNet classes in JavaScript space:
    ///         - Register renderer in table here.
    ///         - Lookup renderer before executing
    ///     
    /// </summary>
    public class RendererCallbackProxy
    {
        private Func<RendererArgs, Task> _callback;

        public RendererCallbackProxy (Func<RendererArgs, Task> callback)
        {
            _callback = callback;
        }

        [JSInvokable]
        public async Task OnRendererCallback(
            IJSObjectReference hotInstance, 
            IJSObjectReference td, 
            int row, int col, 
            object prop, object value,
            IDictionary<string,object> cellProperties )
        {
            var args = new RendererArgs{
                HotInstance = hotInstance,
                Td = new JQueryJsInterop(td),
                Row = row,
                Column = col,
                Prop = prop.ToString()!,
                Value = value,
                CellProperties = cellProperties!
            };
            await _callback(args);
        }
    }
}
