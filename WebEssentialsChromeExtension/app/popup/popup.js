/* global chrome */

(function () {

    function Execute(command) {
        chrome.tabs.getSelected(null, function (tab) {
            chrome.tabs.sendMessage(tab.id, { command: command }, function () {
                window.close();
            });
        });
    }

    function setInspectMode() {
        Execute("inspectMode");
    }

    function setDesignMode() {
        Execute("designMode");
    }

    var btnInspect = document.getElementById("inspect");
    var btnDesign = document.getElementById("design");

    btnInspect.addEventListener("click", setInspectMode);
    btnDesign.addEventListener("click", setDesignMode);

})();