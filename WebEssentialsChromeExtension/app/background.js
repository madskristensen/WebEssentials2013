/* global chrome */

function onRequest(request, sender, sendResponse) {

    if (request.enabled) {
        chrome.pageAction.show(sender.tab.id);
    }

    // Return nothing to let the connection be cleaned up.
    sendResponse({});
}

chrome.extension.onRequest.addListener(onRequest);