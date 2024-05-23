// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

import '/_content/Handsontable.Blazor/lib/handsontable/handsontable.js';


var handsontableJsDict = {}

/**
 * {string} elemId - Handsontable HTML element identifier, must be unique to module instance.
 *                     Handsontable will be add to this element.
 * {jQuery selector} boundingElem - Bounding jQuery element selector determines the maximum
 *                   visible area available to the Handsontable.
 * {HandsontableOptions} hotOptions.
 * {DotNetObjectReference<HandsontableWidget>} dotNetHelper - For DotNet callbacks.
 */
export function newHandsontable(elemId, data) {
    let handsontableJs = new HandsontableJs(elemId, data);
    handsontableJsDict[elemId] = handsontableJs;
    return elemId;
}

export function invokeMethod(elemId, method, ...args) {
  return handsontableJsDict[elemId]._hot[method](...args);
}

export function disposeHansontable(elemId) {
  delete handsontableJsDict[elemId];
}



class HandsontableJs {
  _hot;                       // Handsontable

  constructor(elemId, data) {
    var containerElem = document.getElementById(elemId)
    this._hot = new Handsontable( 
      containerElem, 
      {
        data: data,
        rowHeaders: true,
        colHeaders: true, 
        height: 'auto',
        autoWrapRow: true,
        autoWrapCol: true
    } );
  }
}
