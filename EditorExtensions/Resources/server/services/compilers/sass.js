//#region Imports
//var sass = require("node-sass");
//#endregion

//#region Handler
var handleSass = function (writer, params) {
    writer.write(JSON.stringify({ Success: false, Remarks: "Service Not Implemented." }));
    writer.end();
};
//#endregion

//#region Exports
exports.handleSass = handleSass;
//#endregion
