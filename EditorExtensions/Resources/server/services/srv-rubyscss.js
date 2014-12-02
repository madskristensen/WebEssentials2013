//#region Imports
var fs = require("fs"),
    path = require("path"),
    xRegex = require("xregexp").XRegExp,
    querystring = require('querystring'),
    http = require('http');
//#endregion

//#region Handler
var handleSass = function (writer, params) {

    // Call SassBuild (located at https://github.com/davidtme/SassBuild)

    var post_data = querystring.stringify({
        'sourceFileName': params.sourceFileName,
        'targetFileName': params.targetFileName,
        'mapFileName': params.mapFileName
    });

    var post_options = {
        host: 'localhost',
        port: params.rubyPort,
        path: '/convert',
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'Content-Length': post_data.length,
            'auth': params.rubyAuth
        }
    };

    // Set up the request
    var post_req = http.request(post_options, function (res) {
        res.setEncoding('utf8');
        res.on('data', function (chunk) {

            try {
                var result = JSON.parse(chunk);
            }
            catch (ex) { ///got a bad response from the compiler, lets report something useful instead of crashing
                writer.write(JSON.stringify({
                    Success: true,
                    SourceFileName: params.sourceFileName,
                    TargetFileName: params.targetFileName,
                    MapFileName: params.mapFileName,
                    Remarks: "Unable to Compile"
                }));
            }

            if (result.css !== undefined) {

                fs.writeFileSync(params.targetFileName, result.css);
                fs.writeFileSync(params.mapFileName, JSON.stringify(result.map));

                var css = result.css;
                var map = result.map;

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

            } else {

                writer.write(JSON.stringify({
                    Success: false,
                    SourceFileName: params.sourceFileName,
                    TargetFileName: params.targetFileName,
                    MapFileName: params.mapFileName,
                    Remarks: "SASS: An error has occured while processing your request.",
                    Details: result.message,
                    Errors: [{
                        Line: result.line,
                        Message: "SASS: " + result.message,
                        FileName: result.fileName,
                        FullMessage: "SASS" + result.message
                    }]
                }));
                writer.end();
            }
        });
    });

    // post the data
    post_req.write(post_data);
    post_req.end();
};


//#endregion

//#region Exports
module.exports = handleSass;
//#endregion
