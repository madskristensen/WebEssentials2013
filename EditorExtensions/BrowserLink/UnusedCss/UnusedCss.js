/// <reference path="../intellisense/browserlink.intellisense.js" />

(function (browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    var chunkSize = 20 * 1024; //<-- 20k chunk size

    var demandXPathRecalculation = false;
    
    var anyXPathUpdated = false;

    var firstXPathCalculation = true;

    var validSheetIndexes = [];

    //List of vendor prefixes from http://stackoverflow.com/a/5411098/1203388
    //var prefixRequiringLeadingDash = ["ms-", "moz-", "o-", "xv-", "atsc-", "wap-", "webkit-", "khtml-", "apple-", "ah-", "hp-", "ro-", "rim-", "tc-"];

    function getReferencedStyleSheets() {
        return $("link[rel='stylesheet'][type='text/css']");
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

    function computeXPath(element) {
        anyXPathUpdated = true;
        var currentElement = element;
        var currentPath = "";
        //Skip the document element
        while (currentElement.length > 0 && currentElement[0].nodeType != 9) {
            var tagName = currentElement[0].tagName;
            var parent = currentElement.parent();
            var siblingsAndSelf = parent.find(tagName).filter(function () {
                return $(this).parent()[0] == parent[0];
            });
            var myIndex = Array.prototype.indexOf.apply(siblingsAndSelf, currentElement);
            var pathPart = "/" + tagName;
            if (siblingsAndSelf.length != 1) {
                pathPart += "[" + (myIndex + 1) + "]";
            }
            currentPath = pathPart + currentPath;
            currentElement = parent;
        }
        return currentPath.toLowerCase();
    }

    function getXPath(element) {
        return (!demandXPathRecalculation && (element.getAttribute("data-xpath") || "").length != 0) || (element.setAttribute("data-xpath", computeXPath(element)));
    }

    function xpathFor(elementOrSelector) {
        var element = $(elementOrSelector);
        var xpaths = [];
        for (var i = 0; i < element.length; ++i) {
            xpaths.push(getXPath(element[i]));
        }
        return xpaths;
    }

    function getRuleReferenceData(ruleSelector) {
        //Create xpaths for the whole document
        xpathFor("*");
        if (anyXPathUpdated && !firstXPathCalculation) {
            demandXPathRecalculation = true;
            xpathFor("*");
        }

        anyXPathUpdated = false;
        demandXPathRecalculation = false;

        var xpaths = xpathFor($(ruleSelector));
        return {
            "Selector": ruleSelector,
            "ReferencingXPaths": xpaths
        };
    }

    function getRuleReferenceDataFast(ruleSelector) {
        var result = [];
        if ($(ruleSelector).length > 0) {
            result.push("//invalid");
        }
        return {
            "Selector": ruleSelector,
            "ReferencingXPaths": result
        };
    }

    function submitChunkedData(method, data, operationId) {
        var chunked = chunk(data);
        for(var i = 0; i < chunked.length; ++i){
            browserLink.Call(method, document.location.href, operationId, chunked[i], i, chunked.length);
        }
    }

    function captureCssUsageFrame() {
        var catalog = [];
        for (var i = 0; i < validSheetIndexes.length; ++i) {
            var sheet = document.styleSheets[validSheetIndexes[i]];
            var rules = sheet.rules || sheet.cssRules;
            for (var j = 0; j < rules.length; ++j) {
                var result = getRuleReferenceData(rules[j].selectorText);
                if (result.ReferencingXPaths.length > 0) {
                    catalog.push(result);
                }
            }
        }
        return { "RawUsageData": catalog };
    }

    function captureCssUsageFrameFast() {
        var catalog = [];
        for (var i = 0; i < validSheetIndexes.length; ++i) {
            var sheet = document.styleSheets[validSheetIndexes[i]];
            var rules = sheet.rules || sheet.cssRules;
            for (var j = 0; j < rules.length; ++j) {
                var result = getRuleReferenceDataFast(rules[j].selectorText);
                if (result.ReferencingXPaths.length > 0) {
                    catalog.push(result);
                }
            }
        }
        return { "RawUsageData": catalog };
    }

    var recordingState = [];
    var recordingSelectorLookup = {};
    var recordingStopRequested = false;

    function record(operationId) {
        var frameData = captureCssUsageFrameFast();

        //Merge
        var rawFrameData = frameData.RawUsageData;
        for(var i = 0; i < rawFrameData.length; ++i){
            var selector = rawFrameData[i].Selector;
            if (!recordingSelectorLookup.hasOwnProperty(selector)) {
                recordingState.push(rawFrameData[i]);
                recordingSelectorLookup[selector] = recordingState.length - 1;
            }
            //<!------- NOTE: Since we're using the "Fast" variation for record, there is no real usage data, uncomment if switching back from "Fast" variation and wanting full data ----------->
            //else {
            //    var index = recordingSelectorLookup[selector];
            //    for(var j = 0; j < rawFrameData[i].ReferencingXPaths.length; ++j){
            //        if(!recordingState[index].ReferencingXPaths.contains(rawFrameData[i].ReferencingXPaths[j])){
            //            recordingState[index].ReferencingXPaths.push(rawFrameData[i].ReferencingXPaths[j]);
            //        }
            //    }
            //}
        }
        //End Merge

        if (!recordingStopRequested) {
            setTimeout(function() {
                record(operationId);
            }, 200);
        }
        else {
            var result = { "RawUsageData": recordingState };
            submitChunkedData("FinishedRecording", result, operationId);
        }
    }

    //Return the brower link interop packet
    return {

        name: "MadsKristensen.EditorExtensions.BrowserLink.UnusedCss",

        startRecording: function (operationId) {
            recordingStopRequested = false;
            recordingState = [];
            recordingSelectorLookup = {};
            record(operationId);
        },

        stopRecording: function () {
            recordingStopRequested = true;
        },

        snapshotPage: function (operationId) {
            var frame = captureCssUsageFrame();
            submitChunkedData("FinishedSnapshot", frame, operationId);
        },

        getLinkedStyleSheetUrls: function (patterns, operationId) {
            var rxs = [];
            for(var p = 0; p < patterns.length; ++p) {
                rxs = new RegExp(patterns[p]);
            }

            var sheets = getReferencedStyleSheets();
            var parseSheets = [];
            for (var i = 0; i < sheets.length; ++i) {
                for (var j = 0; j < patterns.length; ++j) {
                    if (!rxs[j].test(sheets[i].getAttribute("href"))) {
                        validSheetIndexes.push(i);
                        parseSheets.push(sheets[i].getAttribute("href"));
                        break;
                    }
                }
            }

            submitChunkedData("ParseSheets", parseSheets, operationId);
        },

        onInit: function () { // Optional. Is called when a connection is established
            browserLink.call("GetIgnoreList");
        }
    };
});