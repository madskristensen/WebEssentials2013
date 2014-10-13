//#region Imports
var autoprefixer = require("autoprefixer"),
    fs = require("fs");
//#endregion

//#region Process
var processAutoprefixer = function (cssContent, mapContent, browsers, sourceFileName, targetFileName) {
    var result = autoprefixer;

    if (browsers !== undefined)
        try {
            result = autoprefixer(browsers.split(",").map(function (s) { return s.trim(); }));
        } catch (e) {
            // Return same css and map back so compilers can continue.
            return {
                Success: false,
                Remarks: "Invalid browser provided! See autoprefixer docs for list of valid browsers options.",
                css: cssContent,
                map: mapContent
            };
        }

    if (!mapContent)
        return {
            Success: true,
            css: result.process(cssContent).css
        };

    result = result.process(cssContent, {
        map: { prev: mapContent },
        from: sourceFileName,
        to: targetFileName
    });

    // Curate maps
    mapContent = result.map.toJSON();

    return {
        Success: true,
        css: result.css,
        map: mapContent
    };
};
//#endregion

//#region Handler
var handleAutoPrefixer = function (writer, params) {
    if (!fs.existsSync(params.sourceFileName)) {
        writer.write(JSON.stringify({
            Success: false,
            SourceFileName: params.sourceFileName,
            TargetFileName: params.targetFileName,
            Remarks: "Autoprefixer: Input file not found!",
            Errors: [{
                Message: "Autoprefixer: Input file not found!",
                FileName: params.sourceFileName
            }]
        }));
        writer.end();
        return;
    }

    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({
                Success: false,
                SourceFileName: params.sourceFileName,
                TargetFileName: params.targetFileName,
                Remarks: "Autoprefixer: Error reading input file.",
                Details: err,
                Errors: [{
                    Message: "Autoprefixer: " + err,
                    FileName: params.sourceFileName
                }]
            }));
            writer.end();
            return;
        }

        var output = processAutoprefixer(data, null, params.autoprefixerBrowsers);

        if (!output.Success) {
            writer.write(JSON.stringify({
                Success: false,
                SourceFileName: params.sourceFileName,
                TargetFileName: params.targetFileName,
                Remarks: "Autoprefixer: " + output.Remarks,
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
                Remarks: "Successful!",
                Content: output.css
            }));
        }

        writer.end();
    });
};
//#endregion

//#region Exports
module.exports = handleAutoPrefixer;
module.exports.processAutoprefixer = processAutoprefixer;
//#endregion
