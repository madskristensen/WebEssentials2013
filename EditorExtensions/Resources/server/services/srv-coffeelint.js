//#region Imports
var coffeelint = require("coffeelint"),
    configFinder = require("coffeelint/lib/configfinder"),
    fs = require("fs");
//#endregion

//#region Reporter
var reporter = function (results, writer, path) {
    var errorItems = results.map(function (error) {
        return {
            Line: error.lineNumber,
            Message: "CoffeeLint: " + error.message,
            FileName: path
        };
    });

    writer.write(JSON.stringify({
        Success: true,
        SourceFileName: path,
        Remarks: "Successful!",
        Errors: errorItems
    }));
    writer.end();
};
//#endregion

//#region Handler
var handleCoffeeLint = function (writer, params) {
    var tempError = console.error;
    console.error = function () { /* waste log messages toDevNull */ };

    var config = configFinder.getConfig(params.sourceFileName);

    console.error = tempError;

    if (config == null) {
        writer.write(JSON.stringify({
            Success: false,
            SourceFileName: params.sourceFileName,
            Remarks: "CoffeeLint: Invalid Config file.",
            Errors: [{
                Message: "CoffeeLint: Invalid config file.",
                FileName: params.sourceFileName
            }]
        }));
        writer.end();
        return;
    }

    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({
                Success: false,
                SourceFileName: params.sourceFileName,
                Remarks: "CoffeeLint: Error reading input file.",
                Details: err,
                Errors: [{
                    Message: "CoffeeLint: " + err,
                    FileName: params.sourceFileName
                }]
            }));
            writer.end();
            return;
        }

        var results = coffeelint.lint(data, config);
        reporter(results, writer, params.sourceFileName);
    });
};
//#endregion

//#region Exports
module.exports = handleCoffeeLint;
//#endregion
