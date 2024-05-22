// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

import '/_content/Handsontable.Blazor/lib/handsontable/handsontable.js';

export function showPrompt(message) {
  var elem = document.getElementById("hot")
  var hot = new Handsontable( elem, {
    data: [
      ['', 'Tesla', 'Volvo', 'Toyota', 'Ford'],
      ['2019', 10, 11, 12, 13],
      ['2020', 20, 11, 14, 13],
      ['2021', 30, 15, 12, 13]
    ],
    rowHeaders: true,
    colHeaders: true, 
    height: 'auto',
    autoWrapRow: true,
    autoWrapCol: true
  });
  return "";
}
