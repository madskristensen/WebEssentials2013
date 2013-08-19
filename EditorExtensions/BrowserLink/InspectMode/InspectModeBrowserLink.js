/// <reference path="../_intellisense/browserlink.intellisense.js" />
/// <reference path="../_intellisense/jquery-1.8.2.js" />

(function (browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    var inspectModeOn = false;
    var inspectOverlay = null;
    var current;

    function getInspectOverlay() {
        if (inspectOverlay === null) {

            $("body").append("<div id=\"__browserLink_InspectOverlay\"></div");
            $("body").append(
                "<style>" +
                    "#__browserLink_InspectOverlay {position:fixed; cursor: crosshair; box-sizing: border-box; top: 0; left: 0; bottom: 0; right: 0; background-color: #888; opacity: 0.2; overflow: visible; z-index: 9999999999; /*overrde*/ width: auto; height: auto; margin: 0; padding: 0; background-image: none; display:none}" +
                    ".__browserLink_selected {outline: 3px solid lightblue;}" +
                "</style>");

            inspectOverlay = $("#__browserLink_InspectOverlay");

            inspectOverlay.mousemove(function (args) {
                inspectOverlay.css("height", "0");

                var target = document.elementFromPoint(args.clientX, args.clientY);

                inspectOverlay.css("height", "auto");

                if (target) {
                    while (target && !browserLink.sourceMapping.canMapToSource(target)) {
                        target = target.parentElement;
                    }

                    if (target) {
                        if (current && current !== target) {
                            $(current).removeClass("__browserLink_selected");
                        }

                        current = target;
                        $(target).addClass("__browserLink_selected");
                        browserLink.sourceMapping.selectCompleteRange(target);
                    }
                }
            });

            inspectOverlay.click(function () {
                turnOffInspectMode();

                browserLink.call("BringVisualStudioToFront");
            });
        }

        return inspectOverlay;
    }

    function turnOnInspectMode() {
        if (!inspectModeOn) {
            inspectModeOn = true;

            getInspectOverlay().show();
        }
    }

    function turnOffInspectMode() {
        if (inspectModeOn) {
            inspectModeOn = false;

            getInspectOverlay().hide();

            if (current)
                $(current).removeClass("__browserLink_selected");
        }
    }

    $(document).keyup(function (e) {
        if (e.keyCode === 73 && e.ctrlKey && e.altKey) { // 73 = i
            turnOnInspectMode();
        }
        else if (e.which === 27) { // ESC
            turnOffInspectMode();
        }
    });

    window.__we_setInspectMode = turnOnInspectMode;

    return {

        name: "InspectMode", // Has to match the BrowserLinkFactoryName attribute. Not needed in final version of VS2013
        
        setInspectMode: function (inspectModeOn) {
            if (inspectModeOn) {
                turnOnInspectMode();
            }
            else {
                turnOffInspectMode();
            }
        }
    };
});
