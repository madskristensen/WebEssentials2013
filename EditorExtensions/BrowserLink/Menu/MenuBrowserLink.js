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
        logo.src = "data:image/png;base64,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";
        logo.title = "Web Essentials";
        logo.className = "logo";

        _menu.appendChild(logo);

        _bar = document.createElement("blbar");
        _menu.appendChild(_bar);

        document.body.appendChild(_menu);
        AddStyles();

        _fixed = _menu.addCheckbox("Auto-hide", "This will auto-hide this menu. Click the SHIFT key to make it visible", true, function () { browserLink.invoke("ToggleVisibility", !this.checked); });
        
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
            this.disabled = false;
            this.style.opacity = 1;
        };

        button.disable = function () {
            this.disabled = true;
            this.style.opacity = ".6";
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
        label.for = id;

        item.checked = function(value) {
            if (typeof(value) === typeof(undefined)) {
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

        item.enable = function() {
            checkbox.removeAttribute("disabled");
            checkbox.removeAttribute("disabled");
            this.style.opacity = 1;
        };

        item.disable = function() {
            checkbox.disabled = "disabled";
            label.disabled = "disabled";
            this.style.opacity = ".6";
        };

        return item;
    }

    function AddStyles() {
        var style = document.createElement("style");
        style.innerHTML =
            "bl {position: fixed; left: 10px; bottom: 10px; opacity: .3; height: 40px; color:black;}" +
            "bl:hover {opacity: .95;}" +
            "bl:hover blbar {display:inline-block;}" +
            "bl .logo {width: 40px; margin-right: 8px; vertical-align:baseline; }" +
            "blbar {background: #d8d8d8; display: none; font:13px arial; position: relative; top: -15px; border-radius: 3px;}" +
            "blbar label {display: inline;}" +
            "blbar blcheckbox {margin: 0 10px;}" +
            "blbar blcheckbox:last-child {border-left: 1px solid gray; padding-left: 7px}" +
            "blbar blcheckbox input {margin: -3px auto auto 3px!important; vertical-align: middle;}" +
            "blbar blbutton:not([disabled]) {cursor: pointer; display: inline-block; margin: 0 10px; padding: 8px 0}" +
            "blbar blbutton:hover {text-decoration: underline;}";

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
            visible = (visible == "true");
            _menu.style.display = visible ? "block" : "none";
            _fixed.checked(!visible);
        },

        onConnected: function () { // Optional. Is called when a connection is established

        }
    };
});