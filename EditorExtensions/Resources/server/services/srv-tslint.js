//#region Imports
var tslint = require("tslint"),
    path = require("path"),
    fs = require("fs"),
    findup = require("findup-sync");
//#endregion

//#region Configuration Finder
// When https://github.com/palantir/tslint/pull/178 is merged and committed,
// we need to make two changes: 1) remove this region entirely and use:
// var config = tslint.Configuration.findConfiguration(null, path.dirname(params.sourceFileName)); 
// in handleTSLint.
// 2) Add the folowing to PreBuildTask.cs after node-sass' middleware
// (currently line 73):
// // Hack: for TsLint until they expose the main namespace to the API:
// // https://github.com/palantir/tslint/pull/178#issuecomment-48493723
// using (StreamWriter sw = File.AppendText(@"Resources\nodejs\tools\node_modules\tslint\lib\tslint.js"))
//     sw.WriteLine("module.exports = Lint;");
var CONFIG_FILENAME = "tslint.json";

function findConfiguration(configFile, inputFileLocation) {
    if (configFile) {
        return JSON.parse(fs.readFileSync(configFile, "utf8"));
    }

    // First look for package.json from input file location
    configFile = findup("package.json", { cwd: inputFileLocation, nocase: true });

    if (configFile) {
        var content = require(configFile);

        if (content.tslintConfig) {
            return content.tslintConfig;
        }
    }

    // Next look for tslint.json
    var homeDir = getHomeDir();

    if (!homeDir) {
        return undefined;
    }

    var defaultPath = path.join(homeDir, CONFIG_FILENAME);

    configFile = findup(CONFIG_FILENAME, { cwd: inputFileLocation, nocase: true }) || defaultPath;

    return configFile ? JSON.parse(fs.readFileSync(configFile, "utf8")) : undefined;
}

function getHomeDir() {
    var environment = global.process.env;
    var paths = [environment.HOME, environment.USERPROFILE, environment.HOMEPATH, environment.HOMEDRIVE + environment.HOMEPATH];

    for (var homeIndex in paths) {
        if (paths.hasOwnProperty(homeIndex)) {
            var homePath = paths[homeIndex];

            if (homePath && fs.existsSync(homePath)) {
                return homePath;
            }
        }
    }
}
//#endregion

//#region Reporter
var reporter = function (results, writer) {
    if (results)
        var messageItems = results.map(function (result) {
            return {
                Line: result.startPosition.line + 1,
                Column: result.startPosition.character + 1,
                Message: "TsLint: " + result.failure,
                FileName: result.name
            };
        }).sort(function (a, b) {
            return a.Line < b.Line ? -1 : (a.Line > b.Line ? 1 : 0);
        });

    writer.write(JSON.stringify({
        Success: true,
        Remarks: "Successful!",
        Output: {
            Content: messageItems || []
        }
    }));
    writer.end();
}
//#endregion

//#region Handler
var handleTSLint = function (writer, params) {
    var config;

    try {
        config = findConfiguration(null, path.dirname(params.sourceFileName));
    } catch (e) { }

    if (config == null) {
        writer.write(JSON.stringify({ Success: false, Remarks: "TSLint: Invalid Config file" }));
        writer.end();
        return;
    }

    fs.readFile(params.sourceFileName, 'utf8', function (err, data) {
        if (err) {
            writer.write(JSON.stringify({ Success: false, Remarks: "TSLint: Error reading input file.", Details: err }));
            writer.end();
            return;
        }

        var results = new tslint(params.sourceFileName, data, {
            configuration: config,
            formatter: 'json',
            formattersDirectory: null,
            rulesDirectory: null
        }).lint();

        reporter(JSON.parse(results.output), writer);
    });
};
//#endregion

//#region Exports
module.exports = handleTSLint;
//#endregion
