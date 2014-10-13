//#region Imports
var sass = require("node-sass"),
    path = require("path"),
    xRegex = require("xregexp").XRegExp;
//#endregion

//#region Handler
var handleSass = function (writer, params) {
    sass.render({
        file: params.sourceFileName,
        outFile: params.targetFileName,
        includePaths: [path.dirname(params.sourceFileName)],
        precision: parseInt(params.precision, 10),
        outputStyle: params.outputStyle,
        sourceMap: params.mapFileName,
        omitSourceMapUrl: params.sourceMapURL === undefined,
        success: function (css, map) {
            map = JSON.parse(map);

            if (params.autoprefixer !== undefined) {
                var autoprefixedOutput = require("./srv-autoprefixer").processAutoprefixer(css, map, params.autoprefixerBrowsers, params.targetFileName, params.targetFileName);

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
                var rtlResult = require("./srv-rtlcss").processRtlCSS(css,
                                                                      map,
                                                                      params.targetFileName,
                                                                      rtlTargetFileName);


                if (rtlResult.Success === true) {
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
            var regex = xRegex.exec(error, xRegex("(?<fileName>.+):(?<line>.\\d+): error: (?<fullMessage>(?<message>.*))", 'gi'));
            writer.write(JSON.stringify({
                Success: false,
                SourceFileName: params.sourceFileName,
                TargetFileName: params.targetFileName,
                MapFileName: params.mapFileName,
                Remarks: "SASS: An error has occured while processing your request.",
                Details: regex.message,
                Errors: [{
                    Line: regex.line,
                    Message: "SASS: " + regex.message,
                    FileName: regex.fileName,
                    FullMessage: "SASS" + regex.fullMessage
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
