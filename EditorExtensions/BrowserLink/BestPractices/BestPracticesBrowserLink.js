/// <reference path="../_intellisense/browserlink.intellisense.js" />
/// <reference path="../_intellisense/jquery-1.8.2.js" />

(function (browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    var file;

    function getFile(element) {
        var range = browserLink.sourceMapping.getCompleteRange(element);

        if (range)
            file = range.sourcePath;
    }

    window.__weReportError = function (key, success) {
        browserLink.invoke("Error", key, success, file);
    };

    return {

        onConnected: function () { // Renamed to 'onConnected' in final version of VS2013

            setTimeout(function () {

                // HTML 5 microdata
                var microdata = $("[itemscope]").length > 0;
                window.__weReportError("microdata", microdata);


                // Description
                var description = $("meta[name=description]").length > 0;

                if (!description && browserLink.sourceMapping) {
                    var head = document.getElementsByTagName("head");

                    if (head.length > 0 && browserLink.sourceMapping.canMapToSource(head[0])) {
                        getFile(head[0]);
                    }
                }

                window.__weReportError("description", description);


                // Viewport
                var viewport = $("meta[name=viewport]").length > 0;
                if (file)
                    window.__weReportError("viewport", viewport);

            }, 500);

        }
    };
});