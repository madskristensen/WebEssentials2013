/* jshint node: true */
"use strict";

module.exports = {
    reporter: function (res) {
        var messageItems = res.map(function (result) {
            var error = result.error;

            if (error.reason === "Missing radix parameter.") {
                error.reason = "When using the parseInt function, remember to specify the radix parameter. Example: parseInt('3', 10)";
            }

            return {
                Line: parseInt(error.line, 10),
                Column: parseInt(error.character, 10),
                Message: "JsHint (" + error.code + "): " + error.reason,
                FileName: result.file
            };
        });

        process.stdout.write(JSON.stringify(messageItems));
    }
};
