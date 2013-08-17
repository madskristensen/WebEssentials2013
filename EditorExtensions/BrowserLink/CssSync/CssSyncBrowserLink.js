/// <reference path="../_intellisense/browserlink.intellisense.js" />
/// <reference path="../_intellisense/jquery-1.8.2.js" />

(function (browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    return {
        name: "CssSync", // Has to match the BrowserLinkFactoryName attribute

        refresh: function () {

            $("link[rel=stylesheet]").each(function (i, link) {
                var href = $(link).attr('href');

                if (href.indexOf("://") === -1 && href.indexOf("//") !== 0) {
                    var parts = href.split('?');
                    $(link).attr('href', parts[0] + '?rnd=' + Math.random());
                }
            });
        },

        onInit: function () {
            // Do nothing. This function doesn't need to be implemented.
        }
    };
});