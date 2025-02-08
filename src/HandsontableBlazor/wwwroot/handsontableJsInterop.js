// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

import "/_content/Excielo.Handsontable.Blazor/lib/handsontable/handsontable.full.js";
import "/_content/Excielo.Handsontable.Blazor/lib/jquery/jquery.js";


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
    rendererName, async (...callbackArgs) => await rendererCallback(...callbackArgs));
}


class HandsontableJs {
  _hot;                        // Handsontable
  _dotNetHelper;               // DotNetObjectReference<HandsontableJsInterop>
  _hookCallbackDict = new Map();  // {string id, hookCallback}

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
    let result = this._hot[method](...args);
    return result;
  }

  /**
   * Invoke a Handsontable method that has a callback parameter.
   * @param {string} method the name of the method to invoke on the Handsontable instance.
   * @param  {...any} args 
   * @returns result of the Handsontable method invocation.
   */
  invokeMethodWithCallback(method, ...args) {
    args = this.#processCallabackArgs(args);
    let result = this._hot[method](...args);
    return result;
  }

  /**
   * Convert any CallbackProxy objects in the args array to JavaScript callbacks.
   * @param {*} args 
   * @returns the args array with any CallbackProxy objects replaced with JavaScript callbacks.
   */
  #processCallabackArgs(args) {
    let result = new Array();
    for (let i = 0; i < args.length; i++) {
      result[i] = args[i];
      if (args[i].typeName && args[i].typeName.includes("CallbackProxy")) {
        result[i] = this.#createJsCallback(args[i]);
      }
    }
    return result;
  }

  /**
   * Create a JavaScript callback function that wraps a call to the .NET callbackProxy.
   * It will be used to call the Callback method on the .NET object reference in the
   * callbackProxy.
   * @param {ICallbackProxy} callbackProxy 
   * @returns JavaScript callback function that wraps a call to the .NET callbackProxy.
   */
  #createJsCallback(callbackProxy) {
    let jsCallback = null;
    if (callbackProxy.isAsync) {
      jsCallback = async (...callbackArgs) => {
        return await callbackProxy.objectReference.invokeMethodAsync("Callback", callbackArgs);
      }
    }
    else {
      jsCallback = (...callbackArgs) => {
        return callbackProxy.objectReference.invokeMethod("Callback", callbackArgs);
      }
    }
    return jsCallback;
  }

  invokeMethodReturnsJQuery(method, ...args) {
    let result = this._hot[method](...args);
    let jQueryResult = jQuery(result);
    return jQueryResult;
  }
  
  /**
   * Add a hook to Handsontable.
   * @param {ICallbackProxy} callbackProxy
   */
  addHook(callbackProxy) {
    let callback = this.#createJsCallback(callbackProxy);
    this._hot.addHook(callbackProxy.callbackName, callback);
    this._hookCallbackDict.set(callbackProxy.id, callback);
  }

  /**
   * Remove a hook from Handsontable.
   * Uses the callbackProxy.id to find the callback in to remove.
   * @param {ICallbackProxy} callbackProxy
   */
  removeHook(callbackProxy) {
    var callback = this._hookCallbackDict.get(callbackProxy.id);

    // Remove hook from Handsontable.
    this._hot.removeHook(callbackProxy.callbackName, callback);

    delete this._hookCallbackDict[callbackProxy.id];
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
