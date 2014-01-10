/* jshint node: true */
"use strict";

function PathFormatter() { }

PathFormatter.prototype.format = function (failures) {
    var messageItems = failures.map(function (result) {
        var lineAndCharacter = result.getStartPosition().getLineAndCharacter();

        return {
            Line: lineAndCharacter.line() + 1
          , Column: lineAndCharacter.character() + 1
          , Message: "TsLint: " + result.getFailure()
          , FileName: result.getFileName()
        };
    }).sort(function (a, b) {
        return a.Line < b.Line ? -1 : (a.Line > b.Line ? 1 : 0);
    });

    return JSON.stringify(messageItems);
};

module.exports = {
    Formatter: PathFormatter
};