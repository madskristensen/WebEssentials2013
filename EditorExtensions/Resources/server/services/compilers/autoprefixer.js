//#region Imports
var autoprefixer = require("autoprefixer");
//#endregion

//#region Handler
var handleAutoPrefixer = function (writer, params) {
    writer.write(JSON.stringify({ Success: false, Remarks: "Service Not Implemented." }));
    writer.end();
};
//#endregion

//#region Exports
exports.handleAutoPrefixer = handleAutoPrefixer;
//#endregion
