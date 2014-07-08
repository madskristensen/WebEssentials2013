//#region Imports
var tslint = require("tslint");
//#endregion

//#region Handler
var handleTSLint = function (writer, params) {
    writer.write(JSON.stringify({ Success: false, Remarks: "Service Not Implemented." }));
    writer.end();
};
//#endregion

//#region Exports
module.exports = handleTSLint;
//#endregion
