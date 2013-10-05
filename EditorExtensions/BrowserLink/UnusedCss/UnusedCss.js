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
                return false;
            }
        }

        return true;
    });

    var recordIconData = "iVBORw0KGgoAAAANSUhEUgAAABoAAAAZCAYAAAAv3j5gAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAadEVYdFNvZnR3YXJlAFBhaW50Lk5FVCB2My41LjExR/NCNwAAA8ZJREFUSEu9lm1MU3cUxtG5+GHzJXPTqRiXLfE9MaIxspkxTXTLsmVDMTqz+GHYiUZnAoWZ4TIlmcZIzIxFnG4zgCCkDQIKTGRSuiJoeScGWwVKQQF5H7392Mfz/CuGqWRcQtbk+XJ7c3//c85zzvkH8efz+SaJpopmixaK3pkABYtmiF4RKQ5Bk0XzfZq2VRsaivMO9McP9feNW97BwXjN690v33xf9NpI0BRRiHdwILnX3dTYfqfM3WorFt3QL/tNd0dDdctgV0eZwPbKd996HrRuoPORpanoqvdWgtFvjY70l8bt8du+jxqzSmMN/lKjwV+TnOgnTCI7JN+d8wKoz+O2NKQka4Vff4rrkVvw9+GDKDtqDOhIDOzUT9EBHaFi1PPh/61GA/J3bAZhjEzSOAqotcVSf9GkFRki4PjlZ7hv2dBWUwmPowItdise3CiAqyAXzoIcuP7MQ1NJEVor7GiruoO26ko4868oGCNjGv8DlKQV79uJ+rQL6G71oLezC50uJ5qtxfLsPCrPnIDj9DFUnzuFxiuZCtLt8aDvcbeCMTKmUReoq7lZQXjSihM/osiwDVcjNiBvSxjyv/oYJTG7UfPbGbjL7QpGKNOoD5T6Kx421Kt0lSXEIvfL9cgKfQ8ZIfOQvmouLq9ZAPNHy3H9m3AFI4Rp1B1R3cWzaLbdRNXZRBTs/AQZq4ORumQmUhZPf6a05bMUjJExjawZDaILVPu7Cc5r2bD9cADZm1chbcWbSFk07d8SGA9wbfsmOdBJuArzlBt1gxpzslASHSmnXoa0ZW+8FJS+8m3kfB6K24lH1cFofd2ge7lmWGO/hWXjCknTKCCpV174h6odnPk54wD9kYT7YoTy4/HI/eIDXFo5J1CbESDWLDP0XXFjhDIPjaO7RnUp5+CpvI275kv467tdMIctVTBGxjSyZqwP3Ujrs8/Y1OPoo/PSQy5pQgdqxYGEMTKmkTWjQehGWp99xn7jBNHfR08nQ0/7QwVjZEwja0aD0I20PtNFCCcIx5UOkEkrjtqhzNDhvKdgjIxpZM1oELqRDmOfsak5QfgeZyMHMaf+mECFuz6DNW6PgjEyppE1o0H4bFhsapog8M4F5TxOfa4Y7rNRQf3tHkujOVUi2g7CGBnTOFbRfVwx3GdcnrKpXwpa+09vT/qjWkc3YYwsoCQdMmncZ1ye3NRyLYh5HsQLxCJZvUbCGBnTOC7J8uSm5rVA7iDh8t2ZI0G8Bb1OmGitaN0EKEQ0X/Sq6BloGMbImMaJEm9X6vv/0y8o6AlLYteIrSQOsAAAAABJRU5ErkJggg==";
    var pauseIconData = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAZCAYAAAArK+5dAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAadEVYdFNvZnR3YXJlAFBhaW50Lk5FVCB2My41LjExR/NCNwAAAgtJREFUSEvtlU9IVGEUxadcJAjaELhp3yZcuGrTpmW0lECiP2QYDi3bBC7DjauKKJBmrKiWFYGYhlpZQUglpe8tIkscsJls3p+ZN6set/v7ulEuNLMWEXPhcLn3nnPP9x6872X+j6jX638dq8KaWxTbFG2K7CaADj17bLMFDRvuTpJ6d7WW9EXVWi6KvyGuJjnt53TuMvX3GTz46NDbHttsQUPRBulTEI94i2XvqVf0H88t+U/mi/7rhZJfqkR+LUlcpqbPHB58dGbCk9hmCzPIchLIl0fffDl56VF65Pxk2nPxYTp451U6++5jGsY1l6npM4cHHx169qxpwONyIkQ7e29J6+Fr0n78hnQNjsvE7KKshLHL1PSZw4OPDv36BvpOeWxOhjjTdUWau4dl/9lRGX/5QVaC2GVq+szhwUeHfmMG537TQPkNg4bBD4NpJR67MJVmj16XpoN5aTl0VQ4M3F9lQE2fOTz46H5pEOuH8uLtst9/83m6t/+edJ6+LXvO3JVTQ9PyzCtKJaq6TE2fOTz46NCvZ7Bd75oTS6VgRr/WYHjCC4fG5sL8g/lwZGYhfL9cCfWSc5maPnN48NGhZ89aBi2KfbpkoFyJ88VyUFBRgVz6HBX0GijoZeYy9c9z+OjQ2x7bbEFD0aTYodil6NgE0KFnj222oGHgZ7H1D+B+NqAR/0pkMl8BSKGNnGSwFa8AAAAASUVORK5CYII=";

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

        recordButton = window.browserLink.menu.addButton("", "", function () {
            toggleRecordingMode();
        });

        recordButton.appendChild(recordImage);
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