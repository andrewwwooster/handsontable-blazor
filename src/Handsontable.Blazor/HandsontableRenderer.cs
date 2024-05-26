using Microsoft.JSInterop;

namespace Handsontable.Blazor;

public class HandsontableRenderer {
    public delegate Task RendererCallback(RendererArgs args);

    public class RendererArgs {
        public required IJSObjectReference HotInstance { get; set; }
        public required IJSObjectReference Td { get; set; }
        public required int Row { get; set; }
        public required int Column { get; set; }
        public required string Prop { get; set; }
        public required object Value { get; set; }
        public required IDictionary<string,object> CellProperties { get; set; }
    }
}