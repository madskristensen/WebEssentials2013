/// <reference path="../_intellisense/browserlink.intellisense.js" />
/// <reference path="../_intellisense/jquery-1.8.2.js" />

(function (browserLink, $) {
    /// <param name="browserLink" value="bl" />
    /// <param name="$" value="jQuery" />

    var _menu, _bar, _fixed, _toggle;

    function CreateMenu() {
        _menu = document.createElement("bl");
        _menu.addButton = AddButton;
        _menu.addCheckbox = AddCheckbox;
        _menu.style.display = "none";

        var logo = document.createElement("img");
        logo.src = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAoCAYAAACM/rhtAAAEtklEQVRYw82YW2gcVRjHf3tJSFJ0a9IWxFF8cSBU3xTxhqKCFjrFihWLN3yINyoi9EXUJ0HFlyreWRAfxNY3dXwINYqpFbGtlPqyMtWW6hjbNG3dpDXJ7s4cH/ptcmZyZmd2szV+sLCXmZ3f+X/Xc3IAvmMPAGPADay8hcALluu9BpCXLwPgFP8PC3WWYlcBc5AvdHarCkCpBZaTJsCp5fLlCzAw2D6hUjBXDQlqaolYTUAlgKHm9s5EzHfoVJEPmAfOLCwawHI9RNZwpQJvkY9ZYCYCKHZK5F0ROBWmA05dIMA5oNYSMOq3f5IA/xb/d9N8wAHuAN4BjgB1s4QRT9ZNgNMib7fsCPAwMFYqV/YCzwC3AM8Bu3WVVBiJwROSE0sAZ0TebsGNAOOlcgWAUrmiSuXKBPAucD/whSFBACYjpUt7f65LgBPAY8C3pXJFxX+U72aAs4sKRi47kQRYM3QTBZwGvgH+RI8UY7RzDLgH2FMqVzKXLBVNzeNGQPH7ZOzeMWCjvO4DDraAPBYGPAIcaLq1gxrY0sVxeRXwCfCD5XqzwI+SkTuBRuy+34BHge90t1ZHhqmODA+lNpIwO2BcQSzXw3fsHKAs15sAngJekpgF+F1ibo/lejpcAXgIeDlVvTAiSoShGLv+eML/XA1c7jv2qOV6075jvw4cAp4Htluut09XDVgri3gScFPiVk+Ss9rCsykoNgi8DWzxHTtnuV4IjAIbY3A54BrgY+BpoKfNGFzSLIotYjBua6WGzfqO7Yo7pxdahmMX5qrh3X2l/A7gqrb68CJgNQ4YV3AqZaIZBF4ETIF/RdjgzXbgFmrgIuDpeN+OA87EYyBmfwDbEobbo4Xe3FbJdpUdMPLxpIRPImBN4sBkh4EHgH2S2QXfsS+TDMdyPdbt+mU/sAn4VKaYdgGXhFgccD4BcAJ4XGoivmP3S68dBTb7jp2PJdoTwCvisnYAJ9MAkxT8FThguZ7yHXu1PHyHlJ8PBDInvZZSuTINvCqKT6TGYFZA8b9+UY/v2EjG5n3HtoHPgGeBPrlmjUBu9R27oA0FjVK58hWwPWMfjuzmkhTUV5ETt97mO/YqYAPwJXCr/KbbEPAWcG91ZDgfm17mM7rYuPU1AeqBei3wOfA18FFKCRnMF3kf2CIFO3uZabH1TVJQD4yLgevFlWnW7Dibs0DGirQx/k2Ay91+rgE+BB6UgSFiEgLrgH4USgOcNg3MRcMDmru7wjIgS8AbgKqODO+UBQ8A62Xc3wDYSpHTAM+Ydn8mwG7tj4fE3T3iqU2yaRqMDFcdADb7YX8XIFcD7wlg79ISo+LPbWSJwTl9SumC9ZngEibpMAtgTT+8uTBnHRAGENRV6ixa/K8AlYKwoQhqENQUYUOZFFRZABumltPRUWkAYV3RmFcEdaUfUsYtSBqWiwkXbwN2AXcBNwEWsCpL6QlqikZNEcyfVywBqOmlw7K13Q0c1I88tEPblNMfx75I6teNwM3S/i5NWFwrOydHInvltR84arleI+VUOeMx1flxqle6wHrgdoEeBi4x/Fcd+Av4CRgHvhfAKhCY1FoWYALwKuBK4DrgToGelEF2HPhZPteyAsXtX2B+CejC3iTLAAAAAElFTkSuQmCC";
        logo.title = "Web Essentials";
        logo.className = "logo";

        _menu.appendChild(logo);

        _bar = document.createElement("blbar");
        _menu.appendChild(_bar);

        document.body.appendChild(_menu);
        logo.draggable = true;

        AddStyles();

        _fixed = _menu.addCheckbox("Auto-hide", "This will auto-hide this menu. Click the CTRL key to make it visible", true, function () {
            browserLink.invoke("ToggleVisibility", !this.checked);
            if (this.checked) {
                _menu.style.display = "none";
            }
        });

        return _menu;
    }

    function AddButton(text, tooltip, callback) {
        var button = document.createElement("blbutton");
        button.innerHTML = text;
        button.title = tooltip;
        button.disabled = false;

        button.onclick = function () {
            if (!this.disabled) {
                callback(arguments);
            }
        };

        _bar.insertBefore(button, _fixed);

        button.enable = function () {
            $(this).removeClass("bldisabled");
            this.disabled = false;
        };

        button.disable = function () {
            $(this).addClass("bldisabled");
            this.disabled = true;
        };

        return button;
    }

    function AddCheckbox(text, tooltip, checked, callback) {
        var item = document.createElement("blcheckbox");

        var checkbox = document.createElement("input");
        checkbox.checked = checked;
        checkbox.type = "checkbox";
        checkbox.title = tooltip;
        checkbox.onclick = callback;
        var id = ("_" + Math.random()).replace('.', '_');
        checkbox.id = id;

        var label = document.createElement("label");
        label.innerHTML = text;
        label.title = tooltip;
        label.style.fontWeight = "normal";
        label.htmlFor = id;

        item.checked = function (value) {
            if (typeof (value) === typeof (undefined)) {
                return checkbox.checked;
            }

            return checkbox.checked = value;
        };

        item.appendChild(label);
        label.appendChild(checkbox);

        if (!_fixed) {
            _bar.appendChild(item);
        }
        else {
            _bar.insertBefore(item, _fixed);
        }

        item.enable = function () {
            $(checkbox).removeClass("bldisabled");
            $(label).removeClass("bldisabled");
            checkbox.removeAttribute("disabled");
            checkbox.removeAttribute("disabled");
        };

        item.disable = function () {
            $(checkbox).addClass("bldisabled");
            $(label).addClass("bldisabled");
            checkbox.disabled = "disabled";
            label.disabled = "disabled";
            this.style.opacity = ".6";
        };

        return item;
    }

    function AddStyles() {
        var style = document.createElement("style");
        style.innerHTML =
            "bl {position: fixed; left: 10px; bottom: 5px; opacity: .3; color:black; z-index:2147483638; }" +
            "bl:hover {opacity: .95;}" +
            "bl .logo {width: 40px; margin-right: 8px; vertical-align:baseline; background:white; }" +
            "blbar {background: #d8d8d8; display: inline-block; font:13px arial; position: relative; top: -15px; border-radius: 5px; padding: 4px 3px}" +
            "blbar img { margin: -2px 0 0 2px; }" +
            "blbar label {display: inline; cursor: pointer; font: 13px arial; }" +
            "blbar blcheckbox {margin: 0 4px;}" +
            "blbar blbutton, blbar blcheckbox:not(:last-child) {border-right: 1px solid #b8b6b6; padding-right: 7px}" +
            "blbar input {margin: -3px auto auto 3px!important; vertical-align: middle;}" +
            "blbar blbutton:not([disabled]) {cursor: pointer; display: inline-block; margin: 0 4px; }" +
            "blbar blbutton:hover {text-decoration: underline;}" +
            ".bldisabled {cursor:default !important; opacity:.5}" +
            ".bldisabled:hover {text-decoration:none;}";

        document.body.appendChild(style);
    }

    $(document).keyup(function (e) {
        if (e.keyCode === 17 && _toggle) { // Ctrl
            if (_menu.style.display !== "block")
                _menu.style.display = "block";
            else
                _menu.style.display = "none";
        }

        _toggle = false;
    });

    $(document).keydown(function (e) {
        _toggle = e.keyCode === 17;// Ctrl
    });

    window.browserLink.menu = CreateMenu();

    return {
        setVisibility: function (visible) { // Can be called from the server-side extension
            _menu.style.display = visible ? "block" : "none";
            _fixed.checked(!visible);
        }
    };
});