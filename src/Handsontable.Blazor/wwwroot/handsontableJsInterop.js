// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

import '/_content/Handsontable.Blazor/lib/handsontable/handsontable.full.js';


var handsontableJsDict = {}

/**
 * {string} elemId - Handsontable HTML element identifier, must be unique to module instance.
 *                     Handsontable will be add to this element.
 * {jQuery selector} boundingElem - Bounding jQuery element selector determines the maximum
 *                   visible area available to the Handsontable.
 * {HandsontableOptions} hotOptions.
 * {DotNetObjectReference<HandsontableWidget>} dotNetHelper - For DotNet callbacks.
 */
export function newHandsontable(elemId, data, dotNetHelper) {
    let handsontableJs = new HandsontableJs(elemId, data, dotNetHelper);
    handsontableJsDict[elemId] = handsontableJs;
    return elemId;
}

export function invokeMethod(elemId, method, ...args) {
  return handsontableJsDict[elemId]._hot[method](...args);
}

export function enableHook(elemId, hookName) {
  handsontableJsDict[elemId].enableHook(hookName);
}

export function disposeHansontable(elemId) {
  delete handsontableJsDict[elemId];
}



class HandsontableJs {
  _hot;                        // Handsontable
  _dotNetHelper;               // DotNetObjectReference<HandsontableJsInterop>

  constructor(elemId, configurationOptions, dotNetHelper) {
    let containerElem = document.getElementById(elemId)
    this._dotNetHelper = dotNetHelper;
    this._hot = new Handsontable( containerElem, configurationOptions )
  }

  enableHook(hookName) {
    this._hot.addHook(hookName, async (...callbackArgs) => this.hookCallback(hookName, ...callbackArgs));
  }

  hookCallback(hookName, ...callbackArgs) {
    let callbackName = "OnAfterChangeCallback";
    this._dotNetHelper.invokeMethodAsync(callbackName, ...callbackArgs);
  }
}
