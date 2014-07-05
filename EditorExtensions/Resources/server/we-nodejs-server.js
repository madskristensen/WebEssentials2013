//#region Imports
var controller = require("./controller.js"),
    http = require("http"),
    url = require("url"),
    os = require("os");
//#endregion

//#region Utilities
var developmentEnv = true;

console.logType = { message: "\x1b[32;1m", error: "\x1b[31;1m", transaction: "\x1b[33;1m" };

console.logLine = function (message, type, indent, noTimeStamp) {
    if (typeof (noTimeStamp) === 'undefined')
        noTimeStamp = false;

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
        remainder = message.substr(offset + limit, message.length - offset);

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
    return 0 === int % (!isNaN(parseFloat(int)) && 0 <= ~~int);
};

var validatePort = function (port) {
    var validity = false;

    if (validPositiveInteger(port)) {
        port = parseInt(port, 10);
        validity = port > 1023 && port < 65536;
    }

    if (!validity)
        console.log("Invalid Port!");

    return validity;
};
//#endregion

var start = function (port) {
    function onRequest(request, response) {
        console.logLine("Request recieved: " + JSON.stringify(request.headers), console.logType.transaction);

        // Unauthorized access checks (for production environment)
        if (!developmentEnv && !authenticateAll(response, request.headers))
            return;

        //response.writeHead(200, { "Content-Type": "application/json" });
        response.writeHead(200, { "Content-Type": "text/plain" });

        try {
            controller.handleRequest(response, url.parse(request.url, true).query);
        } catch (e) {
            response.write(e.stack);
        }
    }

    http.createServer(onRequest).listen(port, "127.0.0.1");
    console.logLine("Server has started..");
    console.logLine("Started listening on port" + port);
};

if (process.argv.length === 8 &&
   (process.argv[6] === "--environment" || process.argv[6] === "-e") &&
    process.argv[7] === "production")
    developmentEnv = false;

if (!((!developmentEnv && process.argv.length === 6 || process.argv.length === 8) || (developmentEnv && process.argv.length === 4)) ||
    !(process.argv[2] === "--port" || process.argv[2] === "-p") ||
    !validatePort(process.argv[3]) ||
    (!developmentEnv && !(process.argv[4] === "--anti-forgery-token" || process.argv[4] === "-a"))) {
    console.logLine("The server cannot start due to the insufficient or incorrect arguments. Exiting..", console.logType.error);
    process.exit(1);
}

start(process.argv[3]);
protetFromForgery = process.argv[5];
