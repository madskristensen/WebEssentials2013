//#region Imports
var less = require("less"),
    fs = require("fs"),
    path = require("path"),
    commons = require("./_commons.js");
//#endregion

//#region Handler
var handleLess = function (writer, params) {
    var parser = new less.Parser();

    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({ Error: err }));
            writer.end;
            return;
        }

        try {
            parser.parse(data, function (e, tree) {
                var css = tree.toCSS({
                    relativeUrl: true,
                    paths: [path.dirname(params.sourceFileName)],
                    filename: params.sourceFileName,
                    sourceMap: true,
                    sourceMapBasepath: path.dirname(params.mapFileName),
                    sourceMapOutputFilename: params.mapFileName,
                    strictMath: params.strictMath,
                    writeSourceMap: function (output) {
                        commons.ensureDirectory(params.mapFileName);
                        fs.writeFileSync(params.mapFileName, output, 'utf8');
                    }
                });

                commons.ensureDirectory(params.targetFileName);
                fs.writeFileSync(params.targetFileName, css, 'utf8');

                writer.write(JSON.stringify({ Success: true, Remarks: "Successful!", Output: css }));
                writer.end();
            });
        } catch (e) {
            writer.write(JSON.stringify({ Success: false, Remarks: e.stack }));
            writer.end();
        }
    });

    writer.write("output1");
};
//#endregion

//#region Exports
exports.handleLess = handleLess;
//#endregion
