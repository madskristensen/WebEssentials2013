//#region Imports
var less = require("less"),
    fs = require("fs"),
    path = require("path");
//#endregion

//#region Handler
var handleLess = function(writer, params) {
    fs.readFile(params.sourceFileName, 'utf8', function(err, data) {
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

        var css, map;
        var mapFileName = params.targetFileName + ".map";
        var sourceDir = path.dirname(params.sourceFileName);
        var options = {
            filename: params.sourceFileName,
            relativeUrls: true,
            paths: [sourceDir],
            sourceMap: {
                sourceMapFullFilename: mapFileName,
                sourceMapURL: params.sourceMapURL !== undefined ? path.basename(mapFileName) : null,
                sourceMapBasepath: sourceDir,
                sourceMapOutputFilename: path.basename(params.targetFileName),
                sourceMapRootpath: path.relative(path.dirname(params.targetFileName), sourceDir)
            },
            strictMath: params.strictMath !== undefined,
        };

        less.render(data, options, function(error, output) {
            if (error) {
                writer.write(JSON.stringify({
                    Success: false,
                    SourceFileName: params.sourceFileName,
                    TargetFileName: params.targetFileName,
                    MapFileName: params.mapFileName,
                    Remarks: "LESS: Error parsing input file.",
                    Details: error.message,
                    Errors: [{
                        Line: error.line,
                        Column: error.column,
                        Message: "LESS: " + error.message,
                        FileName: error.filename
                    }]
                }));
                writer.end();
                return;
            }

            css = output.css;
            map = JSON.parse(output.map);

            if (params.autoprefixer !== undefined) {
                var autoprefixedOutput = require("./srv-autoprefixer")
                                        .processAutoprefixer(css, map, params.autoprefixerBrowsers,
                                                             params.targetFileName, params.targetFileName);

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
                var rtlResult = require("./srv-rtlcss")
                               .processRtlCSS(css, map, params.targetFileName, rtlTargetFileName);

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
    });
};
//#endregion

//#region Exports
module.exports = handleLess;
//#endregion
