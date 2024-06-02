using HandsontableBlazor.Interop;
using Microsoft.JSInterop;

namespace HandsontableBlazor;

public class Renderer {

    public class RendererArgs {
        public required IJSObjectReference HotInstance { get; set; }
        public required JQueryJsInterop Td { get; set; }
        public required int Row { get; set; }
        public required int Column { get; set; }
        public required string Prop { get; set; }
        public required object Value { get; set; }
        public required IDictionary<string,object> CellProperties { get; set; }
    }
}