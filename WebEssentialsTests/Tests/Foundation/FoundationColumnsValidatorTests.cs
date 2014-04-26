using System;
using System.Collections.Generic;
using MadsKristensen.EditorExtensions.Html;
using Microsoft.Html.Core;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.Core;

namespace WebEssentialsTests
{
    [TestClass]
    public class FoundationColumnsValidatorTests
    {
        [TestMethod]
        public void NoColumnsNoWarnings()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='someClass'>
                            <i class='someOtherClass'></i>
                        </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void CorrectColumnsUsage()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='row'>
                              <div class='medium-2 columns'>2 columns</div>
                              <div class='medium-10 columns'>10 columns</div>
                           </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void CorrectComplexColumnsUsage()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='row'>
                             <div class='small-8 medium-8 large-6 columns'>8-8-6</div>
                             <div class='small-4 medium-4 large-6 columns'>4-4-6</div>
                           </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void InvalidComplexColumnsUsage_MissingOneLargeDefinition()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='row'>
                             <div class='small-8 medium-8 large-10 columns'>8-8-6</div>
                             <div class='small-4 medium-4 columns'>4-4-6</div>
                           </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 1;

            Assert.AreEqual(expected, compiled.Count);
            Console.WriteLine(compiled[0].Message);
        }

        [TestMethod]
        public void InvalidComplexColumnsUsage_IncorrectMediumSum()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='row'>
                             <div class='small-8 medium-6 large-6 columns'>8-8-6</div>
                             <div class='small-4 medium-4 large-6 columns'>4-4-6</div>
                           </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 1;

            Assert.AreEqual(expected, compiled.Count);
            Console.WriteLine(compiled[0].Message);
        }

        [TestMethod]
        public void RowClassMustBeOnDirectParentElement()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='row'>
                              <div>
                                 <div class='medium-2 columns'>2 columns</div>
                                 <div class='medium-10 columns'>10 columns</div>
                              </div>
                           </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0].Children[0]);

            int expected = 1;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void NoWarningIfNoParentForColumns()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='medium-2 columns'>2 columns</div>
                           <div class='medium-10 columns'>10 columns</div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void UnderTwelveIsOKIfEndClassIsThereAtLastColumn()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='row'>
                              <div class='medium-3 columns'>3 columns</div>
                              <div class='medium-3 columns'>3 columns</div>
                              <div class='medium-3 columns end'>3 columns - End</div>
                           </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void Under12_WithEnd_ButNotAtTheLastColumn_Error()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='row'>
                              <div class='medium-3 columns end'>3 columns</div>
                              <div class='medium-3 columns'>3 columns</div>
                              <div class='medium-3 columns'>3 columns - End</div>
                           </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 1;

            Assert.AreEqual(expected, compiled.Count);
            Assert.IsTrue(compiled[0].Message.Contains("the last column need the 'end' class element"));
        }

        [TestMethod]
        public void ErrorIfUnderTwelveWithoutEndClass()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='row'>
                              <div class='medium-3 columns'>3 columns</div>
                              <div class='medium-3 columns'>3 columns</div>
                              <div class='medium-3 columns'>3 columns - End</div>
                           </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 1;

            Assert.AreEqual(expected, compiled.Count);
            Assert.IsTrue(compiled[0].Message.Contains("the last column need the 'end' class element"));
        }

        [TestMethod]
        public void ErrorMsgIfMoreThanTwelveColumns()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='row'>
                              <div class='medium-8 columns'>8 columns</div>
                              <div class='medium-6 columns'>6 columns</div>
                           </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 1;

            Assert.AreEqual(expected, compiled.Count);
            Assert.IsTrue(compiled[0].Message.Contains("must not exceed 12"));
            Assert.IsTrue(compiled[0].Message.Contains("medium-"));
        }
    }
}
