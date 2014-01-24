/* jshint node: true */
"use strict";

module.exports = function (fileCollection) {
    var fileName = fileCollection[0].getFilename();
    var errorItems = fileCollection[0].getErrorList().map(function (error) {
        return {
            Line: error.line
          , Column: error.column + 1
          , Message: "JSCS: " + error.message
          , FileName: fileName
        };
    });
    process.stdout.write(JSON.stringify(messageItems));
};