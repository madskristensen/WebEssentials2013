/// <reference path="~/BrowserLink/_intellisense/browserlink.intellisense.js" />

(function (browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    var chunkSize = 20 * 1024; //<-- 20k chunk size
    var isRecording = false;

    //List of vendor prefixes from http://stackoverflow.com/a/5411098/1203388
    //var prefixRequiringLeadingDash = ["ms-", "moz-", "o-", "xv-", "atsc-", "wap-", "webkit-", "khtml-", "apple-", "ah-", "hp-", "ro-", "rim-", "tc-"];

    function getReferencedStyleSheets() {
        var sheets = [];
        for (var i = 0; i < document.styleSheets.length; ++i) {
            var sheet = document.styleSheets[i];
            if (sheet.href && sheet.href.length > 0) {
                sheets.push(sheet.href);
            }
            else {
                sheets.push(null);
            }
        }
        return sheets;
    }

    var currentPatterns = [];

    function getLinkedStyleSheets()
    {
        var rxs = [];
        var validSheetIndexes = [];
        for (var p = 0; p < currentPatterns.length; ++p) {
            rxs.push(new RegExp(currentPatterns[p], "i"));
        }

        var sheets = getReferencedStyleSheets();
        var parseSheets = [];
        for (var i = 0; i < sheets.length; ++i) {
            var match = false;
            for (var j = 0; !match && j < currentPatterns.length; ++j) {
                match = !sheets[i] || rxs[j].test(sheets[i]);
            }
            if (!match) {
                validSheetIndexes.push(i);
                parseSheets.push(sheets[i]);
            }
        }
        return { sheets: parseSheets, indexes: validSheetIndexes };
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

    function locationFor(elementOrSelector) {
        var jqElem = $(elementOrSelector);
        var ret = [];
        for (var i = 0; i < jqElem.length; ++i) {
            var target = jqElem[i];
            if (browserLink.sourceMapping.canMapToSource(target)) {
                ret.push(browserLink.sourceMapping.getCompleteRange(target));
            }
            else {
                ret.push(null);
            }
        }
        return ret;
    }

    function getRuleReferenceData(ruleSelector) {
        var locations = [];

        try {
            locations = locationFor($(ruleSelector));
        }
        catch (e) {
        }

        return {
            "Selector": ruleSelector,
            "SourceLocations": locations
        };
    }

    function getRuleReferenceDataFast(ruleSelector) {
        var result = [];
        try {
            if ($(ruleSelector).length > 0) {
                result.push(null);
            }
        }
        catch (e) {
        }
        return {
            "Selector": ruleSelector,
            "SourceLocations": result
        };
    }

    function submitChunkedData(method, data, operationId) {
        var chunked = chunk(data);
        for(var i = 0; i < chunked.length; ++i){
            browserLink.invoke(method, operationId, chunked[i], i, chunked.length);
        }
    }

    function captureCssUsageFrame() {
        var catalog = [];
        var sheets = getLinkedStyleSheets();
        var validSheetIndexes = sheets.indexes;
        sheets = sheets.sheets;

        for (var i = 0; i < validSheetIndexes.length; ++i) {
            var sheet = document.styleSheets[validSheetIndexes[i]];
            var rules = sheet.rules || sheet.cssRules;
            for (var j = 0; j < rules.length; ++j) {
                var result = getRuleReferenceData(rules[j].selectorText);
                if (result.SourceLocations.length > 0) {
                    catalog.push(result);
                }
            }
        }
        return { "RawUsageData": catalog, "Sheets": sheets };
    }

    function captureCssUsageFrameFast() {
        var catalog = [];
        var validSheetIndexes = getLinkedStyleSheets().indexes;

        for (var i = 0; i < validSheetIndexes.length; ++i) {
            var sheet = document.styleSheets[validSheetIndexes[i]];
            var rules = sheet.rules || sheet.cssRules;
            for (var j = 0; j < rules.length; ++j) {
                var result = getRuleReferenceDataFast(rules[j].selectorText);
                if (result.SourceLocations.length > 0) {
                    catalog.push(result);
                }
            }
        }
        return { "RawUsageData": catalog };
    }

    var recordingState = [];
    var recordingSelectorLookup = {};
    var recordingStopRequested = false;
    var finishRecording;
    var currentRecordingOperationId = false;

    function record(operationId) {
        if (currentRecordingOperationId && currentRecordingOperationId !== operationId) {
            return;
        }

        var frameData = captureCssUsageFrameFast();
        currentRecordingOperationId = operationId;
        var dataUpdated = false;

        //Merge
        var rawFrameData = frameData.RawUsageData;
        for(var i = 0; i < rawFrameData.length; ++i){
            var selector = rawFrameData[i].Selector;
            if (!recordingSelectorLookup.hasOwnProperty(selector)) {
                dataUpdated = true;
                recordingState.push(rawFrameData[i]);
                recordingSelectorLookup[selector] = recordingState.length - 1;
            }
        }
        //End Merge

        if (!recordingStopRequested) {
            if (dataUpdated) {
                finishRecording(true);
            }

            setTimeout(function() {
                record(operationId);
            }, 50);
        }
        else {
            isRecording = false;
            currentRecordingOperationId = false;
            finishRecording(false);
            updateMenu();
        }
    }

    function toggleRecordingMode() {
        browserLink.invoke("ToggleRecordingMode");
    }

    function snapshotCssUsage() {
        browserLink.invoke("SnapshotPage");
    }

    window.__weToggleRecordingMode = function () {
        toggleRecordingMode();
        return false;
    };

    window.__weSnapshotCssUsage = function () {
        snapshotCssUsage();
        return false;
    };

    $(document).keydown(function (e) {
        if (e.ctrlKey && e.altKey && e.keyCode === 82) { // 82 = r
                toggleRecordingMode();
                return false;
        }

        return true;
    });

    var recordIconData = "iVBORw0KGgoAAAANSUhEUgAAABEAAAAQCAYAAADwMZRfAAAAZUlEQVQ4y2NgoCb4//8/yRirIQvVeP8Ti/EaQowLiDbk4clj/3elhvzfGuEGpkF8kgwBaVhmLI3ifBAfZhBRhoBsxhYOIHGiDQF5AZshG3ws6OwSqoQJVWKH4nRCcYqlOO8MOAAANAKqWKGMwwoAAAAASUVORK5CYII=";
    var pauseIconData = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAM0lEQVQ4y2NkoBAwDi4D/v//T5wmRkbcBjCGzIWb8n9NMlgeXWzUgEFvAEXpYBjkBXIAAMgUUxEPd8DIAAAAAElFTkSuQmCC";

    function base64ToImage(base64, img, mimeType) {
        var tmp = img || document.createElement("img");
        tmp.src = "data:" + (mimeType || "image/png") + ";base64," + base64;
        return tmp;
    }

    var recordImage = base64ToImage(recordIconData);
    var recordButton;

    function updateMenu() {
        if (!window.browserLink.menu || !recordButton) {
            return;
        }

        recordImage.title = (isRecording ? "Stop Recording" : "Start Recording") + " CSS Usage (CTRL+ALT+R)";
        
        if (isRecording) {
            base64ToImage(pauseIconData, recordImage);
        } else {
            base64ToImage(recordIconData, recordImage);
        }
    }

    function configureMenu() {
        if (!window.browserLink.menu) {
            return;
        }

        recordButton = window.browserLink.menu.addButton("Unused CSS", "Start recording CSS Usage (CTRL+ALT+R)", function () {
            toggleRecordingMode();
        });

        recordButton.appendChild(recordImage);
    }

    //Return the brower link interop packet
    return {

        startRecording: function (operationId) {
            isRecording = true;
            
            finishRecording = function (doContinue) {
                var sheets = getLinkedStyleSheets().sheets;
                var result = { "RawUsageData": recordingState, "Continue": !!doContinue, "Sheets": sheets };
                submitChunkedData("FinishedRecording", result, operationId);
            };

            recordingStopRequested = false;
            recordingState = [];
            recordingSelectorLookup = {};
            record(operationId);
            updateMenu();
        },

        stopRecording: function () {
            recordingStopRequested = true;
        },

        snapshotPage: function (operationId) {
            var frame = captureCssUsageFrame();
            submitChunkedData("FinishedSnapshot", frame, operationId);
        },

        installIgnorePatterns: function (patterns) {
            currentPatterns = patterns;
        },
        
        blipRecording: function() {
            recordingState = [];
            recordingSelectorLookup = {};
        },

        onConnected: function () { // Optional. Is called when a connection is established
            configureMenu();
            browserLink.invoke("GetIgnoreList");
        }
    };
});