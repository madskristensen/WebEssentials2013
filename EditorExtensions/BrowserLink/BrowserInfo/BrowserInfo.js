/// <reference path="../intellisense/browserlink.intellisense.js" />

(function (browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    var name = browserLink.initializationData.appName;

    function SendInfo() {

        var width = window.innerWidth || document.body.clientWidth;
        var height = window.innerHeight || document.body.clientHeight;

        browserLink.invoke("CollectInfo", name, width, height);
    }

    window.addEventListener('resize', function (event) {
        SendInfo();
    });

    return {

        onConnected: function () { // Optional. Is called when a connection is established
            SendInfo();
        }
    };
});