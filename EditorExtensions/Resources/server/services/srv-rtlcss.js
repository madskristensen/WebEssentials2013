//#region Imports
var rtlcss = require("rtlcss"),
    configLoader = require("rtlcss/lib/config-loader"),
    fs = require("fs"),
    path = require("path");
//#endregion

//#region Process
var processRtlCSS = function (cssContent, mapContent, sourceFileName, targetFileName, autoprefixer, autoprefixerBrowsers) {
    if (mapContent !== true) {
        mapContent = { prev: mapContent };
    }

    var result, css, map;
    try {
        var config = configLoader.load(null, path.dirname(sourceFileName), { options: { minify: false } });

        result = rtlcss.configure(config).process(cssContent, {
            map: mapContent,
            from: sourceFileName,
            to: targetFileName
        });

        css = result.css;
        map = result.map.toJSON();
    } catch (e) {
        // Return same css and map back so the upstream compilers can continue.
        return {
            Success: false,
            Remarks: "RTLCSS: Exception occured: " + e.message,
            css: cssContent,
            map: mapContent
        };
    }


    if (autoprefixer !== undefined) {
        var autoprefixedOutput = require("./srv-autoprefixer").processAutoprefixer(css, map, autoprefixerBrowsers, sourceFileName, targetFileName);
        css = autoprefixedOutput.css;
        map = autoprefixedOutput.map;
    }

    return {
        Success: true,
        css: css,
        map: map
    };
};
//#endregion

//#region Handler
var handleRtlCSS = function (writer, params) {
    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({
                Success: false,
                SourceFileName: params.sourceFileName,
                TargetFileName: params.targetFileName,
                Remarks: "RTLCSS: Error reading input file.",
                Details: err,
                Errors: [{
                    Message: "RTLCSS" + err,
                    FileName: params.sourceFileName
                }]
            }));
            writer.end();
            return;
        }

        var output = processRtlCSS(data,
                                   true,
                                   params.sourceFileName,
                                   params.targetFileName,
                                   params.autoprefixer,
                                   params.autoprefixerBrowsers);

        if (!output.Success) {
            writer.write(JSON.stringify({
                Success: false,
                SourceFileName: params.sourceFileName,
                TargetFileName: params.targetFileName,
                Remarks: "RTLCSS: " + output.Remarks,
                Errors: [{
                    Message: output.Remarks,
                    FileName: params.sourceFileName
                }]
            }));
        } else {
            writer.write(JSON.stringify({
                Success: true,
                SourceFileName: params.sourceFileName,
                TargetFileName: params.targetFileName,
                MapFileName: params.mapFileName,
                Remarks: "Successful!",
                Content: output.css,
                Map: JSON.stringify(output.map)
            }));
        }

        writer.end();
    });
};
//#endregion

//#region Exports
module.exports = handleRtlCSS;
module.exports.processRtlCSS = processRtlCSS;
//#endregion
