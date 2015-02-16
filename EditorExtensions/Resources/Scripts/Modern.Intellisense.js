/*
 * This file contains addional Intellisense for Visual Studio's JavaScript editor
 */

//#region Shadow DOM

var ShadowRoot = _$inherit(DocumentFragment);

ShadowRoot.prototype.applyAuthorStyles = true;
ShadowRoot.prototype.resetStyleInheritance = true;
ShadowRoot.prototype.activeElement = Element.prototype;
ShadowRoot.prototype.innerHTML = "";
ShadowRoot.prototype.styleSheets = StyleSheetList.prototype;

ShadowRoot.prototype.getElementById = document.getElementById;
ShadowRoot.prototype.getElementsByClassName = document.getElementsByClassName;
ShadowRoot.prototype.getElementsByTagName = document.getElementsByTagName;
ShadowRoot.prototype.getElementsByTagNameNS = document.getElementsByTagNameNS;
ShadowRoot.prototype.getSelection = document.getSelection;
ShadowRoot.prototype.elementFromPoint = document.elementFromPoint;

Element.prototype.createShadowRoot = function () {
    /// <summary>The ShadowRoot object represents the shadow root.</summary>
    /// <returns type="ShadowRoot" />
}

Element.prototype.webkitCreateShadowRoot = Element.prototype.createShadowRoot;

//#endregion

//#region Vibration API

Navigator.prototype.vibrate = function () {
    /// <signature>
    ///   <param name="time" type="Number">The number of miliseconds to vibrate.</param>
    ///   <returns type="Boolean" />
    /// </signature>
    /// <signature>
    ///   <param name="pattern" type="Array">An array of miliseconds that makes up the pattern of vibration.</param>
    ///   <returns type="Boolean" />
    /// </signature>
}

//#endregion

//#region Fullscreen API

Element.prototype.requestFullscreen = Element.prototype.msRequestFullscreen;
Element.prototype.mozRequestFullscreen = Element.prototype.msRequestFullscreen;
Element.prototype.webkitRequestFullscreen = Element.prototype.msRequestFullscreen;

document.fullscreenElement = document.msFullscreenElement;
document.mozFullscreenElement = document.msFullscreenElement;
document.webkitFullscreenElement = document.msFullscreenElement;

document.fullscreenEnabled = document.msFullscreenEnabled;
document.mozFullscreenEnabled = document.msFullscreenEnabled;
document.webkitFullscreenEnabled = document.msFullscreenEnabled;

document.exitFullscreen = document.msExitFullscreen;
document.mozExitFullscreen = document.msExitFullscreen;
document.webkitExitFullscreen = document.msExitFullscreen;

//#endregion

//#region Canvas

Element.prototype.getContext = HTMLCanvasElement.prototype.getContext;

//#endregion

//#region Server-Sent Events

function EventSource(url) {
    /// <signature>
    ///   <param name="url" type="String">An absolute URI.</param>
    ///   <returns type="EventSource" />
    /// </signature>
    /// <signature>
    ///   <param name="url" type="String">An absolute URI.</param>
    ///   <param name="eventSourceInitDict" type="dictionary" />
    ///   <returns type="EventSource" />
    /// </signature>

    this.url = url;
    this.withCredentials = false;
    this.readyState = 0;

    this.close = function () { }


    this.onopen = function (event) { }
    this.onerror = function (event) { }
    this.onmessage = function (event) { }
}

EventSource.CONNECTING = 0;
EventSource.OPEN = 1;
EventSource.CLOSED = 2;

//#endregion

//#region HTML Import

Element.prototype.import = Document.prototype;

//#endregion

//#region Object.observe()

Object.observe = function (object, callback) {
    /// <summary>Oberves changes made to an object</summary>
    /// <param name="object" type="object">The object to observe.</param>
    /// <param name="callback" type="function(changes)">The callback function.</param>
};

//#endregion