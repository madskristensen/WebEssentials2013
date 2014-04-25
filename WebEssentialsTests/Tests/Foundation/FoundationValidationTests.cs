using System.Collections.Generic;
using MadsKristensen.EditorExtensions.Html;
using Microsoft.Html.Core;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.Core;


namespace WebEssentialsTests
{
    [TestClass]
    public class FoundationValidationTests
    {
        [TestMethod]
        public void ValidColumnDeclaration()
        {
            FoundationClassValidator validator = new FoundationClassValidator();

            var source = @"<div class='small-2 columns'>2 columns</div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void MissingColumnClass()
        {
            FoundationClassValidator validator = new FoundationClassValidator();

            var source = @"<div class='small-2'>2 columns</div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0]);

            int expected = 1;
            string expectedMessagePart = "When using \"small-#\"";

            Assert.AreEqual(expected, compiled.Count);
            Assert.IsTrue(compiled[0].Message.Contains(expectedMessagePart));
            System.Console.WriteLine(compiled[0].Message);
        }

        [TestMethod]
        public void MissingSizeClass()
        {
            FoundationClassValidator validator = new FoundationClassValidator();

            var source = @"<div class='columns'>2 columns</div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0]);

            int expected = 1;
            string expectedMessagePart = "When using \"columns\"";

            Assert.AreEqual(expected, compiled.Count);
            Assert.IsTrue(compiled[0].Message.Contains(expectedMessagePart));
            System.Console.WriteLine(compiled[0].Message);
        }

        [TestMethod]
        public void FoundationClassValidatorDoNothingIfClassAttributIsMissing()
        {
            FoundationClassValidator validator = new FoundationClassValidator();

            var source = @"<div>2 columns</div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void FoundationClassValidatorDoNothingIfClassAttributContainsNothingAboutColumns()
        {
            FoundationClassValidator validator = new FoundationClassValidator();

            var source = @"<div class='somethingElse'>2 columns</div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

    }
}
