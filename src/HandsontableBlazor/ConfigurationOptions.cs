namespace HandsontableBlazor;

/// <summary>
/// Corresponds to JSON settings passed to a client as Handsontable options
/// See https://handsontable.com/docs/javascript-data-grid/configuration-options/
/// </summary>
public class ConfigurationOptions
{
    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// The following are Handsontable options.
    /// </summary>
    //////////////////////////////////////////////////////////////////////

    public class MergeCell
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public int Rowspan { get; set; }
        public int Colspan { get; set; }
    }

    public class NumericFormat
    {
        public string? Pattern { get; set; }
    }

    public class NestedHeader
    {
        public string? Label { get; set; }
        public int Colspan { get; set; }
    }

    public class HtCellProperties : ICloneable
    {
        public string? Type { get; set; }
        public NumericFormat? NumericFormat { get; set; }
        public string? Format { get; set; }
        public string? DateFormat { get; set; }
        public bool AllowInvalid { get; set; } = false;
        public bool CorrectFormat { get; set; } = true;
        public bool ReadOnly { get; set; }
        public string? ClassName { get; set; }
        public IList<string>? Source { get; set; }
        public bool WordWrap { get; set; }
        public string? Renderer { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class HtRow
    {
        public string? Data { get; set; }
    }

    public class HtColumn
    {
        public string? Data { get; set; }
        public bool ReadOnly { get; set; }
        public string? Renderer { get; set; }
    }

    public class HtComment
    {
        public string? Value { get; set; }
        public bool ReadOnly { get; set; } = true;
    }

    public class HtCell : HtCellProperties
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public HtComment? Comment { get; set; }

        public HtCell(int row, int col)
        {
            this.Row = row;
            this.Col = col;
        }
    }
    public bool AutoRowSize { get; set; }
    public bool AutoWralCol { get; set; }
    public bool AutoWrapRow { get; set; }
    public bool Comments { get; set; } = true;

    public int? FixedColumnsLeft { get; set; }
    public object RowHeaders { get; set; } = false;  // Can be boolean or array of strings
    public IList<HtCell> Cell { get; set; } = new List<HtCell>();
    public IList<HtColumn>? Columns { get; set; }
    public bool ColumnSorting { get; set; } = false;
    public object ColHeaders { get; set; } = false;  // Can be boolean or array of strings
    public IList<double?> ColWidths { get; set; } = new List<double?>();
    public IList<Dictionary<string, object>> Data { get; set; } = new List<Dictionary<string, object>>();
    public IList<double?> RowHeights { get; set; } = new List<double?>();
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool CustomBorders { get; set; }
    public IList<string> DropdownMenu { get; set; } = new List<string> { "filter_by_value", "filter_action_bar" };
    public bool MultiColumnSorting { get; set; }
    public bool ManualRowMove { get; set; }
    public IList<List<NestedHeader>>? NestedHeaders { get; set; }
    public string? Renderer { get; set; }
    public bool SortIndicator { get; set; } = true;
    public bool Filters { get; set; } = true;

    public string? LicenseKey { get; set; } = "non-commercial-and-evaluation";

    public IList<MergeCell>? MergeCells { get; set; }
    public IList<string>? ColHeaderLabels { get; set; }
}

