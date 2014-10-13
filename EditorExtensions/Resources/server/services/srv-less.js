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
            new (less.Parser)({ filename: params.sourceFileName, relativeUrls: true }).parse(data, function (parseErrors, tree) {
                if (parseErrors) {
                    writer.write(JSON.stringify({
                        Success: false,
                        SourceFileName: params.sourceFileName,
                        TargetFileName: params.targetFileName,
                        MapFileName: params.mapFileName,
                        Remarks: "LESS: Error parsing input file.",
                        Details: parseErrors.message,
                        Errors: [{
                            Line: parseErrors.line,
                            Column: parseErrors.column,
                            Message: "LESS: " + parseErrors.message,
                            FileName: parseErrors.filename
                        }]
                    }));
                    writer.end();
                    return;
                }

                var css, map;
                var mapFileName = params.targetFileName + ".map";
                var mapDir = path.dirname(mapFileName);

                try {
                    css = tree.toCSS({
                        paths: [path.dirname(params.sourceFileName)],
                        sourceMap: mapFileName,
                        sourceMapURL: params.sourceMapURL !== undefined ? path.basename(mapFileName) : null,
                        sourceMapBasepath: mapDir,
                        sourceMapOutputFilename: mapFileName,
                        strictMath: params.strictMath !== undefined,
                        writeSourceMap: function (output) {
                            output = JSON.parse(output);
                            output.file = path.basename(params.targetFileName);
                            // There might be a configuration in toCSS which let us remove
                            // the following fix to save a millisecond or such per compile.
                            output.sources = output.sources.map(function (source) {
                                var sourceDir = path.dirname(source);

                                if (sourceDir !== '.' && mapDir !== sourceDir)
                                    return path.relative(mapDir, source).replace(/\\/g, '/');

                                return source;
                            });

                            map = output;
                        }
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
                    return;
                }

                if (params.autoprefixer !== undefined) {
                    var autoprefixedOutput = require("./srv-autoprefixer").processAutoprefixer(css, map, params.autoprefixerBrowsers, params.targetFileName, params.targetFileName);

                    if (!autoprefixedOutput.Success) {
                        writer.write(JSON.stringify({
                            Success: false,
                            SourceFileName: params.sourceFileName,
                            TargetFileName: params.targetFileName,
                            MapFileName: params.mapFileName,
                            Remarks: "LESS: " + autoprefixedOutput.Remarks,
                            Details: autoprefixedOutput.Remarks,
                            Errors: [{
                                Message: "LESS: " + autoprefixedOutput.Remarks,
                                FileName: params.sourceFileName
                            }]
                        }));
                        writer.end();
                        return;
                    }

                    css = autoprefixedOutput.css;
                    map = autoprefixedOutput.map;
                }

                if (params.rtlcss !== undefined) {
                    var rtlTargetWithoutExtension = params.targetFileName.substr(0, params.targetFileName.lastIndexOf("."));
                    var rtlTargetFileName = rtlTargetWithoutExtension + ".rtl.css";
                    var rtlMapFileName = rtlTargetFileName + ".map";
                    var rtlResult = require("./srv-rtlcss").processRtlCSS(css,
                                                                          map,
                                                                          params.targetFileName,
                                                                          rtlTargetFileName);


                    if (rtlResult.Success) {
                        writer.write(JSON.stringify({
                            Success: true,
                            SourceFileName: params.sourceFileName,
                            TargetFileName: params.targetFileName,
                            MapFileName: params.mapFileName,
                            RtlSourceFileName: params.targetFileName,
                            RtlTargetFileName: rtlTargetFileName,
                            RtlMapFileName: rtlMapFileName,
                            Remarks: "Successful!",
                            Content: css,
                            Map: JSON.stringify(map),
                            RtlContent: rtlResult.css,
                            RtlMap: JSON.stringify(rtlResult.map)
                        }));

                        writer.end();
                    } else {
                        throw new Error("Error while processing RTLCSS");
                    }
                } else {
                    writer.write(JSON.stringify({
                        Success: true,
                        SourceFileName: params.sourceFileName,
                        TargetFileName: params.targetFileName,
                        MapFileName: params.mapFileName,
                        Remarks: "Successful!",
                        Content: css,
                        Map: JSON.stringify(map)
                    }));
                }

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
