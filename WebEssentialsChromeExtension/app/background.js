/* global chrome */

function onRequest(request, sender, sendResponse) {

    if (request.enabled) {
        chrome.pageAction.show(sender.tab.id);

        setTimeout(function () {
            CheckAjax(sender.tab.id, request.url + "/robots.txt", "noRobotsTxt");
        }, 1000);
    }

    // Return nothing to let the connection be cleaned up.
    sendResponse({});
}

function CheckAjax(tabId, url, command) {
    
    var xhr = new XMLHttpRequest();
    xhr.open("GET", url + "?rnd=" + Math.random(), true);
    xhr.onreadystatechange = function () {

        if (xhr.readyState === 4) {
            chrome.tabs.sendMessage(tabId, { command: command, result: xhr.status !== 404 }, function () { });
            xhr.abort();
            xhr = null;
        }
    };

    xhr.send();
}

chrome.extension.onRequest.addListener(onRequest);