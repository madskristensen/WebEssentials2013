//#region Imports
var sass = require("node-sass"),
    path = require("path")
//#endregion

//#region Handler
var handleSass = function (writer, params) {
    var omitSourceMap = typeof params.sourceMapURL === 'undefined';

    sass.render({
        file: params.sourceFileName,
        outFile: params.targetFileName,
        includePaths: [path.dirname(params.sourceFileName)],
        precision: parseInt(params.precision, 10),
        outputStyle: params.outputStyle,
        sourceMap: params.mapFileName,
        omitSourceMapUrl: omitSourceMap,
        success: function (result) {
            var css = result.css, map;
            if (!omitSourceMap)
                map = result.map;

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
                        Remarks: "SASS: " + autoprefixedOutput.Remarks,
                        Details: autoprefixedOutput.Remarks,
                        Errors: [{
                            Message: "SASS: " + autoprefixedOutput.Remarks,
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
        },
        error: function (error) {
            writer.write(JSON.stringify({
                Success: false,
                SourceFileName: params.sourceFileName,
                TargetFileName: params.targetFileName,
                MapFileName: params.mapFileName,
                Remarks: "SASS: An error has occured while processing your request.",
                Details: error.message,
                Errors: [{
                    Line: error.line,
                    Column: error.column,
                    Message: "Message: " + error.message,
                    FileName: error.file,
                    FullMessage: "Code: " + error.code + " Message: " + error.message
                }]
            }));
            writer.end();
        }
    });
};
//#endregion

//#region Exports
module.exports = handleSass;
//#endregion
