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