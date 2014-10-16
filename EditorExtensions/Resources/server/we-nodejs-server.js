//#region Imports
var http = require("http"),
    url = require("url"),
    fs = require("fs"),
    program = require("commander");
//#endregion

//#region Utilities
var developmentEnv = true;

console.logType = { message: "\x1b[32;1m", error: "\x1b[31;1m", transaction: "\x1b[33;1m" };

console.logLine = function (message, type, indent, noTimeStamp) {
    if (!developmentEnv || noTimeStamp) {
        this.log(message);
        return;
    }

    // stringify null
    message += '';

    if (!type)
        type = this.logType.message;

    if (!indent)
        indent = true;

    var date = new Date().toUTCString();
    var columns = process.stdout.columns;
    var timeStampSlugSize = date.length + 4;

    if (!indent || message.length + timeStampSlugSize <= columns) {
        this.log("\x1b[36;1m%s \x1b[0m=> %s%s\x1b[0m", date, type, message);
        return;
    }

    var offset = 0;
    var limit = columns - timeStampSlugSize;
    var spaces = Array(timeStampSlugSize + 1).join(" ");

    while (true) {
        var remainder = message.substr(offset + limit, message.length - offset);

        if (remainder === "") break;

        message = message.substr(0, offset + limit) + spaces + remainder;

        offset += columns;
    }

    this.log("\x1b[36;1m%s \x1b[0m=> %s%s\x1b[0m", date, type, message);
};
//#endregion

//#region Validators
var antiForgeryToken;

var authenticateAll = function (writer, headers) {
    if (!headers["web-essentials"] ||
        headers.origin !== "web essentials" ||
        headers["user-agent"] !== "web essentials" ||
        headers.auth !== antiForgeryToken) {
        writer.writeHead(404, { "Content-Type": "text/plain" });
        writer.write("404 Not found");
        writer.end();

        return false;
    }

    return true;
};

var validPositiveInteger = function (int) {
    return int % (!isNaN(parseFloat(int)) && ~~int >= 0) === 0;
};

var validatePort = function (port) {
    var validity = false;

    if (validPositiveInteger(port)) {
        port = parseInt(port, 10);
        validity = port > 1023 && port < 65536;
    }

    if (!validity)
        console.logLine("Invalid Port!");

    return validity;
};
//#endregion

//#region Start
var start = function (port) {
    function onRequest(request, response) {
        console.logLine("Request recieved: " + JSON.stringify(request.headers), console.logType.transaction);

        // Unauthorized access checks (for production environment)
        if (!developmentEnv && !authenticateAll(response, request.headers))
            return;

        response.writeHead(200, { "Content-Type": "application/json" });
        //response.writeHead(200, { "Content-Type": "text/plain" });

        var params = url.parse(request.url, true).query;

        try {
            // Change to valid character string and let it respond as the regular (invalid) case
            if (!/^[a-zA-Z0-9_-]+$/.test(params.service))
                params.service = "invalid-service-name";

            var service = require('./services/srv-' + params.service.toLowerCase());

            if (!fs.existsSync(params.sourceFileName)) {
                response.write(JSON.stringify({
                    Success: false, Remarks: "Input file not found!"
                }));
                response.end();
                return;
            }

            service(response, params);
        } catch (e) {
            response.write(JSON.stringify({ Success: false, Remarks: e.stack }));
            response.end();
        }
    }

    http.createServer(onRequest).listen(port, "127.0.0.1");
    console.logLine("Server has started in " + (developmentEnv ? "development" : "production") + " environment");
    console.logLine("Started listening on port " + port);
};
//#endregion

//#region CronJob: auto-close
var checkIfParentExists = function (pid) {
    try {
        process.kill(pid, 0);
    }
    catch (e) {
        return false;
    }

    return true;
};
//#endregion

//#region CLI
program
  .option('-p, --port <n>', 'Port to bind to when listening for connections.', parseInt)
  .option('-a, --anti-forgery-token [value]', 'Token to validate requests with.')
  .option('-e, --environment [value]', 'Configures the environment to what you need.')
  .option('-pid, --process-id <n>', 'Process ID of the parent process.', parseInt)
  .parse(process.argv);

if (program.rawArgs.length === 10 && program.environment === "production") {
    developmentEnv = false;

    // Each VS instance upon loading Web Essentials for the first time,
    // will instantiate this server. This cron-job will check if the PID
    // of parent VS instance (which we will provide at handshake) exists,
    // if not (in case the instance gets crashed or terminated unexpectedly)
    // it will exit the sever and free-up the memory. Not sure if we need it
    // after the GC runs.. But I saw the node.exe process running even after
    // terminating the Experimental Instance from host. I happened once,
    // couldn't reproduce the behavior.
    //
    // NOTE: process.kill() that is used in checkIfParentExists() function,
    // with signal '0' is a magic code; it returns if the process is running
    // and throws otherwise (with SGNL 0 it doesn't kill the process).
    setInterval(function () {
        if (!checkIfParentExists(program.processId))
            process.exit(0);
    }, 300000);
}

if (!((!developmentEnv && program.rawArgs.length === 6 || program.rawArgs.length === 10) ||
    (developmentEnv && program.rawArgs.length === 4)) ||
    !validatePort(program.port) || (!developmentEnv && !program.antiForgeryToken)) {
    console.logLine("The server cannot start due to the insufficient or incorrect arguments. Exiting..", console.logType.error);
    process.exit(1);
}

start(process.argv[3]);
antiForgeryToken = process.argv[5];
//#endregion
