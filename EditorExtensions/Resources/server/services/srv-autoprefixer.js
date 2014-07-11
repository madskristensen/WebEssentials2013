//#region Imports
var autoprefixer = require("autoprefixer"),
    fs = require("fs");
//#endregion

//#region Process
var processAutoprefixer = function (cssContent, mapContent, browsers, from, to) {
    var result = autoprefixer;

    if (browsers != undefined)
        try {
            result = autoprefixer(browsers.split(",").map(function (s) { return s.trim(); }));
        } catch (e) {
            // Return same css and map back so compilers can continue.
            return {
                Success: false,
                Remarks: "Autoprefixer: Invalid browser provided! See autoprefixer docs for list of valid browsers options.",
                css: cssContent,
                map: mapContent
            };
        }


    if (!mapContent)
        return {
            Success: true,
            css: result.process(cssContent).css
        };

    result = autoprefixer.process(cssContent, {
        map: { prev: mapContent },
        from: from,
        to: to
    });

    return {
        Success: true,
        css: result.css,
        map: result.map
    };
};
//#endregion

//#region Handler
var handleAutoPrefixer = function (writer, params) {
    if (!fs.existsSync(params.sourceFileName)) {
        writer.write(JSON.stringify({ Success: false, Remarks: "Autoprefix: Input file not found!" }));
        writer.end();
        return;
    }

    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({ Success: false, Remarks: "Autoprefixer: Error reading input file.", Details: err }));
            writer.end();
            return;
        }

        var output = processAutoprefixer(data, null, params.autoprefixerBrowsers);

        if (!output.Success)
            writer.write(JSON.stringify({
                Success: false,
                Remarks: output.Remarks,
            }));
        else
            writer.write(JSON.stringify({
                Success: true,
                Remarks: "Successful!",
                Output: {
                    Content: output.css
                }
            }));

        writer.end();
    });
};
//#endregion

//#region Exports
module.exports = handleAutoPrefixer;
module.exports.processAutoprefixer = processAutoprefixer;
//#endregion
