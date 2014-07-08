//#region Imports
var http = require("http"),
    url = require("url"),
    os = require("os"),
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

    message = message.toString();

    if (typeof (type) === 'undefined')
        type = this.logType.message;

    if (typeof (indent) === 'undefined')
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
var protetFromForgery;

var authenticateAll = function (writer, headers) {
    if (!headers["web-essentials"] ||
        headers.origin !== "web essentials" ||
        headers["user-agent"] !== "web essentials" ||
        headers.auth !== protetFromForgery ||
        !validateUptime(headers.uptime)) {
        writer.writeHead(404, { "Content-Type": "text/plain" });
        writer.write("404 Not found");
        writer.end();

        return false;
    }

    return true;
};

var validateUptime = function (then) {
    if (!validPositiveInteger(then)) {
        return false;
    }

    var now = parseInt(os.uptime(), 10);

    then = parseInt(then, 10);

    return then < now && now - 5 < then;
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

        //response.writeHead(200, { "Content-Type": "application/json" });
        response.writeHead(200, { "Content-Type": "text/plain" });

        try {
            (function (writer, params) {
                if (!/^[a-zA-Z0-9-_]+$/.test(params.service))
                    // Change to valid character string and let it respond as the regular (invalid) case
                    params.service = "invalid-service-name";

                require('./services/srv-' + params.service)(writer, params);
            })(response, url.parse(request.url, true).query);
        } catch (e) {
            response.write(JSON.stringify({ Success: false, Remarks: e.stack }));
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
  .option('-e, --environment [value]', 'Configures your Rails environment to what you need.')
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
        require("fs").appendFile('c:/temp/message.txt', program.processId + '\n');
        if (!checkIfParentExists(program.processId)) {
            require("fs").appendFile('c:/temp/message.txt', 'exiting: ' + program.processId + '\n');
            exit(1);
        }
    }, 300000);
}

if (!((!developmentEnv && program.rawArgs.length === 6 || program.rawArgs.length === 10) ||
    (developmentEnv && program.rawArgs.length === 4)) ||
    !validatePort(program.port) || (!developmentEnv && !program.antiForgeryToken)) {
    console.logLine("The server cannot start due to the insufficient or incorrect arguments. Exiting..", console.logType.error);
    process.exit(1);
}

start(process.argv[3]);
protetFromForgery = process.argv[5];
//#endregion
