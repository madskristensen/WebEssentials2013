/* global chrome */

var menu;

function onRequest(request, sender, sendResponse) {

    chrome.pageAction.show(sender.tab.id);

    if (!menu) {
        menu = chrome.contextMenus.create({ "title": "Web Essentials" });
        chrome.contextMenus.create({ "title": "Inspect Mode", "parentId": menu, "onclick": function () { execute("inspectMode", sender.tab.id) } });
        chrome.contextMenus.create({ "title": "Design Mode", "parentId": menu, "onclick": function () { execute("designMode", sender.tab.id) } });
    }

    // Return nothing to let the connection be cleaned up.
    sendResponse({});
}

function execute(command, tabId) {
    chrome.tabs.sendMessage(tabId, { command: command }, function (response) { });
}

chrome.extension.onRequest.addListener(onRequest);