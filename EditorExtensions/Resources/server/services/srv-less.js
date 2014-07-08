//#region Imports
var less = require("less"),
    fs = require("fs"),
    path = require("path");
//#endregion

//#region Handler
var handleLess = function (writer, params) {
    if (!fs.existsSync(params.sourceFileName)) {
        writer.write(JSON.stringify({ Success: false, Remarks: "Input file not found!" }));
        writer.end();
        return;
    }

    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({ Success: false, Remarks: err }));
            writer.end();
            return;
        }

        try {
            new (less.Parser)({ filename: params.sourceFileName }).parse(data, function (e, tree) {
                if (e) {
                    writer.write(JSON.stringify({ Success: false, Remarks: e }));
                    writer.end();
                    return;
                }

                var map;
                var mapFileName = params.targetFileName + ".map";
                var mapDir = path.dirname(mapFileName);
                var css = tree.toCSS({
                    relativeUrl: params.relativeUrl,
                    paths: [path.dirname(params.sourceFileName)],
                    sourceMap: mapFileName,
                    sourceMapURL: params.sourceMapURL != undefined ? path.basename(mapFileName) : null,
                    sourceMapBasepath: mapDir,
                    sourceMapOutputFilename: mapFileName,
                    strictMath: params.strictMath,
                    writeSourceMap: function (output) {
                        output = JSON.parse(output);
                        output.file = path.basename(params.targetFileName);
                        output.sources = output.sources.map(function (source) {
                            var sourceDir = path.dirname(source);

                            if (sourceDir !== '.' && mapDir !== sourceDir)
                                return path.relative(mapDir, source).replace(/\\/g, '/');

                            return source;
                        });
                        map = output;
                    }
                });

                if (params.autoprefixer != undefined) {
                    var autoprefixedOutput = require("./srv-autoprefixer").processAutoprefixer(css, map, params.autoprefixerBrowsers, params.sourceFileName, params.targetFileName);
                    css = autoprefixedOutput.css;
                    // Curate the sources returned by autoprefix; remove ../ from the start of each source
                    var newMaps = autoprefixedOutput.map;
                    newMaps.sources = map.sources;
                    map = newMaps;
                }

                writer.write(JSON.stringify({
                    Success: true,
                    Remarks: "Successful!",
                    Output: {
                        outputContent: css,
                        mapContent: map
                    }
                }));
                writer.end();
            });
        } catch (e) {
            writer.write(JSON.stringify({ Success: false, Remarks: e.stack }));
            writer.end();
        }
    });
};
//#endregion

//#region Exports
module.exports = handleLess;
//#endregion
