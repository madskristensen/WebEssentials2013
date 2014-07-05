//#region Imports
var coffeescript = require("coffee-script");
//#endregion

//#region Handler
var handleCoffeeScript = function (writer, params) {
    writer.write(JSON.stringify({ Success: false, Remarks: "Service Not Implemented." }));
    writer.end();
};
//#endregion

//#region Exports
exports.handleCoffeeScript = handleCoffeeScript;
//#endregion
