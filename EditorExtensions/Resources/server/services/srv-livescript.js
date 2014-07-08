//#region Imports
var autoprefixer = require("LiveScript");
//#endregion

//#region Handler
var handleLiveScript = function (writer, params) {
    writer.write(JSON.stringify({ Success: false, Remarks: "Service Not Implemented." }));
    writer.end();
};
//#endregion

//#region Exports
module.exports = handleLiveScript;
//#endregion
