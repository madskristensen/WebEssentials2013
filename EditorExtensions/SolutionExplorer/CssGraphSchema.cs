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
    internal class CssGraphSchema
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
        public static GraphNodeIdName CssStyleSheet = GraphNodeIdName.Get("CssStylesheet", null, typeof(StyleSheet), true);
        public static GraphNodeIdName CssType = GraphNodeIdName.Get("CssType", null, typeof(string), true);
        public static GraphNodeIdName CssParseItem = GraphNodeIdName.Get("CssParseItem", null, typeof(ParseItem), true);
        public static GraphNodeIdName CssRandom = GraphNodeIdName.Get("CssRandom", null, typeof(Guid), true);

        public static GraphSchema Schema = new GraphSchema("CssSchema");

        /// <summary>
        /// Category for values within a Css file
        /// </summary>
        public static GraphCategory CssAtDirectives;
        public static GraphCategory CssAtDirectivesParent;

        public static GraphCategory CssIdSelector;
        public static GraphCategory CssIdSelectorParent;

        public static GraphCategory CssClassSelector;
        public static GraphCategory CssClassSelectorParent;

        /// <summary>
        /// Category for Css files
        /// </summary>
        public static GraphCategory CssFile;

        /// <summary>
        /// Category for links
        /// </summary>
        public static GraphCategory CssValues;


        static CssGraphSchema()
        {
            CssAtDirectives = Schema.Categories.AddNewCategory("CssValue");
            CssFile = Schema.Categories.AddNewCategory("CssFile");
            CssValues = Schema.Categories.AddNewCategory("CssValues");
            CssIdSelector = Schema.Categories.AddNewCategory("CssIdSelector");
            CssAtDirectivesParent = Schema.Categories.AddNewCategory("CssAtDirectivesParent");
            CssIdSelectorParent = Schema.Categories.AddNewCategory("CssIdSelectorParent");
            CssClassSelector = Schema.Categories.AddNewCategory("CssClassSelector");
            CssClassSelectorParent = Schema.Categories.AddNewCategory("CssClassSelectorParent");
        }
    }
}
