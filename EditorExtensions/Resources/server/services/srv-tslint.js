//#region Imports
var tslint = require("tslint"),
    path = require("path"),
    fs = require("fs");
//#endregion

//#region Reporter
var reporter = function () { };
reporter.prototype.format = function (results) {
    var messageItems = [];

    if (results)
        messageItems = results.map(function (result) {
            var lineAndCharacter = result.getStartPosition().getLineAndCharacter();

            return {
                Line: lineAndCharacter.line() + 1,
                Column: lineAndCharacter.character() + 1,
                Message: "TsLint: " + result.getFailure(),
                FileName: result.getFileName()
            };
        }).sort(function (a, b) {
            return a.Line < b.Line ? -1 : (a.Line > b.Line ? 1 : 0);
        });

    return messageItems;
};
//#endregion

//#region Handler
var handleTSLint = function (writer, params) {
    var config;

    try {
        config = tslint.findConfiguration(null, path.dirname(params.sourceFileName));
    } catch (e) { }

    if (!config) {
        writer.write(JSON.stringify({
            Success: false,
            SourceFileName: params.sourceFileName,
            Remarks: "TSLint: Invalid Config file",
            Errors: [{
                Message: "TSLint: Invalid config file.",
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
                Remarks: "TSLint: Error reading input file.",
                Details: err,
                Errors: [{
                    Message: "TSLint: Error reading input file.",
                    FileName: params.sourceFileName
                }]
            }));
            writer.end();
            return;
        }

        var results = new tslint(params.sourceFileName, data, {
            configuration: config,
            formatter: reporter,
            formattersDirectory: null,
            rulesDirectory: null
        }).lint();

        writer.write(JSON.stringify({
            Success: true,
            SourceFileName: params.sourceFileName,
            Remarks: "Successful!",
            Errors: results.output
        }));
        writer.end();
    });
};
//#endregion

//#region Exports
module.exports = handleTSLint;
//#endregion
