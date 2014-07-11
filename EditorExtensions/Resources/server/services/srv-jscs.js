//#region Imports
var jscs = require("jscs/lib/checker"),
    configFile = require("jscs/lib/cli-config"),
    path = require("path"),
    vow = require('vow');
//#endregion

//#region Reporter
var reporter = function (results, writer) {
    var errorItems = [];
    var errors = results[0];
    var fileName = errors.getFilename();

    if (!errors.isEmpty()) {
        errorItems = errors.getErrorList().map(function (error) {
            return {
                Line: error.line,
                Column: error.column + 1,
                Message: "JSCS: " + error.message,
                FileName: fileName
            };
        });
    }

    writer.write(JSON.stringify({
        Success: true,
        Remarks: "Successful!",
        Output: {
            Content: errorItems
        }
    }));
    writer.end();
};
//#endregion

//#region Handler
var handleJSCS = function (writer, params) {
    var config;

    try {
        config = configFile.load(null, path.dirname(params.sourceFileName));
    } catch (e) {
        writer.write(JSON.stringify({ Success: false, Remarks: "JSCS: Invalid Config file", Details: e.message || e.stack }));
        writer.end();
        return;
    }

    var checker = new jscs();

    checker.registerDefaultRules();
    checker.configure(config);

    // Unlike their cli, we don't need Vows'defer() and call all().
    // All we need is proceed on done().
    vow.done(checker.checkPath(params.sourceFileName),
             function (results) { reporter(results, writer); },
             function () { // They should probably need add this to their CLI code too: https://github.com/mdevils/node-jscs/issues/490
                 writer.write(JSON.stringify({ Success: false, Remarks: "JSCS: Cannot parse invalid JavaScript." }));
                 writer.end();
             });
};
//#endregion

//#region Exports
module.exports = handleJSCS;
//#endregion
