//#region Imports
var livescript = require("LiveScript"),
    fs = require("fs");
//#endregion

//#region Handler
var handleLiveScript = function (writer, params) {
    var options = {
        filename: params.sourceFileName,
        bare: params.bare !== null
    };

    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({ Success: false, Remarks: "Error reading input file." }));
            writer.end();
            return;
        }

        try {
            compiled = livescript.compile(data, options);

            writer.write(JSON.stringify({
                Success: true,
                Remarks: "Successful!",
                Output: {
                    outputContent: compiled
                    // Maps aren't supported yet: https://github.com/gkz/LiveScript/issues/452
                }
            }));
            writer.end();
        } catch (error) {
            writer.write(JSON.stringify({ Success: false, Remarks: error.stack || ("" + error) }));
            writer.end();
        }
    });
};
//#endregion

//#region Exports
module.exports = handleLiveScript;
//#endregion
