/* jshint node: true */
"use strict";

module.exports = function (errorCollection) {
    var errorItems = [];

    if (errorCollection[0]) {
        var errors = errorCollection[0];
        var fileName = errors.getFilename();

        if (!errors.isEmpty()) {
            errorItems = errors.getErrorList().map(function (error) {
                return {
                    Line: error.line,
                    Column: error.column + 1,
                    Message: "JSCS: " + error.message,
                    FileName: fileName
                };
            });
        }
    }

    process.stdout.write(JSON.stringify(errorItems));
};
