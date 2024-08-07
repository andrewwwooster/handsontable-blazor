using Microsoft.JSInterop;

namespace HandsontableBlazor.Interop;

/// <summary>
/// Creates a JQuery based interface on an HTML element.
/// See https://api.jquery.com/category/attributes/
/// See https://api.jquery.com/category/css/
/// </summary>
public class JQueryJsInterop
{
    IJSObjectReference _jqueryObjectReference;

    /// <summary>
    /// Creates a new instance of the JQueryJsInterop class.
    /// jqueryObjectReference is a reference to a jQuery object.
    /// </summary>
    public JQueryJsInterop(IJSObjectReference jqueryObjectReference)
    {
        _jqueryObjectReference = jqueryObjectReference;
    }
    
    public async Task<string> Attr(string property)
    {
        return await _jqueryObjectReference.InvokeAsync<string>("attr", property);
    }

    public async Task Attr(string property, string value)
    {
        await _jqueryObjectReference.InvokeVoidAsync("attr", property, value);
    }


    public async Task<string> Css(string property)
    {
        return await _jqueryObjectReference.InvokeAsync<string>("css", property);
    }

    public async Task Css(string property, string value)
    {
        await _jqueryObjectReference.InvokeVoidAsync("css", property, value);
    }

    public async Task Height()
    {
        await _jqueryObjectReference.InvokeAsync<int>("height");
    }

    public async Task<string> Html()
    {
        return await _jqueryObjectReference.InvokeAsync<string>("html");
    }

    public async Task Html(string value)
    {
        await _jqueryObjectReference.InvokeVoidAsync("html", value);
    }

    public async Task<string> Text()
    {
        return await _jqueryObjectReference.InvokeAsync<string>("text");
    }

    public async Task Text(string value)
    {
        await _jqueryObjectReference.InvokeVoidAsync("text", value);
    }

    public async Task Width()
    {
        await _jqueryObjectReference.InvokeAsync<int>("width");
    }
}