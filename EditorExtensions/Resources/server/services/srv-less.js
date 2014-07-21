//#region Imports
var less = require("less"),
    fs = require("fs"),
    path = require("path");
//#endregion

//#region Handler
var handleLess = function (writer, params) {
    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({
                Success: false,
                SourceFileName: params.sourceFileName,
                TargetFileName: params.targetFileName,
                MapFileName: params.mapFileName,
                Remarks: "LESS: Error reading input file.",
                Details: err,
                Errors: [{
                    Message: "LESS" + err,
                    FileName: params.sourceFileName
                }]
            }));
            writer.end();
            return;
        }

        try {
            new (less.Parser)({ filename: params.sourceFileName }).parse(data, function (e, tree) {
                if (e) {
                    writer.write(JSON.stringify({
                        Success: false,
                        SourceFileName: params.sourceFileName,
                        TargetFileName: params.targetFileName,
                        MapFileName: params.mapFileName,
                        Remarks: "LESS: Error parsing input file.",
                        Details: e.message,
                        Errors: [{
                            Line: e.line,
                            Column: e.column,
                            Message: "LESS: " + e.message,
                            FileName: e.filename
                        }]
                    }));
                    writer.end();
                    return;
                }

                var map;
                var mapFileName = params.targetFileName + ".map";
                var mapDir = path.dirname(mapFileName);
                var css = tree.toCSS({
                    relativeUrl: true,
                    paths: [path.dirname(params.sourceFileName)],
                    sourceMap: mapFileName,
                    sourceMapURL: params.sourceMapURL != undefined ? path.basename(mapFileName) : null,
                    sourceMapBasepath: mapDir,
                    sourceMapOutputFilename: mapFileName,
                    strictMath: params.strictMath != null,
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
                    var newMaps = JSON.parse(autoprefixedOutput.map);
                    newMaps.sources = newMaps.sources.map(function (source) {
                        return source.substr(3, source.length);
                    });
                    map = newMaps;
                }

                writer.write(JSON.stringify({
                    Success: true,
                    SourceFileName: params.sourceFileName,
                    TargetFileName: params.targetFileName,
                    MapFileName: params.mapFileName,
                    Remarks: "Successful!",
                    Content: css,
                    Map: JSON.stringify(map)
                }));
                writer.end();
            });
        } catch (e) {
            writer.write(JSON.stringify({
                Success: false,
                SourceFileName: params.sourceFileName,
                TargetFileName: params.targetFileName,
                MapFileName: params.mapFileName,
                Remarks: "LESS: " + e.message,
                Details: e.message,
                Errors: [{
                    Line: e.line,
                    Column: e.column,
                    Message: "LESS: " + e.message,
                    FileName: params.sourceFileName
                }]
            }));
            writer.end();
        }
    });
};
//#endregion

//#region Exports
module.exports = handleLess;
//#endregion
