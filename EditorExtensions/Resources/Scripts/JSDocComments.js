(function () {
    intellisense.addEventListener("statementcompletionhint", function (event) {
        var itemValue = event.completionItem.value;

        if (typeof itemValue === "function" && !canApplyComments(event.symbolHelp.functionHelp)) {
            return;
        }

        //non functions will only have one documentation type, therefore, no need to check symbolHelp fileds
        var comments = event.completionItem.comments;

        if (!isJSDocComment(comments)) {
            return;
        }

        parseComment(comments, event);
    });

    function canApplyComments(functionHelp) {
        var signatures = functionHelp.signatures;
        var signature = signatures[0];

        if (signatures.length > 1) {
            return false;
        }

        if (signature.description && signature.description.charAt(0) != "*") {
            return false;
        }

        if (!signature.params.every(function (param) { return !param.description; })) {
            return false;
        }

        return true;
    }

    intellisense.addEventListener("signaturehelp", function (event) {
        if (!canApplyComments(event.functionHelp)) {
            return;
        }

        if (!isJSDocComment(event.functionComments.above)) {
            return;
        }

        parseComment(event.functionComments.above, event);
    });

    intellisense.addEventListener("statementcompletion", function (event) {
        var i = 0;
        while (i < event.items.length) {
            var item = event.items[i];
            var isHidden = false;
            var comments = item.comments;

            if (isJSDocComment(comments)) {
                isHidden = parseCommentForCompletionItem(comments, item);
            }

            if (isHidden) {
                event.items.splice(i, 1);
            } else {
                i++;
            }
        }
    });

    function isJSDocComment(comments) {
        return comments.length > 0 && comments.charAt(0) == "*";
    }

    var tagPattern = /^[* ]*@(\w+)[ ]?(.*)$/img;

    function parseCommentForCompletionItem(comment, completionItem) {
        ///<returns type="boolean">Returns true if the cpmpletionItem should be hidden from the 
        ///     completionItem list</returns>
        var obj = {};

        tagPattern.exec(comment).map(function (tag) {
            obj["_" + tag[1]] = true;
        });

        completionItem.glyph = (obj._class && "vs:GlyphGroupClass") ||
            (obj._constructor && "vs:GlyphGroupClass") ||
            (obj._namespace && "vs:GlyphGroupNamespace") ||
            (obj._event && "vs:GlyphGroupEvent") ||
            (obj._enum && "vs:GlyphGroupEnum") ||
            (obj._interface && "vs:GlyphGroupInterface");

        return obj._private || obj._ignore;
    }

    var typePattern = /{([a-zA-Z0-9(),=.]+)}/;
    var namePattern = /\[*(\w+)\]*/;

    function JSDoc(commentStringArr) {
        this.params = [];
        //to prevent ShowPlainComments from overwriting description field for items with JSDoc
        //set the description to " ".
        this.description = " ";
        this.returnType = null;
        this.type = null;
        this._deprecated = false;
        for (var i = 0; i < commentStringArr.length; i++) {
            var description = commentStringArr[i];
            var desArr = description.split(" ");
            this["_" + desArr[0].substr(1)] = true;
            var tagName = desArr[0];
            switch (tagName) {
                case "@description":
                    desArr.splice(0, 1);
                    this.description = desArr.join(" ");
                    break;
                case "@type":
                    this.type = desArr[1];
                    break;
                case "@returns":
                    this.returnType = typePattern.exec(desArr[1])[1];

                    if (this.returnType == "void") {
                        this.returnType = "";
                    }

                    break;
                case "@param":
                    var param = {};
                    var name = namePattern.exec(desArr[2]);
                    param.name = name[1];
                    param.type = typePattern.exec(desArr[1])[1].replace("...", "");

                    if (param.type.charAt(param.type.length - 1) == "=") {
                        param.isOptional = true;
                        param.type = param.type.substr(0, param.type.length - 1);
                    }

                    if (param.type.toLowerCase().indexOf("function") >= 0) {
                        var type = param.type;
                        param.type = "function";
                        param.functionParas = type.slice(type.indexOf("(") + 1, type.indexOf(")")).split(",");
                    }

                    param.isOptional = param.isOptional || name[0].charAt(0) == "[";
                    desArr.splice(0, 3);
                    param.description = desArr.join(" ");
                    this.params.push(param);
                    break;
            }
        }
    }

    var splitAtRegExp = /[\s*]*[\r\n][\s*]*/;

    function processComment(commentString) {
        //replace the first "*" with a "*\r\n"
        commentString = commentString.replace("*", "*\r\n");
        //remove consecutive spaces 
        commentString = commentString.replace(/[ ]+/g, " ");
        //split at each new line [strip leading "*"s and spaces]
        var arr = commentString.split(splitAtRegExp);
        var index = 0;
        while (index < arr.length) {
            if (arr[index].length == 0) {
                arr.splice(index, 1);
                continue;
            }
            if (arr[index].charAt(0) != "@") {
                if (index != 0) {
                    //remainder of a description found. concatinate it with the string above it.
                    arr[index] = arr[index - 1] + " " + arr[index];
                    //remove the previous string from the array.
                    arr.splice(index - 1, 1);
                } else {
                    //inline description found - add "@description" tag
                    arr[index] = "@description " + arr[index];
                }
            } else {
                //new tag found. continue to the next tag
                index++;
            }
        }
        return arr;
    }

    function parseComment(comment, event) {
        var doc = new JSDoc(processComment(comment));
        //if the completion item is a function, then do
        var sig = event.functionHelp.signatures[0] || event.symbolHelp.functionHelp.signatures[0];
        var symHlp = event.symbolHelp;

        if (symHlp) {
            symHlp.description = ((doc._deprecated && "[deprecated]") || "") + (doc.description || "");
        }

        sig.description = ((doc._deprecated && "[deprecated]") || "") + (doc.description || "");

        if (symHlp) {
            symHlp.type = doc.type;
            symHlp.symbolDisplayType = doc.type;
        }

        sig.returnValue = new ReturnValue();
        sig.returnValue.type = doc.returnType;
        sig.returnValue.elementType = doc.returnType;
        for (var i = 0; i < doc.params.length; i++) {
            var param = doc.params[i];
            for (var j = 0; j < sig.params.length; j++) {
                //search for the right parameter to modify
                if (sig.params[j].name == param.name) {
                    //if param type is function, set the params for the function
                    if (param.type.toLowerCase() == "function") {
                        sig.params[j].funcParamSignature = new Signature();
                        sig.params[j].funcParamSignature.params = [];
                        for (var k = 0; k < param.functionParas.length; k++) {
                            var para = new Parameter();
                            para.name = param.functionParas[k];
                            sig.params[j].funcParamSignature.params.push(para);
                        }
                    }
                    sig.params[j].type = param.type;
                    sig.params[j].optional = param.isOptional;
                    sig.params[j].description = param.description;
                    break;
                }
            }
        }
        return doc._private || doc._ignore;
    }
})();
