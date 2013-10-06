using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.Commands.JavaScript
{
    ///<summary>Contains a parsed representation of a .jshintignore file.</summary>
    class JsHintIgnoreList
    {
        //legacy.js
        //somelib/**
        //otherlib/*.js


        public static JsHintIgnoreList Parse(TextReader reader)
        {
            return null;
        }
        public static JsHintIgnoreList Parse(string text) { return Parse(new StringReader(text)); }
        public static JsHintIgnoreList Load(string path) { using (var reader = File.OpenText(path)) return Parse(reader); }
    }
}
