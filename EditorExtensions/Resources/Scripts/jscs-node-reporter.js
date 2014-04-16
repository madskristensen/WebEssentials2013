/* jshint node: true */
"use strict";

module.exports = function (errorCollection) {
    var errorItems = errorCollection.map(function (errors) {
        if (!errors.isEmpty()) {
            errors.getErrorList().forEach(function (error) {
                var fileName = error.getFilename();

                return {
                    Line: error.line
                  , Column: error.column + 1
                  , Message: "JSCS: " + error.message
                  , FileName: fileName
                };
            });
        }
    });
    process.stdout.write(JSON.stringify(errorItems));
};