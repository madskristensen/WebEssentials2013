//#region Imports
var jshint = require("jshint/src/cli"),
    path = require("path");
//#endregion

//#region Reporter
var reporter = function (results, writer, params) {
    var messageItems = results.map(function (result) {
        var error = result.error;

        if (error.reason === "Missing radix parameter.") {
            error.reason = "When using the parseInt function, remember to specify the radix parameter. Example: parseInt('3', 10)";
        }

        return {
            Line: parseInt(error.line, 10),
            Column: parseInt(error.character, 10),
            Message: "JsHint (" + error.code + "): " + error.reason,
            FileName: result.file
        };
    });

    writer.write(JSON.stringify({
        Success: true,
        SourceFileName: params.sourceFileName,
        Remarks: "Successful!",
        Errors: messageItems
    }));
    writer.end();
};
//#endregion

//#region Handler
var handleJSHint = function (writer, params) {
    // Override jshint's export.exit(1). The only reason this would exit
    // in case of Web Essentials is the invalid JSON.
    jshint.exit = function () {
        writer.write(JSON.stringify({
            Success: false,
            SourceFileName: params.sourceFileName,
            Remarks: "JsHint: Invalid Config file",
            Errors: [{
                Message: "JSHint: Invalid config file.",
                FileName: params.sourceFileName
            }]
        }));
        writer.end();
    };

    jshint.run({
        args: [params.sourceFileName],
        cwd: path.dirname(params.sourceFileName),
        reporter: function (results) { reporter(results, writer, params); }
    });
};
//#endregion

//#region Exports
module.exports = handleJSHint;
//#endregion
