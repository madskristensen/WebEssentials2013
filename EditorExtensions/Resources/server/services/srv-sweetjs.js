//#region Imports
var sweetjs = require("sweet.js");
//#endregion

//#region Handler
var handleSweetJS = function (writer, params) {
    writer.write(JSON.stringify({ Success: false, Remarks: "Service Not Implemented." }));
    writer.end();
};
//#endregion

//#region Exports
module.exports = handleSweetJS;
//#endregion
