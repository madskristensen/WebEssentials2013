//#region Imports
var compilers = require("./services/compilers/index.js"),
    linters = require("./services/linters/index.js");
//#endregion

//#region Controller
var handleRequest = function (writer, params) {
    switch (params.service) {
        case 'autoprefixer':
            handleAutoPrefixer(writer, params);
            break;
        case 'less':
            handleLess(writer, params);
            break;
        case 'sass':
            handleSass(writer, params);
            break;
        case 'coffeescript':
            handleCoffeeScript(writer, params);
            break;
        case 'icedcoffeescript':
            handleIcedCoffeeScript(writer, params);
            break;
        case 'livescript':
            handleLiveScript(writer, params);
            break;
        case 'sweetjs':
            handleSweetJS(writer, params);
            break;
        case 'jscs':
            handleJSCS(writer, params);
            break;
        case 'jshint':
            handleJSHint(writer, params);
            break;
        case 'tslint':
            handleTSLint(writer, params);
            break;
        default:
            writer.write(JSON.stringify({ Success: false, Remarks: "Invalid Service Name" }));
            writer.end();
            break;
    }
};
//#endregion

//#region Exports
exports.handleRequest = handleRequest;
//#endregion
