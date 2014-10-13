//#region Imports
var hbs = require("handlebars"),
    fs = require("fs"),
    path = require("path");
//#endregion

//#region Handler
var handleHandlebars = function (writer, params) {
    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({
                Success: false,
                SourceFileName: params.sourceFileName,
                TargetFileName: params.targetFileName,
                MapFileName: params.mapFileName,
                Remarks: "HANDLEBARS: Error reading input file.",
                Details: err,
                Errors: [{
                    Message: "HANDLEBARS" + err,
                    FileName: params.sourceFileName
                }]
            }));
            writer.end();
            return;
        }

        try {
            var compiled = hbs.precompile(data);
            compiled = ("var " + params.compiledTemplateName + " = Handlebars.template(" + compiled + ");");


            writer.write(JSON.stringify({
                Success: true,
                SourceFileName: params.sourceFileName,
                TargetFileName: params.targetFileName,
                Remarks: "Successful!",
                Content: compiled
            }));
            writer.end();

        } catch (e) {
            writer.write(JSON.stringify({
                Success: false,
                SourceFileName: params.sourceFileName,
                TargetFileName: params.targetFileName,
                MapFileName: params.mapFileName,
                Remarks: "HANDLEBARS: " + e.message,
                Details: e.message,
                Errors: [{
                    Line: e.line,
                    Column: e.column,
                    Message: "HANDLEBARS: " + e.message,
                    FileName: params.sourceFileName
                }]
            }));
            writer.end();
        }
    });
};
//#endregion

//#region Exports
module.exports = handleHandlebars;
//#endregion
