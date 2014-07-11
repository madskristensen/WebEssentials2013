//#region Imports
var icedcoffeescript = require("iced-coffee-script"),
    path = require("path"),
    fs = require("fs"),
    xRegex = require("xregexp").XRegExp;
//#endregion

//#region Handler
var handleIcedCoffeeScript = function (writer, params) {
    var options = {
        filename: params.sourceFileName,
        bare: params.bare !== null,
        runtime: "inline",
        sourceMap: true,
        sourceRoot: "",
        sourceFiles: [path.relative(path.dirname(params.targetFileName), params.sourceFileName).replace(/\\/g, '/')],
    };

    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({ Success: false, Remarks: "IcedCoffeeScript: Error reading input file.", Details: err }));
            writer.end();
            return;
        }

        try {
            compiled = icedcoffeescript.compile(data, options);

            var map = JSON.parse(compiled.v3SourceMap);
            map.file = path.basename(params.targetFileName);
            delete map.sourceRoot;

            var js = compiled.js;
            if (params.sourceMapURL != undefined)
                js = "" + js + "\n//# sourceMappingURL=" + path.basename(params.targetFileName) + ".map\n";

            writer.write(JSON.stringify({
                Success: true,
                Remarks: "Successful!",
                Output: {
                    Content: js,
                    Map: map
                }
            }));
            writer.end();
        } catch (error) {
            var regex = xRegex.exec(error, xRegex(".*:.\\d*:.\\d*: error: (?<fullMessage>(?<message>.*)(\\n*.*)*)", 'gi'));
            writer.write(JSON.stringify({
                Success: false,
                Remarks: "IcedCoffeeScript: An error has occured while processing your request.",
                Details: error.message,
                Errors: {
                    Line: error.location.first_line,
                    Column: error.location.first_column,
                    Message: regex.message,
                    FileName: error.filename,
                    FullMessage: regex.fullMessage
                }
        }));
            writer.end();
        }
    });
};
//#endregion

//#region Exports
module.exports = handleIcedCoffeeScript;
//#endregion
