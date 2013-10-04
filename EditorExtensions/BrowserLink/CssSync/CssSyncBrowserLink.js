/// <reference path="../_intellisense/browserlink.intellisense.js" />
/// <reference path="../_intellisense/jquery-1.8.2.js" />

(function (browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    function isMatch(fileName, href) {

        var end = href.indexOf("?");
        var start = href.lastIndexOf("/");

        if (end === -1)
            end = href.length;

        if (end > start && start !== -1) {
            var name = href.substring(start, end);

            return normalize(fileName) === normalize(name);
        }
    }

    function normalize(href) {
        return href.toLowerCase().replace(".min.", ".").replace("/", "");
    }

    function refreshStyle(href, link) {
        if (href.indexOf("://") === -1 && href.indexOf("//") !== 0) {
            var parts = href.split('?');
            $(link).attr('href', parts[0] + '?rnd=' + Math.random());
        }
    }

    return {

        refresh: function (fileName) {

            var found = false;
            var stylesheets = $("link[rel=stylesheet]");

            stylesheets.each(function (i, link) {
                var href = $(link).attr('href');
                var match = isMatch(fileName, href);

                if (match) {
                    refreshStyle(href, link);
                    found = true;
                    // We don't break in case of multiple similar file names
                }
            });

            if (!found) {
                stylesheets.each(function (i, link) {
                    var href = $(link).attr('href');
                    refreshStyle(href, link);
                });
            }
        }

    };
});