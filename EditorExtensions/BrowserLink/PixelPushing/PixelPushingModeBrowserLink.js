/// <reference path="../_intellisense/browserlink.intellisense.js" />
/// <reference path="../_intellisense/jquery-1.8.2.js" />

(function (browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    var chunkSize = 20 * 1024; //<-- 20k chunk size
    var lastSheet = false;
    var isInPixelPusingMode = false;

    function getLastSheetHref() {
        if (!lastSheet) {
            for (var i = document.styleSheets.length - 1; i >= 0; --i) {
                if ((document.styleSheets[i].href || "") !== "") {
                    return lastSheet = document.styleSheets[i].href;
                }
            }
        }

        return lastSheet;
    }

    function getSheetHref(sheet) {
        if ((sheet.href || "") !== "") {
            return sheet.href;
        }

        return getLastSheetHref();
    }

    function chunk(obj) {
        var str = JSON.stringify(obj);
        var remainingSize = str.length;
        var result = [];
        var offset = 0;

        while (remainingSize > 0) {
            var length = remainingSize > chunkSize ? chunkSize : remainingSize;
            result.push(str.substr(offset, length));
            remainingSize -= length;
            offset += length;
        }

        return result;
    }

    var updateNumber = 0;

    function submitChunkedData(method, data) {
        var myUpdateNumber = updateNumber++;
        var chunked = chunk(data);
        for (var i = 0; i < chunked.length; ++i) {
            browserLink.invoke(method, myUpdateNumber, chunked[i], i, chunked.length);
        }
    }

    var deltaLog = [];
    var continuousSyncMode = false;

    function shipUpdate() {
        var localLog = deltaLog;
        deltaLog = [];
        submitChunkedData("SyncCssRules", localLog);
    }

    $(document).keydown(function (e) {
        if (e.ctrlKey && e.altKey && e.keyCode === 84) { // 84 = t
            shipUpdate();
        }
        else if (e.ctrlKey && e.altKey && e.keyCode === 85) { // 84 = t {// 85 = u
            setContinuousSyncMode(!continuousSyncMode);
        }
    });

    function standardizeSelector(selector) {
        /*
            var tmp = selectorText.Replace('\r', ' ').Replace('\n', ' ').Trim().ToLowerInvariant();

            while (tmp.Contains("  "))
            {
                tmp = tmp.Replace("  ", " ");
            }

            return tmp.Replace(", ", ",");
        */

        var tmp = selector.replace(/\r/g, " ").replace(/\n/g, " ").trim().toLowerCase();

        while (tmp.indexOf("  ") != -1) {
            tmp = tmp.replace(/\s\s/g, " ");
        }

        return tmp.replace(/,\s/g, ",");
    }

    function setPixelPushingModeInternal(pixelPushingModeOn, continuousSync) {
        if (isInPixelPusingMode = pixelPushingModeOn) {
            setContinuousSyncMode(continuousSync);
            performAudit();
        } else {
            setContinuousSyncMode(false);
        }

        updateMenuItems();
    }

    var lastRunSheets = [];

    function setContinuousSyncMode(value) {
        if (value != continuousSyncMode) {
            browserLink.invoke("EnterContinuousSyncMode", !!value);
        }

        continuousSyncMode = value;
        updateMenuItems();
    }

    function getCurrentRuleDefinitions() {
        var sheets = document.styleSheets;
        var tmp = [];

        for (var i = 0; i < sheets.length; ++i) {
            tmp.push({ "href": getSheetHref(sheets[i]) });
            var sheet = sheets[i];
            var rules = sheet.cssRules;

            // Some Chrome Extension add style sheets to the page and the rules can't be access or return null.
            if (rules === null) {
                continue;
            }

            var nameEncounters = {};

            for (var j = 0; j < rules.length; ++j) {
                var selector = rules[j].selectorText;

                if (!selector) {
                    continue;
                }

                selector = standardizeSelector(rules[j].selectorText);
                nameEncounters[selector] = (typeof (nameEncounters[selector]) === typeof (0) ? nameEncounters[selector] + 1 : 0);
                tmp[i][selector] = (tmp[i][selector] || {});
                tmp[i][selector][nameEncounters[selector]] = rules[j].cssText;
            }
        }

        return tmp;
    }

    function performAudit() {
        var current = getCurrentRuleDefinitions();
        var logRecords = [];
        var okToAdd = true;

        if (current.length == lastRunSheets.length) {
            for (var sheetIndex = 0; sheetIndex < current.length; ++sheetIndex) {
                var currentSheet = current[sheetIndex];
                var previousSheet = lastRunSheets[sheetIndex];
                var href = getSheetHref(document.styleSheets[sheetIndex]);

                if (href != previousSheet.href) {
                    deltaLog = [];
                    okToAdd = false;
                    break;
                }

                for (var rule in currentSheet) {
                    if (!currentSheet.hasOwnProperty(rule) || !previousSheet.hasOwnProperty(rule) || rule === "href") {
                        continue;
                    }

                    var currentRule = currentSheet[rule];
                    var previousRule = previousSheet[rule];

                    for (var ruleIndex in currentRule) {
                        if (!currentRule.hasOwnProperty(ruleIndex) || !previousRule.hasOwnProperty(ruleIndex)) {
                            continue;
                        }

                        if (currentRule[ruleIndex] != previousRule[ruleIndex]) {
                            logRecords.push({
                                "Url": href,
                                "RuleIndex": ruleIndex,
                                "Rule": rule,
                                "OldValue": previousRule[ruleIndex],
                                "NewValue": currentRule[ruleIndex]
                            });
                        }
                    }
                }
            }

            if (okToAdd && logRecords.length > 0) {
                deltaLog.push(logRecords);
            }
        }

        if (deltaLog.length > 0) {
            shipUpdate();
        }

        lastRunSheets = current;

        if (continuousSyncMode) {
            setTimeout(performAudit, 100);
        }
    }

    var takeChangesNowMenuItem;
    var continuouslyTakeChangesMenuItem;

    function updateMenuItems() {
        if (!window.browserLink.menu || !takeChangesNowMenuItem || !continuouslyTakeChangesMenuItem) {
            return;
        }

        if (!isInPixelPusingMode) {
            takeChangesNowMenuItem.disable();
            continuouslyTakeChangesMenuItem.disable();
            continuouslyTakeChangesMenuItem.checked(false);
        } else {
            continuouslyTakeChangesMenuItem.enable();
            continuouslyTakeChangesMenuItem.checked(continuousSyncMode);

            if (!continuousSyncMode) {
                takeChangesNowMenuItem.enable();
            } else {
                takeChangesNowMenuItem.disable();
            }
        }
    }

    function AddToMenu() {
        if (!window.browserLink.menu)
            return;

        takeChangesNowMenuItem = window.browserLink.menu.addButton("Save F12 changes", "Use CTRL+ALT+T to sync the current CSS changes into Visual Studio", function () {
            performAudit();
        });

        continuouslyTakeChangesMenuItem = window.browserLink.menu.addCheckbox("F12 auto-sync", "Use CTRL+ALT+U to continuously sync CSS changes into Visual Studio", false, function () {
            setContinuousSyncMode(!continuousSyncMode);
        });

        updateMenuItems();
    }

    return {
        setPixelPusingMode: setPixelPushingModeInternal,
        pullStyleData: performAudit,
        setContinuousSync: setContinuousSyncMode,
        onConnected: function () {
            AddToMenu();
        }
    };
});