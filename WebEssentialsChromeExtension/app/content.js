/* global chrome */

(function () {

    chrome.runtime.onMessage.addListener(
      function (request, sender, sendResponse) {
          window.postMessage({ sender: "WE", command: request.command }, "*");
          sendResponse({});
      });

    window.addEventListener("load", function (event) {

        if (document.getElementById("__browserLink_initializationData")) {

            chrome.extension.sendRequest({}, function (response) { });

            var script = document.createElement("script");
            script.innerHTML = 'window.addEventListener("message", function (event) {' +
                                   'if (event.data.command === "inspectMode")' +
                                       'window.__we_setInspectMode();' +
                                   'else if (event.data.command === "designMode")' +
                                       'window.__we_setDesignMode();' +
                               '}, false);';

            document.body.appendChild(script);
        }
    }, false);

})();