//#region Imports
var jshint = require("jshint/src/cli");
//#endregion

//#region Reporter
var reporter = function (results, writer) {
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
        Remarks: "Successful!",
        Output: {
            Content: messageItems
        }
    }));
    writer.end();
}
//#endregion

//#region Handler
var handleJSHint = function (writer, params) {
    // Override jshint's export.exit(1). The only reason this would exit
    // in case of Web Essentials is the invalid JSON.
    jshint.exit = function () {
        writer.write(JSON.stringify({ Success: false, Remarks: "JsHint: Invalid Config file" }));
        writer.end();
    };

    jshint.run({
        args: [params.sourceFileName],
        reporter: function (results) { reporter(results, writer); }
    });
};
//#endregion

//#region Exports
module.exports = handleJSHint;
//#endregion
