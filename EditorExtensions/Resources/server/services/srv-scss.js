//#region Imports
var exec = require("child_process").exec,
    fs = require("fs"),
    path = require("path"),
    xRegex = require("xregexp").XRegExp;
//#endregion

//#region Handler
var handleSass = function (writer, params) {

    // Call SassBuild (located at https://github.com/davidtme/SassBuild)

    var command = "..\\Tools\\sass";

    command += ' --quiet'
    command += ' --style ' + params.outputStyle;
    command += ' --load-path "' + path.dirname(params.sourceFileName) + '"';
    command += ' --cache-location "' + path.dirname(params.sourceFileName) + '\\.sass-cache"';

    if (params.sourceMapURL === undefined) {
        command += '  --sourcemap=none';
    }

    command += ' "' + params.sourceFileName + '"';
    command += ' "' + params.targetFileName + '"';

    fs.appendFileSync('C:\\Temp\\output.txt', command);

    var child = exec(command);

    child.on('exit', function (code) {
        if (code === 0) {

            var css = fs.readFileSync(params.targetFileName);
            var map = JSON.parse(fs.readFileSync(params.mapFileName));

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
        }
        else {
            var error = fs.readFileSync(params.targetFileName);
            var regex = xRegex.exec(error, xRegex("Error: (?<fullMessage>(?<message>.*))\r\n +?on line (?<line>[0-9]+) of (?<fileName>.+?)\r\n", 'gi'));

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
