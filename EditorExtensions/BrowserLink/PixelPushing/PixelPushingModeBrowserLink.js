/// <reference path="../_intellisense/browserlink.intellisense.js" />
/// <reference path="../_intellisense/jquery-1.8.2.js" />

(function(browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    /*
    [   //sheet index
        {   /*rule selector name/ :
            [   //index within duplicately named selector group
                {   /*property name/ : 
                    [
                        /*property value history/
                    ]
                }
            ]
        }
    ]
    */

    var chunkSize = 20 * 1024; //<-- 20k chunk size
    var deltaLog = [];
    var lastSheet = false;
    var isInPixelPusingMode = false;

    function getWithCoalesceAssign(from, prop, nullValue) {
        var wasCreated = false;
        if (typeof (from[prop]) === typeof (undefined)) {
            from[prop] = nullValue;
            wasCreated = true;
        }

        return {
            value: function () {
                return from[prop];
            },
            created: wasCreated
        };
    }

    function getPropertyBag(sheetIndex, selectorText, selectorTextDisambiguationIndex) {
        var ambiguousSelectorBag = getWithCoalesceAssign(deltaLog[sheetIndex], selectorText, []).value();

        while (ambiguousSelectorBag.length <= selectorTextDisambiguationIndex) {
            ambiguousSelectorBag.push({});
        }

        return ambiguousSelectorBag[selectorTextDisambiguationIndex];
    }

    function deletePropertyValue(sheetIndex, selectorText, selectorTextDisambiguationIndex, propertyName) {
        var specificSelectorBag = getPropertyBag(sheetIndex, selectorText, selectorTextDisambiguationIndex);
        delete specificSelectorBag[propertyName];
    }

    function updatePropertyValue(sheetIndex, selectorText, selectorTextDisambiguationIndex, propertyName, newValue) {
        var currentValue = getCurrentPropertyValue(sheetIndex, selectorText, selectorTextDisambiguationIndex, propertyName);
        var propertyHistory = getPropertyHistory(sheetIndex, selectorText, selectorTextDisambiguationIndex, propertyName);

        //If we didn't just make the property slot and the value we're setting is undefined, delete the property from the source
        if (typeof (currentValue) === typeof (undefined)) {
            if (typeof (newValue) === typeof (undefined) || newValue === "") {
                return "NoOp";
            }
            propertyHistory.push(newValue);
            return "Add";
        } else if (typeof (newValue) === typeof (undefined) || newValue === "") {
            deletePropertyValue(sheetIndex, selectorText, selectorTextDisambiguationIndex, propertyName);
            return "Delete";
        }

        var index = propertyHistory.indexOf(newValue);

        if (index === -1) {
            propertyHistory.push(newValue);
            return "Update";
        }

        if (index === propertyHistory.length - 1) {
            return "NoOp";
        }

        while (propertyHistory.length > index + 1) {
            propertyHistory.pop();
        }

        return "Reset";
    }

    function getPropertyHistory(sheetIndex, selectorText, selectorTextDisambiguationIndex, propertyName) {
        var specificSelectorBag = getPropertyBag(sheetIndex, selectorText, selectorTextDisambiguationIndex);
        return getWithCoalesceAssign(specificSelectorBag, propertyName, []).value();
    }

    function getCurrentPropertyValue(sheetIndex, selectorText, selectorTextDisambiguationIndex, propertyName) {
        var history = getPropertyHistory(sheetIndex, selectorText, selectorTextDisambiguationIndex, propertyName);

        if (history.length === 0) {
            return undefined;
        }

        return history[history.length - 1];
    }

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

    var isBaseline = true;

    function reconcile(source, sheetIndex) {
        var delta = [];

        for (var ambiguousSelectorText in source) {
            if (!source.hasOwnProperty(ambiguousSelectorText)) {
                continue;
            }

            for (var selectorDisambiguationIndex in source[ambiguousSelectorText]) {
                if (!source[ambiguousSelectorText].hasOwnProperty(selectorDisambiguationIndex)) {
                    continue;
                }

                var unusedProperties = {};
                var rawBag = getPropertyBag(sheetIndex, ambiguousSelectorText, selectorDisambiguationIndex);

                for (var propertyName in rawBag) {
                    if (!rawBag.hasOwnProperty(propertyName)) {
                        continue;
                    }

                    unusedProperties[propertyName] = true;
                }

                for (propertyName in source[ambiguousSelectorText][selectorDisambiguationIndex]) {
                    if (!source[ambiguousSelectorText][selectorDisambiguationIndex].hasOwnProperty(propertyName)) {
                        continue;
                    }

                    var newValue = source[ambiguousSelectorText][selectorDisambiguationIndex][propertyName];
                    var action = updatePropertyValue(sheetIndex, ambiguousSelectorText, selectorDisambiguationIndex, propertyName, newValue);
                    unusedProperties[propertyName] = false;

                    if (action != "NoOp") {
                        delta.push({
                            "Url": getSheetHref(document.styleSheets[sheetIndex]),
                            "Rule": ambiguousSelectorText,
                            "RuleIndex": selectorDisambiguationIndex,
                            "Action": action,
                            "Property": propertyName,
                            "NewValue": newValue
                        });
                    }
                }

                for (propertyName in unusedProperties) {
                    if (unusedProperties[propertyName]) {
                        delta.push({
                            "Url": getSheetHref(document.styleSheets[sheetIndex]),
                            "Rule": ambiguousSelectorText,
                            "RuleIndex": selectorDisambiguationIndex,
                            "Action": "Delete",
                            "Property": propertyName
                        });

                        deletePropertyValue(sheetIndex, ambiguousSelectorText, selectorDisambiguationIndex, propertyName);
                    }
                }
            }
        }

        return delta;
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

    var operationId;

    function submitChunkedData(method, data) {
        var chunked = chunk(data);
        for (var i = 0; i < chunked.length; ++i) {
            browserLink.invoke(method, operationId, chunked[i], i, chunked.length);
        }
    }

    function shipUpdate(delta) {
        submitChunkedData("SyncCssSources", delta);
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
            deltaLog = [];
            isBaseline = true;
            performAudit();
        }
    }

    function performAudit() {
        var sheets = document.styleSheets;
        var updateDelta = [];

        for (var i = 0; i < sheets.length; ++i) {
            var record = {};
            var sheet = sheets[i];

            while (deltaLog.length <= i) {
                deltaLog.push({ "href": sheets[deltaLog.length].href });
                updateDelta = [];
                isBaseline = true;
            }

            if (deltaLog[i].href != sheet.href) {
                setTimeout(function () { setPixelPushingModeInternal(isInPixelPusingMode, operationId); }, 0);
                return;
            }

            var rules = sheet.cssRules;
            var nameEncounters = {};

            for (var j = 0; j < rules.length; ++j) {
                var rule = rules[j];
                if (typeof (rule.selectorText) === typeof (undefined)) {
                    continue;
                }

                var selector = standardizeSelector(rule.selectorText);
                nameEncounters[selector] = (typeof (nameEncounters[selector]) === typeof (0) ? nameEncounters[selector] + 1 : 0);
                var style = rule.style || [];
                var selectorRecord = getWithCoalesceAssign(record, selector, {}).value();
                var selectorEncounterRecord = getWithCoalesceAssign(selectorRecord, nameEncounters[selector], {}).value();

                for (var k = 0; k < style.length; ++k) {
                    selectorEncounterRecord[style[k]] = style[style[k]];
                }
            }

            updateDelta = updateDelta.concat(reconcile(record, i));
        }

        if (isInPixelPusingMode) {
            if (!isBaseline && updateDelta.length > 0) {
                setTimeout(function () { shipUpdate(updateDelta); }, 1);
            }

            isBaseline = false;
            setTimeout(performAudit, 250);
        }
    }

    return {
        setPixelPusingMode: setPixelPushingModeInternal
    };
});