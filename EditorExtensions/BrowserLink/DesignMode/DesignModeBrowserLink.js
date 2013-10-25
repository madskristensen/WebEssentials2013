/// <reference path="../_intellisense/browserlink.intellisense.js" />
/// <reference path="../_intellisense/jquery-1.8.2.js" />

(function (browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    var inspectModeOn;
    var inspectOverlay;
    var current, map, hasChanged,
        selectedClass = "__browserLink_selected_design",
        overlay = "__browserLink_InspectOverlay";

    function getInspectOverlay() {
        if (!inspectOverlay) {

            $("body").append("<div id=\"" + overlay + "\"></div");
            $("body").append(
                "<style>" +
                    "#" + overlay + " {position:fixed; cursor: crosshair; box-sizing: border-box; top: 0; left: 0; bottom: 0; right: 0; background-color: #888; opacity: 0.2; overflow: visible; z-index: 999999999; /*overrde*/ width: auto; height: auto; margin: 0; padding: 0; background-image: none; display:none}" +
                    "." + selectedClass + " {outline: 3px solid lightgreen;}" +
                "</style>");

            inspectOverlay = $("#" + overlay);

            inspectOverlay.mousemove(function (args) {
                inspectOverlay.css("height", "0");

                var target = document.elementFromPoint(args.clientX, args.clientY);

                inspectOverlay.css("height", "auto");

                if (target) {
                    while (target && !browserLink.sourceMapping.canMapToSource(target)) {
                        target = target.parentElement;
                    }

                    if (target && target.innerHTML) {
                        if (current && current !== target) {
                            $(current).removeClass(selectedClass);
                        }
                        
                        map = browserLink.sourceMapping.getCompleteRange(target);

                        if (map && map.sourcePath) {
                            if (isValidFile(map.sourcePath.toLowerCase())) {
                                current = target;
                                $(target).addClass(selectedClass);
                                browserLink.sourceMapping.selectCompleteRange(current);
                            }
                            else {
                                enableDesignMode(false);
                                alert("Design Mode doesn't work for ASP.NET Web Forms");
                            }
                        }
                    }
                }
            });

            inspectOverlay.click(function () {
                if (!current || current.tagName.toUpperCase() === "BODY" || current.tagName.toUpperCase() === document.documentElement.tagName.toUpperCase()) {
                    return;
                }

                inspectOverlay.hide();

                if (current) {
                    current.contentEditable = true;
                    current.focus();
                    $(current).keydown(typing);
                }
            });
        }

        return inspectOverlay;
    }

    function isValidFile(sourcePath) {
        var exts = [".master", ".aspx", ".ascx"];
        var index = sourcePath.lastIndexOf(".");
        var extension = sourcePath.substring(index);

        for (var i = 0; i < exts.length; i++) {
            if (exts[i] === extension)
                return false;
        }

        return true;
    }

    var originalSelectionContent;

    function enableDesignMode(enabled) {
        if (window.getSelection) {
            inspectModeOn = enabled;
            hasChanged = false;

            if (enabled && hasChanged) {
                current.contentEditable = true;
                current.focus();
                $(current).addClass(selectedClass);
                originalSelectionContent = current.innerHTML;
            }
            else if (enabled) {
                getInspectOverlay().show();
            }
            else {
                getInspectOverlay().hide();
                if (current) {
                    current.contentEditable = false;
                    current.blur();
                    $(current).removeClass(selectedClass);
                    browserLink.invoke("Save");
                }
            }
        }
        else {
            alert("The browser doesn't support Design Mode");
        }
    }

    function typing(e) {
        if (inspectModeOn && map && e.keyCode !== 27 && !e.altKey) {

            if (e.keyCode === 89 && e.ctrlKey) { // 89 = y
                browserLink.invoke("Redo");
            }
            else if (e.keyCode === 90 && e.ctrlKey) { // 90 = z
                browserLink.invoke("Undo");
            }
            else if (e.keyCode === 13 && !e.shiftKey) { // 13 = Enter
                e.preventDefault();
            }
            else if (map && map.sourcePath) {
                hasChanged = true;
                setTimeout(function () {
                    //Only fire the update if something has changed
                    if (!current || current.innerHTML != originalSelectionContent) {
                        browserLink.invoke("UpdateSource", current.innerHTML, map.sourcePath, map.startPosition);
                    }
                }, 50);
            }
        }
    }

    $(document).keydown(function (e) {
        if (e.keyCode === 68 && e.ctrlKey && e.altKey) { // 68 = d
            enableDesignMode(true);
        }
        else if (e.keyCode === 27) { // ESC
            enableDesignMode(false);
        }
    });

    window.__weSetDesignMode = function () {
        enableDesignMode(true);
    };


    function AddToMenu() {
        if (!window.browserLink.menu)
            return;

        window.browserLink.menu.addButton("Design", "Use CTRL+ALT+D to enable Design Mode", function () {
            if (!inspectModeOn)
                enableDesignMode(true);
            else
                enableDesignMode(false);
        });
    }

    return {
        setDesignMode: function () {
            enableDesignMode(true);
        },

        onConnected: function () {
            AddToMenu();
        }
    };

});