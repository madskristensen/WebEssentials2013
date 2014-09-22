//#region Imports
var rtlcss = require("rtlcss"),
    configLoader = require("rtlcss/lib/config-loader"),
    fs = require("fs"),
    path = require("path");
//#endregion

//#region Process
var processRtlCSS = function (cssContent, mapContent, autoprefixer, autoprefixerBrowsers, sourceFileName, targetFileName) {
    var result, css, map;
    try {
        var config = configLoader.load(null, path.dirname(sourceFileName), { options: { minify: false } });

        if (!mapContent) {

          css = rtlcss.configure(config).process(cssContent).css;
          map = mapContent;

        } else {

          // Clone object
          var oldMap = JSON.parse(JSON.stringify(mapContent));

          result = rtlcss.configure(config).process(cssContent, {
            map: typeof mapContent === "string" ? { prev: mapContent } : (typeof mapContent === "object" ? { prev: JSON.stringify(mapContent) } : mapContent),
            from: sourceFileName,
            to: targetFileName
          });

          // Curate maps
          if (typeof mapContent === "object")
            result.map.sources = mapContent.sources;

          css = result.css;
          map = result.map;
        }

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
                                   params.autoprefixer,
                                   params.autoprefixerBrowsers,
                                   params.sourceFileName,
                                   params.targetFileName);

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
