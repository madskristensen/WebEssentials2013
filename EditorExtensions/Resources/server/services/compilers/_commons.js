//#region Imports
var fs = require("fs"),
    path = require("path"),
    mkdirp = require("mkdirp");
//#endregion

//#region Functions
var ensureDirectory = function (filepath) {
    var dir = path.dirname(filepath),
        cmd,
        existsSync = fs.existsSync || path.existsSync;
    if (!existsSync(dir)) {
        cmd = mkdirp && mkdirp.sync || fs.mkdirSync;
        cmd(dir);
    }
};
//#endregion

//#region Exports
exports.ensureDirectory = ensureDirectory;
//#endregion
