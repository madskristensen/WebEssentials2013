using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Schema that defines the categories and properties representing Css files.
    /// </summary>
    internal class LessGraphSchema
    {
        /// <summary>
        /// GraphNodeId name for specifying a Css value within a Css file.
        /// </summary>
        /// <remarks>
        /// A complete Css value id would look something like this:
        ///     (Assembly="c:\MyProject\bar.csproj" File="C:\MyProject\resource1.Css" CssValue="Button_Caption")
        /// Where the last portion uses the CssValueName defined here.
        /// </remarks>
        public static GraphNodeIdName CssValueName = GraphNodeIdName.Get("CssValueName", null, typeof(string), true);
        //public static GraphNodeIdName CssStyleSheet = GraphNodeIdName.Get("CssStylesheet", null, typeof(StyleSheet), true);
        //public static GraphNodeIdName CssType = GraphNodeIdName.Get("CssType", null, typeof(string), true);
        //public static GraphNodeIdName CssParseItem = GraphNodeIdName.Get("CssParseItem", null, typeof(ParseItem), true); 

        public static GraphSchema Schema = new GraphSchema("CssSchema");

        public static GraphCategory LessFile;
        
        public static GraphCategory LessMixin;
        public static GraphCategory LessMixinParent;

        public static GraphCategory LessVariable;
        public static GraphCategory LessVariableParent;

        static LessGraphSchema()
        {
            LessFile = Schema.Categories.AddNewCategory("LessFile");
            LessMixin = Schema.Categories.AddNewCategory("LessMixin");
            LessMixinParent = Schema.Categories.AddNewCategory("LessMixinParent");
            LessVariable = Schema.Categories.AddNewCategory("LessVariable");
            LessVariableParent = Schema.Categories.AddNewCategory("LessVariableParent");
        }
    }
}
