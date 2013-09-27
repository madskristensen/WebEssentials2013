/// <reference path="~/BrowserLink/_intellisense/browserlink.intellisense.js" />

(function (browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    var chunkSize = 20 * 1024; //<-- 20k chunk size

    var validSheetIndexes = [];

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
        return parseSheets;
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
        var sheets = getLinkedStyleSheets();
        return { "RawUsageData": catalog, "Sheets": sheets };
    }

    function captureCssUsageFrameFast() {
        var catalog = [];
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
            currentRecordingOperationId = false;
            finishRecording(false);
        }
    }

    function toggleRecordingMode() {
        browserLink.invoke("ToggleRecordingMode");
    }

    function snapshotCssUsage() {
        browserLink.invoke("SnapshotPage");
    }

    window.__weToggleRecordingMode = function () {
        getLinkedStyleSheets();
        toggleRecordingMode();
        return false;
    };

    window.__weSnapshotCssUsage = function () {
        getLinkedStyleSheets();
        snapshotCssUsage();
        return false;
    };

    $(document).keydown(function (e) {
        if (e.ctrlKey && e.altKey) {
            if (e.keyCode === 82) {// 82 = r
                toggleRecordingMode();
            }
            else if (e.keyCode === 83) { //83 = s
                snapshotCssUsage();
            }
            
            return false;
        }
    });

    var recordingNotificationHeight = 30;
    var recordingNotificationWidth = 157;
    var tailOffset = 28;
    var hiddenLeftPosition = "-" + (recordingNotificationWidth - tailOffset) + "px";
    var recordingNotification = $('<div style="border:black solid 1px;font-weight:500;display:inline-block;padding:0;font-family:verdana,arial;height:' + recordingNotificationHeight + 'px;width:' + recordingNotificationWidth + 'px;position:fixed;top:0;left:' + hiddenLeftPosition + ';z-index:9999;background:none;font-size:16px;cursor:pointer">\
        <div style="opacity:.6;background:white;width:' + recordingNotificationWidth + 'px;height:' + recordingNotificationHeight + 'px;position:absolute"></div>\
		    <div style="position:relative">\
			    <span style="display:inline-block;position:absolute;top:5px;margin-left:5px;text-decoration:none;color:black" title="CTRL+ALT+R">Stop Recording</span>\
			    <div style="border-radius:5px;background:#F00;width:10px;height:10px;display:inline-block;position:absolute;top:' + ((recordingNotificationHeight - 10) / 2) + 'px;left:' + (recordingNotificationWidth - (tailOffset - 10) / 2 - 11) + 'px">&nbsp;</div>\
		    </div>\
        </div>');

    //Return the brower link interop packet
    return {

        startRecording: function (operationId) {
            getLinkedStyleSheets();
            
            finishRecording = function (doContinue) {
                var sheets = getLinkedStyleSheets();
                var result = { "RawUsageData": recordingState, "Continue": !!doContinue, "Sheets": sheets };
                submitChunkedData("FinishedRecording", result, operationId);
            };

            recordingStopRequested = false;
            recordingState = [];
            recordingSelectorLookup = {};
            $("body").append(recordingNotification);
            recordingNotification.bind("mouseover", function () {
                recordingNotification.css("left", 0);
            }).bind("mouseout", function () {
                recordingNotification.css("left", hiddenLeftPosition);
            }).bind("click", __weToggleRecordingMode);
            record(operationId);
        },

        stopRecording: function () {
            recordingStopRequested = true;
            recordingNotification.css("left", hiddenLeftPosition);
            recordingNotification.remove();
        },

        snapshotPage: function (operationId) {
            getLinkedStyleSheets();
            var frame = captureCssUsageFrame();
            submitChunkedData("FinishedSnapshot", frame, operationId);
        },

        installIgnorePatterns: function (patterns) {
            currentPatterns = patterns;
            //var parseSheets = getLinkedStyleSheets();
            //submitChunkedData("ParseSheets", parseSheets, operationId);
        },
        
        blipRecording: function() {
            recordingState = [];
            recordingSelectorLookup = {};
        },

        onConnected: function () { // Optional. Is called when a connection is established
            browserLink.invoke("GetIgnoreList");
        }
    };
});