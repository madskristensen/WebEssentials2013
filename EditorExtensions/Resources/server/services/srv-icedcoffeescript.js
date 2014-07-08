//#region Imports
var icedcoffeescript = require("iced-coffee-script");
//#endregion

//#region Handler
var handleIcedCoffeeScript = function (writer, params) {
    writer.write(JSON.stringify({ Success: false, Remarks: "Service Not Implemented." }));
    writer.end();
};
//#endregion

//#region Exports
module.exports = handleIcedCoffeeScript;
//#endregion
