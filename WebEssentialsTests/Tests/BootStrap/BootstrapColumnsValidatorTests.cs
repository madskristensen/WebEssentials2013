using System.Collections.Generic;
using MadsKristensen.EditorExtensions.Html;
using Microsoft.Html.Core;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.Core;

namespace WebEssentialsTests
{
    [TestClass]
    public class BootstrapColumnsValidatorTests
    {
        [TestMethod]
        public void NoColumnsNoWarnings()
        {
            BootstrapColumnsValidator validator = new BootstrapColumnsValidator();

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
            BootstrapColumnsValidator validator = new BootstrapColumnsValidator();

            var source = @"<div class='row'>
                             <div class='col-md-8'>               
                                <b>Title 1</b>
                            </div>
                             <div class='col-md-4'>               
                                <b>Title 2</b>
                            </div>
                        </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void CorrectColumnsUsageWithRowClassOnTheParentParent()
        {
            BootstrapColumnsValidator validator = new BootstrapColumnsValidator();

            var source = @"<div class='row'>
                             <div>
                               <div class='col-md-8'>               
                                 <b>Title 1</b>
                               </div>
                               <div class='col-md-4'>               
                                 <b>Title 2</b>
                               </div>
                             </div>
                           </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0].Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void NoWarningIfNoParentForColumns()
        {
            BootstrapColumnsValidator validator = new BootstrapColumnsValidator();

            var source = @"<div class='col-md-8'>               
                                <b>Title 1</b>
                            </div>
                             <div class='col-md-4'>               
                                <b>Title 2</b>
                            </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }


        [TestMethod]
        public void ComplexCorrectColumnsUsage()
        {
            BootstrapColumnsValidator validator = new BootstrapColumnsValidator();

            var source = @"<div class='row'>
                             <div class='col-md-3 col-sm-6 col-xs-11'>               
                                <b>Title 1</b>
                            </div>
                             <div class='col-md-8 col-md-offset-1 col-sm-6 col-xs-1'>               
                                <b>Title 2</b>
                            </div>
                        </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void WarnIfParentsElementIsMissingRowClass()
        {
            BootstrapColumnsValidator validator = new BootstrapColumnsValidator();

            var source = @"<html>
                            <body>
                            <div class='someClass'>
                             <div class='col-md-8'>               
                                <b>Title 1</b>
                            </div>
                             <div class='col-md-4'>               
                                <b>Title 2</b>
                            </div>
                        </div>
                        </body>
                        </html>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0].Children[0].Children[0]);

            int expected = 1;

            Assert.AreEqual(expected, compiled.Count);
            Assert.IsTrue(compiled[0].Message.Contains("col-md-8"));
        }

        [TestMethod]
        public void TotalOfGridColumnsMustEqual12()
        {
            BootstrapColumnsValidator validator = new BootstrapColumnsValidator();

            var source = @"<div class='row'>
                             <div class='col-md-8'>               
                                <b>Title 1</b>
                            </div>
                             <div class='col-md-3'>               
                                <b>Title 2</b>
                            </div>
                        </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 1;

            Assert.AreEqual(expected, compiled.Count);
            Assert.IsTrue(compiled[0].Message.Contains("must equal 12"));
            Assert.IsTrue(compiled[0].Message.Contains("col-md-*"));
        }

        [TestMethod]
        public void TotalOfGridColumnsMustEqual12AndHandleOffSetCorrectly()
        {
            BootstrapColumnsValidator validator = new BootstrapColumnsValidator();

            var source = @"<div class='row'>
                             <div class='col-md-7'>               
                                <b>Title 1</b>
                            </div>
                             <div class='col-md-offset-1 col-md-4'>               
                                <b>Title 2</b>
                            </div>
                        </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void PullAndPushMustBeIgnored()
        {
            BootstrapColumnsValidator validator = new BootstrapColumnsValidator();

            var source = @"<div class='row'>
                             <div class='col-sm-6 col-md-9 col-md-push-3'>               
                                <b>Title 1</b>
                            </div>
                             <div class='col-md-3 col-md-pull-9 col-sm-6'>               
                                <b>Title 2</b>
                            </div>
                        </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 0;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void TotalOfGridColumnsMustEqual12ForAllTypeOfColumns()
        {
            BootstrapColumnsValidator validator = new BootstrapColumnsValidator();

            var source = @"<div class='row'>
                             <div class='col-md-8 col-sm-8'>               
                                <b>Title 1</b>
                            </div>
                             <div class='col-md-4 col-sm-6'>               
                                <b>Title 2</b>
                            </div>
                        </div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

            int expected = 1;

            Assert.AreEqual(expected, compiled.Count);
            Assert.IsTrue(compiled[0].Message.Contains("must equal 12"));
        }
    }
}
