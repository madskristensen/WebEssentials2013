/* global chrome */

(function () {

    chrome.runtime.onMessage.addListener(
      function (request, sender, sendResponse) {
          window.postMessage({ sender: "WE", command: request.command, result: request.result }, "*");
          sendResponse({});
      });

    window.addEventListener("load", function () {

        if (document.getElementById("__browserLink_initializationData")) {

            chrome.extension.sendRequest({ enabled: true, url: location.protocol + "//" + location.host }, function () { });

            var script = document.createElement("script");
            script.innerHTML = 'window.addEventListener("message", function (event) {' +
                                   'if (event.data.command === "inspectMode")' +
                                       'window.__weSetInspectMode();' +
                                   'else if (event.data.command === "designMode")' +
                                       'window.__weSetDesignMode();' +
                                   'else if (event.data.command === "noRobotsTxt")' +
                                       'window.__weReportError("robotstxt", event.data.result);' +
                               '}, false);';

            document.body.appendChild(script);
        }
    }, false);

})();