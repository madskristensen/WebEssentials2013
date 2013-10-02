/// <reference path="../_intellisense/browserlink.intellisense.js" />
/// <reference path="../_intellisense/jquery-1.8.2.js" />

(function(browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    var chunkSize = 20 * 1024; //<-- 20k chunk size
    var lastSheet = false;
    var isInPixelPusingMode = false;
    var operationId;

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


    function submitChunkedData(method, data) {
        var chunked = chunk(data);
        for (var i = 0; i < chunked.length; ++i) {
            browserLink.invoke(method, operationId, chunked[i], i, chunked.length);
        }
    }

    function shipUpdate(delta) {
        submitChunkedData("SyncCssRules", delta);
    }

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

    function setPixelPushingModeInternal(pixelPushingModeOn, newOperationId) {
        operationId = newOperationId || operationId;

        if (isInPixelPusingMode = pixelPushingModeOn) {
            performAudit();
        }
    }

    var lastRunSheets = [];

    function getCurrentRuleDefinitions() {
        var sheets = document.styleSheets;
        var tmp = [];
        
        for (var i = 0; i < sheets.length; ++i) {
            tmp.push({ "href": getSheetHref(sheets[i]) });
            var sheet = sheets[i];
            var rules = sheet.cssRules;
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
        var deltaRecords = [];

        if (current.length == lastRunSheets.length) {
            for (var sheetIndex = 0; sheetIndex < current.length; ++sheetIndex) {
                var currentSheet = current[sheetIndex];
                var previousSheet = lastRunSheets[sheetIndex];
                var href = getSheetHref(document.styleSheets[sheetIndex]);
                
                if (href != previousSheet.href) {
                    deltaRecords = [];
                    break;
                }
                
                for (var rule in currentSheet) {
                    if (!currentSheet.hasOwnProperty(rule) || !previousSheet.hasOwnProperty(rule)) {
                        continue;
                    }

                    var currentRule = currentSheet[rule];
                    var previousRule = previousSheet[rule];
                    
                    for (var ruleIndex in currentRule) {
                        if (!currentRule.hasOwnProperty(ruleIndex) || !previousRule.hasOwnProperty(ruleIndex)) {
                            continue;
                        }
                        
                        if (currentRule[ruleIndex] != previousRule[ruleIndex]) {
                            deltaRecords.push({
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
        }

        if (deltaRecords.length > 0) {
            shipUpdate(deltaRecords);
        }

        lastRunSheets = current;

        if (isInPixelPusingMode) {
            setTimeout(performAudit, 100);
        }
    }

    return {
        setPixelPusingMode: setPixelPushingModeInternal
    };
});