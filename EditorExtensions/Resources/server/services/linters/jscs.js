//#region Imports
var jscs = require("jscs");
//#endregion

//#region Handler
var handleJSCS = function (writer, params) {
    writer.write(JSON.stringify({ Success: false, Remarks: "Service Not Implemented." }));
    writer.end();
};
//#endregion

//#region Exports
exports.handleJSCS = handleJSCS;
//#endregion
