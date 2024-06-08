// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

import '/_content/HandsontableBlazor/lib/handsontable/handsontable.full.js';
import "https://ajax.googleapis.com/ajax/libs/jquery/3.7.1/jquery.js";


/**
 * {string} elemId - Handsontable HTML element identifier, must be unique to module instance.
 *                     Handsontable will be add to this element.
 * {ConfigurationOptions} configurationOptions.
 * {DotNetObjectReference<HandsontableWidget>} dotNetHelper - For DotNet callbacks.
 * @returns {HandsontableJs} - HandsontableJs instance.
 */
export function newHandsontable(elemId, configurationOptions, dotNetHelper) {
  let handsontableJs = new HandsontableJs(elemId, configurationOptions, dotNetHelper);
  return handsontableJs;
}

export function registerRenderer(rendererName, dotNetHelper) {
  let renderer =  new CustomRenderer(rendererName, dotNetHelper);

  // Bind renderer to callback so that it is available as 'this' in the callback.
  let rendererCallback = renderer.rendererCallback.bind(renderer);
  
  Handsontable.renderers.registerRenderer(
    rendererName, async (...callbackArgs) => rendererCallback(...callbackArgs));
}


class HandsontableJs {
  _hot;                        // Handsontable
  _dotNetHelper;               // DotNetObjectReference<HandsontableJsInterop>

  constructor(elemId, configurationOptions, dotNetHelper) {
    let containerElem = document.getElementById(elemId)
    this._dotNetHelper = dotNetHelper;

    if (configurationOptions.rendererCallbackDotNetObjectReference != null)
    {
      let customRenderer = new CustomRenderer(null, configurationOptions.rendererCallbackDotNetObjectReference);
      let callback = customRenderer.rendererCallback.bind(customRenderer);
      configurationOptions.renderer = callback;
    }

    this._hot = new Handsontable( containerElem, configurationOptions )
  }

  invokeMethod(method, ...args) {
    return this._hot[method](...args);
  }
  
  /**
   * @param {string} hookName 
   * @param {DotNetObjectReference<*HookProxy>} hookProxy
   */
  addHook(hookName, hookProxy) {
    this._hot.addHook(
      hookName, async (...callbackArgs) => hookProxy.invokeMethodAsync("HookCallback", callbackArgs));
  }
}


class CustomRenderer {
  _rendererName;                // string
  _dotNetHelper;                // DotNetObjectReference<RendererCallbackProxy>

  constructor(rendererName, dotNetHelper) {
    this._rendererName = rendererName;
    this._dotNetHelper = dotNetHelper
  }

  async rendererCallback(hotInstance, td, row, column, prop, value, cellProperties) {
    // Optionally include `BaseRenderer` which is responsible for
    // adding/removing CSS classes to/from the table cells.
    Handsontable.renderers.BaseRenderer.apply(this, arguments);

    let hotInstanceRef = DotNet.createJSObjectReference(hotInstance)
    let tdRef = DotNet.createJSObjectReference(jQuery(td))
      
    await this._dotNetHelper.invokeMethodAsync(
      "OnRendererCallback", 
      hotInstanceRef,
      tdRef,
      row,
      column,
      prop,
      value,
      cellProperties
    );

    DotNet.disposeJSObjectReference(hotInstanceRef);
    DotNet.disposeJSObjectReference(tdRef);
  }
}
