# handsontable-blazor

handsontable-blazor a Blazor Library that wraps the [Handsontable](https://handsontable.com) JavaScript grid library as a Blazor component.  Handsontable is a very rich Excel-like spreadhseet UI.

## How to get started
1. Install [dotnet sdk](https://dotnet.microsoft.com/en-us/download)
2. Clone this project
3. ```dotnet build```
4. ```cd samples/BlazorWasm```
5. ```dotnet run```

## Usage

**Example - razor**
```
@page "/"
@using HandsontableBlazor

<Handsontable 
        Id="hot" 
        ConfigurationOptions="@_configurationOptions" 
        @ref="Handsontable"/>

@{
    private HandsontableBlazor.ConfigurationOptions _configurationOptions = {
            DataArrayOfArrays =  [
                [ 1, "12!", "13" ],
                [ 21, "22!", "23" ],
                [ 31, "32!", "33" ]
            ],
            ColHeaders = new string[] { "A", "B", "C" },
            RendererCallback = OnRenderCallback,
            RowHeaders = true
        };

    private async Task OnRenderCallback(Renderer.RendererArgs args)
    {
        await args.Td.Attr("style", "background-color: yellow; color: #000000;");
        var value = args.Value?.ToString();
        await args.Td.Text(value ?? "");
    }
}
```

