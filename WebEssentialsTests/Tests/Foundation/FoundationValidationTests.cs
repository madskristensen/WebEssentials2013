using System.Collections.Generic;
using MadsKristensen.EditorExtensions.Html;
using Microsoft.Html.Core;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.Core;

namespace WebEssentialsTests
{
    [TestClass]
    public class FoundationClassValidatorTests
    {
        [TestClass]
        public class ColumnPairElementsOkTests
        {
            [TestMethod]
            public void BothElements_OK()
            {
                var input = @"small-6 columns";

                var result = FoundationClassValidator.ColumnPairElementsOk(input);

                Assert.IsTrue(result);
            }

            [TestMethod]
            public void BothElementsMissing_OK()
            {
                var input = @"clearfix no-border";

                var result = FoundationClassValidator.ColumnPairElementsOk(input);

                Assert.IsTrue(result);
            }

            [TestMethod]
            public void SizeButMissingColumnClass_Error()
            {
                var input = @"small-4";

                var result = FoundationClassValidator.ColumnPairElementsOk(input);

                Assert.IsFalse(result);
            }

            [TestMethod]
            public void ColumnClassButMissingSize_Error()
            {
                var input = @"columns highlight";

                var result = FoundationClassValidator.ColumnPairElementsOk(input);

                Assert.IsFalse(result);
            }
            [TestMethod]
            public void NoWarningForFoundationBlockGridClass()
            {
                var input = @"small-block-grid-3";

                var result = FoundationClassValidator.ColumnPairElementsOk(input);

                Assert.IsTrue(result);
            }
        }

        [TestClass]
        public class ValidateElementTests
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
}
