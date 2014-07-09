//#region Imports
var coffeescript = require("coffee-script"),
    path = require("path"),
    fs = require("fs");
//#endregion

//#region Handler
var handleCoffeeScript = function (writer, params) {
    var options = {
        filename: params.sourceFileName,
        bare: params.bare !== null,
        sourceMap: true,
        sourceRoot: "",
        sourceFiles: [path.relative(path.dirname(params.targetFileName), params.sourceFileName).replace(/\\/g, '/')],
    };

    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({ Success: false, Remarks: err }));
            writer.end();
            return;
        }

        try {
            compiled = coffeescript.compile(data, options);

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
                    outputContent: js,
                    mapContent: map
                }
            }));
            writer.end();
        } catch (error) {
            writer.write(JSON.stringify({ Success: false, Remarks: error.stack || ("" + error) }));
            writer.end();
        }
    });
};
//#endregion

//#region Exports
module.exports = handleCoffeeScript;
//#endregion
