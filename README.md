# handsontable-blazor

handsontable-blazor a Blazor Library that wraps the [Handsontable](https://handsontable.com) JavaScript grid library as a Blazor component.  Handsontable is a very rich Excel-like spreadhseet UI.

This project is Blazor / JavaScript interoperability library that maps Handonstable [core API](https://handsontable.com/docs/javascript-data-grid/api/core/), [hooks](https://handsontable.com/docs/javascript-data-grid/api/hooks/), and [configuration options](https://handsontable.com/docs/javascript-data-grid/api/options/) to C#.

This solution is composed of the following projects:
* ```src/HandsontableBlazor``` Blazor components and interoperability library.  The project uses a JQuery interoperability for fine grained control of grid elements.
* ```samples/BlazorWasm``` A sample Blazor WASM project that demonstrates the HandsontableBlazor capabilities.
* ```tests/BlazorWasm.Playwright``` A [Playwright](https://playwright.dev/dotnet/) UI test project that tests the HandsontableBlazor component using the samples/BlazorWasm project.

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

