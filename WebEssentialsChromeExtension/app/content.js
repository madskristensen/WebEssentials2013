/* global chrome */

(function () {

    chrome.runtime.onMessage.addListener(
      function (request, sender, sendResponse) {
          window.postMessage({ sender: "WE", command: request.command }, "*");
          sendResponse({});
      });

    window.addEventListener("load", function () {

        if (document.getElementById("__browserLink_initializationData")) {

            chrome.extension.sendRequest({ enabled: true }, function () { });

            var script = document.createElement("script");
            script.innerHTML = 'window.addEventListener("message", function (event) {' +
                                   'if (event.data.command === "inspectMode")' +
                                       'window.__weSetInspectMode();' +
                                   'else if (event.data.command === "designMode")' +
                                       'window.__weSetDesignMode();' +
                               '}, false);';

            document.body.appendChild(script);
        }
    }, false);

})();