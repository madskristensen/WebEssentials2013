var test = Handlebars.template({
    "1": function (depth0, helpers, partials, data) {
        return "    <h2>Hello</h2>\r\n";
    }, "compiler": [6, ">= 2.0.0-beta.1"], "main": function (depth0, helpers, partials, data) {
        var stack1, helper, functionType = "function", helperMissing = helpers.helperMissing, escapeExpression = this.escapeExpression, buffer = "﻿<div>\r\n    <h1>"
          + escapeExpression(((helper = (helper = helpers.hello || (depth0 != null ? depth0.hello : depth0)) != null ? helper : helperMissing), (typeof helper === functionType ? helper.call(depth0, { "name": "hello", "hash": {}, "data": data }) : helper)))
          + "</h1>\r\n    <div>\r\n        <span>"
          + escapeExpression(((helper = (helper = helpers.world || (depth0 != null ? depth0.world : depth0)) != null ? helper : helperMissing), (typeof helper === functionType ? helper.call(depth0, { "name": "world", "hash": {}, "data": data }) : helper)))
          + "</span>\r\n    </div>\r\n";
        stack1 = helpers['if'].call(depth0, (depth0 != null ? depth0.hello : depth0), { "name": "if", "hash": {}, "fn": this.program(1, data), "inverse": this.noop, "data": data });
        if (stack1 != null) { buffer += stack1; }
        return buffer + "    "
          + escapeExpression(((helpers.customHander || (depth0 && depth0.customHander) || helperMissing).call(depth0, (depth0 != null ? depth0.hello : depth0), { "name": "customHander", "hash": {}, "data": data })))
          + "\r\n</div>\r\n";
    }, "useData": true
});
