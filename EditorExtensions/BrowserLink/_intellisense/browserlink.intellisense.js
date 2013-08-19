/// <reference path="jquery-1.8.2.js" />
/// <reference path="jquery-1.8.2.intellisense.js" />

var bl = {

    "call": function () {
        /// <signature>
        ///   <summary>Calls a server method defined in the Visual Studio BrowserLink extension.</summary>
        ///   <param name="serverMethodName" type="String">The name of the server method</param>
        /// </signature>
        /// <signature>
        ///   <summary>Calls a server method defined in the Visual Studio BrowserLink extension and sends data.</summary>
        ///   <param name="serverMethodName" type="String">The name of the server method</param>
        ///   <param name="arg1.." type="Object">An object to send as a parameter of the server method.</param>
        /// </signature>
    },

    "callAsync": function () {
        /// <signature>
        ///   <summary>Calls a server method defined in the Visual Studio BrowserLink extension.</summary>
        ///   <param name="serverMethodName" type="String">The name of the server method</param>
        ///   <returns type="Promise" />
        /// </signature>
        /// <signature>
        ///   <summary>Calls a server method defined in the Visual Studio BrowserLink extension.</summary>
        ///   <param name="serverMethodName" type="String">The name of the server method</param>
        ///   <param name="arg1..." type="Object">An object to send as a parameter of the server method.</param>
        ///   <returns type="Promise" />
        /// </signature>
    },

    /// <field name="json" type="JSON">The isolated BrowserLink JSON object.</field>
    "json": JSON,


    "log": function () {
        /// <signature>
        ///   <summary>Logs a message to the browser console.</summary>
        ///   <param name="message" type="String">The message to log.</param>
        /// </signature>
    },

    /// <field name="sourceMapping" value="sourceMapping">The server-side source mapping data.</field>
    "sourceMapping": {
        "canMapToSource": function () {
            /// <signature>
            ///   <summary>Checks if the specified DOM element can be mapped to server source.</summary>
            ///   <param name="element" type="Element">The DOM element to test against server-side mapping.</param>
            ///   <returns type="Boolean" />
            /// </signature>
        },

        "getCompleteRange": function () {
            /// <signature>
            ///   <summary>Checks if the specified DOM element can be mapped to server source.</summary>
            ///   <param name="element" type="Element">The DOM element to test against server-side mapping.</param>
            ///   <returns type="sourceMap" />
            /// </signature>
        },

        "getElementAtPosition": function () {
            /// <signature>
            ///   <summary>Checks if the specified DOM element can be mapped to server source.</summary>
            ///   <param name="sourcePath" type="String">The absolute path to the source file.</param>
            ///   <param name="position" type="Number">The position in the source file.</param>
            ///   <returns type="Element" />
            /// </signature>
        },

        "getStartTagRange": function () {
            /// <signature>
            ///   <summary>Checks if the specified DOM element can be mapped to server source.</summary>
            ///   <param name="element" type="Element">The DOM element to test against server-side mapping.</param>
            ///   <returns type="sourceMap" />
            /// </signature>
        },

        "selectCompleteRange": function () {
            /// <signature>
            ///   <summary>Checks if the specified DOM element can be mapped to server source.</summary>
            ///   <param name="element" type="Element">The DOM element to test against server-side mapping.</param>
            /// </signature>
        },

        "selectStartTagRange": function () {
            /// <signature>
            ///   <summary>Checks if the specified DOM element can be mapped to server source.</summary>
            ///   <param name="element" type="Element">The DOM element to test against server-side mapping.</param>
            /// </signature>
        },

        /// <field name="initializationData" type="Object">Contains information about the browser and connection.</field>
        "initializationData": {

            /// <field name="appName" type="String">The name of the connected browser.</field>
            "appName": "",

            /// <field name="requestId" type="String">The ID of the SignalR connection to Visual Studio.</field>
            "requestId": "",
        },
    }
};

var sourceMap = function () {
    return {
        /// <field name="sourcePath" type="String">The absolute file path of the file containing the element on disk.</field>
        "sourcePath": "",

        /// <field name="startPosition" type="Number">The start position of the DOM element in the 'sourcePath' file.</field>
        "startPosition": 1,

        /// <field name="length" type="Number">The length of the DOM element in the 'sourcePath' file.</field>
        "length": 1,
    }
};

var Promise = function () {
    return {
        "continueWith": function () {
            /// <signature>
            ///   <summary>Is called when the server replies with a value.</summary>
            ///   <param name="callback" type="function">
            ///     <signature>
            ///        <param name="data" type="Object">The return value from the server.</param>
            ///     </signature>
            ///   </param>
            /// </signature>
        }
    }
}