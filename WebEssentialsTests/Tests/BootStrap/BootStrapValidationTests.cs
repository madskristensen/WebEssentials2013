using System.Collections.Generic;
using MadsKristensen.EditorExtensions;
using Microsoft.Html.Core;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.Core;


namespace WebEssentialsTests
{
    [TestClass]
    public class BootstrapValidationTests
    {
        [TestMethod]
        public void BootstrapNestedFontAwesomeTest()
        {
            BootstrapClassValidator validator = new BootstrapClassValidator();

            var source = @"<span class='fa-stack fa-lg'>
                            <i class='fa-circle fa-stack-2x fa'></i>
                            <i class='fa fa-twitter fa-stack-1x fa-inverse'></i>
                        </span>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

    }
}
