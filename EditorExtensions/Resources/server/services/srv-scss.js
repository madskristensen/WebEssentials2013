//#region Imports
var sass = require("node-sass"),
    path = require("path"),
    xRegex = require("xregexp").XRegExp;
//#endregion

//#region Handler
var handleSass = function (writer, params) {
    params.targetFileName = params.targetFileName.replace(/\\/g, '/');
    params.sourceFileName = params.sourceFileName.replace(/\\/g, '/');
    params.mapFileName = params.mapFileName.replace(/\\/g, '/');

    sass.render({
        file: params.sourceFileName,
        includePaths: [path.dirname(params.sourceFileName)],
        precision: parseInt(params.precision, 10),
        outputStyle: params.outputStyle,
        sourceMap: params.mapFileName,
        success: function (css, map) {
            map = JSON.parse(map);
            map.file = path.basename(params.targetFileName);
            if (params.autoprefixer !== undefined) {
                var autoprefixedOutput = require("./srv-autoprefixer").processAutoprefixer(css, map, params.autoprefixerBrowsers, params.sourceFileName, params.targetFileName);
                css = autoprefixedOutput.css;
                map = autoprefixedOutput.map;
            }

            // SASS doesn't generate source-maps without source-map comments
            // (unlike LESS and CoffeeScript). So we need to delete the comment
            // manually if its not present in params.
            if (params.sourceMapURL === undefined) {
                var soucemapCommentLineIndex = css.lastIndexOf("\n");
                if (soucemapCommentLineIndex > 0) {
                    css = css.substring(0, soucemapCommentLineIndex);
                }
            }

            if (params.rtlcss !== undefined) {
                var rtlResult = require("./srv-rtlcss").processRtlCSS(css,
                                                                      map,
                                                                      params.autoprefixer,
                                                                      params.autoprefixerBrowsers,
                                                                      params.sourceFileName,
                                                                      params.targetFileName);
                var rtlTargetWithoutExtension = params.targetFileName.substr(0, params.targetFileName.lastIndexOf("."));

                if (rtlResult.Success === true) {
                    writer.write(JSON.stringify({
                        Success: true,
                        SourceFileName: params.sourceFileName,
                        TargetFileName: params.targetFileName,
                        MapFileName: params.mapFileName,
                        RtlSourceFileName: params.targetFileName,
                        RtlTargetFileName: rtlTargetWithoutExtension + ".rtl.css",
                        RtlMapFileName: rtlTargetWithoutExtension + ".rtl.css.map",
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
