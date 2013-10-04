/// <reference path="~/BrowserLink/_intellisense/browserlink.intellisense.js" />

(function (browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    var chunkSize = 20 * 1024; //<-- 20k chunk size
    var isRecording = false;
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

    var recordIconData = "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";
    var cameraIconData = "iVBORw0KGgoAAAANSUhEUgAAABkAAAAZCAYAAADE6YVjAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAadEVYdFNvZnR3YXJlAFBhaW50Lk5FVCB2My41LjExR/NCNwAAAtBJREFUSEu1ldtP1EAUxtc3fOBP4sVX/QN840VCd9tut9u9X7rdUhZMSICYmIASJAQFIqLxEqORGGMM4Y2oyIM3iEYTMZIAsruIn+dMqe5CVSK1yS/dnpn5vjlzZmZDAP47vsGg8Q0GjW8waJqeuKGdiGryRqd0BowU7kAyFX9k26WWvS7//kS1yLEux2qL6eoCmSCRjMEg+M1GktSRI9P2w0Ia7TSxtqmpyePCgIRbFTV8KyJ3QovJiDJk1AjHtZjyBw62u+OUh7IitYbI4JxuRGE7JspdReQKKSRSbhaJlE6xgmjr6i75YIo2D/5ubOfxqbRxO6RpSr1Yyv7syMu09OEkVpZPYfLCaWHMA5yK1cTfTUyUynnISidCtIbig4M8OJtPwq5IcBwJ+awsBnT3lNHdW0al1xa/PSPOMpWJw0hoSKbjsOxC0wRYl/UbTFjMEngijDAg2MA1ceP5YhqyKmFg6DxGZ6/v9g1f3FI1BdlcUmhxZgdMnMrejPezl0FjFhb1j6hhzC0+xVptG+++bmPhcxXj80tQYyrVlVaDTEwrt9/EnbUn6IdnYiQ1DE7f+Lb4pYrVrW283KjiyacaZlbrKA6PQzfUo5i4S6XHVfTN3t+d+1jD/FoNj8ngzvs6xl7XYU3erXKxj5xJnLa8c/nm94k3dVyl2U+v1DH6qo7+FztID13Z9I6Ej4lbEz9xD8+E+4epJmcfPMfg8o4Q73m2A/PeMmQqPm+KJhO+NnjQr93lU/wG2IT7ZnIJKn4E8f7hTX1oqhobGKlGojLSGQM2bd+yUxS6wkSNht8WzIxw5sGcEQv9Dm972iTCh5hPNdeIbwg3g6I4wPw2rTzUaGQjlEwZhkL73SzlxGE6DDxDhpdjP6UyQ+3CIMx3mBG6NjvTQle8To7rvHSc3lFhHboT1+mC1C+NjbT4/skEjW8waHyDQeMbDBrfYLAg9AN6wUUO7rHxXAAAAABJRU5ErkJggg==";

    function base64ToImage(base64, mimeType) {
        var tmp = document.createElement("img");
        tmp.src = "data:" + (mimeType || "image/png") + ";base64," + base64;
        return tmp;
    }

    var recordImage = base64ToImage(recordIconData);
    var snapshotImage = base64ToImage(cameraIconData);
    snapshotImage.title = "Snapshot CSS Usage (CTRL+ALT+S)";
    var recordButton, snapshotButton;

    function updateMenu() {
        if (!window.browserLink.menu || !recordButton || !snapshotButton) {
            return;
        }

        recordImage.title = (isRecording ? "Stop Recording" : "Start Recording") + " CSS Usage (CTRL+ALT+R)";
        
        if (isRecording) {
            recordImage.style.opacity = 1;
            snapshotButton.disable();
        } else {
            recordImage.style.opacity = .7;
            snapshotButton.enable();
        }
    }

    function configureMenu() {
        if (!window.browserLink.menu) {
            return;
        }

        recordButton = window.browserLink.menu.addButton("", "", function () {
            toggleRecordingMode();
        });

        recordButton.appendChild(recordImage);

        snapshotButton = window.browserLink.menu.addButton("", "", function() {
            snapshotCssUsage();
        });

        snapshotButton.appendChild(snapshotImage);
    }

    //Return the brower link interop packet
    return {

        startRecording: function (operationId) {
            isRecording = true;
            getLinkedStyleSheets();
            
            finishRecording = function (doContinue) {
                var sheets = getLinkedStyleSheets();
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
            configureMenu();
            browserLink.invoke("GetIgnoreList");
        }
    };
});