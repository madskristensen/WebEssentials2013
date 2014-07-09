//#region Imports
var sweetjs = require("sweet.js"),
    path = require("path"),
    fs = require("fs");
//#endregion

//#region Handler
var handleSweetJS = function (writer, params) {
    var options = {
        filename: params.sourceFileName,
        sourceMap: true
    };

    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({ Success: false, Remarks: "Error reading input file." }));
            writer.end();
            return;
        }

        try {
            compiled = sweetjs.compile(data, options);

            var targetDir = path.dirname(params.targetFileName);
            var map = JSON.parse(compiled.sourceMap);
            map.file = path.basename(params.targetFileName);
            map.sources = map.sources.map(function (source) {
                return path.relative(targetDir, source).replace(/\\/g, '/');
            });

            var js = compiled.code;
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
module.exports = handleSweetJS;
//#endregion
