//#region Imports
var jshint = require("jshint");
//#endregion

//#region Handler
var handleJSHint = function (writer, params) {
    writer.write(JSON.stringify({ Success: false, Remarks: "Service Not Implemented." }));
    writer.end();
};
//#endregion

//#region Exports
exports.handleJSHint = handleJSHint;
//#endregion
