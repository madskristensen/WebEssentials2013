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
        public void ErrorIfColumnClassIsMissing()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='medium-2'>2 columns</div>
                           <div class='medium-10 columns'>10 columns</div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0]);

            int expected = 1;

            Assert.AreEqual(expected, compiled.Count);
        }

        [TestMethod]
        public void ErrorIfSizeClassIsMissingWhenColumnsClassIsDeclare()
        {
            FoundationColumnsValidator validator = new FoundationColumnsValidator();

            var source = @"<div class='columns'>2 columns</div>
                           <div class='medium-10 columns'>10 columns</div>";

            var tree = new HtmlTree(new TextStream(source));

            tree.Build();

            IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0]);

            int expected = 1;

            Assert.AreEqual(expected, compiled.Count);
        }


        //[TestMethod]
        //public void ComplexCorrectColumnsUsage()
        //{
        //    FoundationColumnsValidator validator = new FoundationColumnsValidator();

        //    var source = @"<div class='row'>
        //                     <div class='col-md-3 col-sm-6 col-xs-11'>               
        //                        <b>Title 1</b>
        //                    </div>
        //                     <div class='col-md-8 col-md-offset-1 col-sm-6 col-xs-1'>               
        //                        <b>Title 2</b>
        //                    </div>
        //                </div>";

        //    var tree = new HtmlTree(new TextStream(source));

        //    tree.Build();

        //    IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

        //    int expected = 0;

        //    Assert.AreEqual(expected, compiled.Count);
        //}

        //[TestMethod]
        //public void MustWorkForNonDivElementsToo()
        //{
        //    FoundationColumnsValidator validator = new FoundationColumnsValidator();

        //    var source = @"<div class='form-group row'>
        //                      <label for='commentcontent' class='control-label col-sm-2'>Comment (no HTML allowed)</label>
        //                      <div class='col-sm-10'>
        //                         <textarea id='commentcontent' class='form-control' rows='4' placeholder='Comment' required></textarea>
        //                      </div>
        //                   </div>";

        //    var tree = new HtmlTree(new TextStream(source));

        //    tree.Build();

        //    IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

        //    int expected = 0;

        //    Assert.AreEqual(expected, compiled.Count);
        //}

        //[TestMethod]
        //public void WarnIfParentsElementIsMissingRowClass()
        //{
        //    FoundationColumnsValidator validator = new FoundationColumnsValidator();

        //    var source = @"<html>
        //                    <body>
        //                    <div class='someClass'>
        //                     <div class='col-md-8'>               
        //                        <b>Title 1</b>
        //                    </div>
        //                     <div class='col-md-4'>               
        //                        <b>Title 2</b>
        //                    </div>
        //                </div>
        //                </body>
        //                </html>";

        //    var tree = new HtmlTree(new TextStream(source));

        //    tree.Build();

        //    IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0].Children[0].Children[0]);

        //    int expected = 1;

        //    Assert.AreEqual(expected, compiled.Count);
        //    Assert.IsTrue(compiled[0].Message.Contains("col-md-8"));
        //}

        //[TestMethod]
        //public void ErrorMsgIfMoreThanTwelveColumns()
        //{
        //    FoundationColumnsValidator validator = new FoundationColumnsValidator();

        //    var source = @"<div class='row'>
        //                     <div class='col-md-10'>               
        //                        <b>Title 1</b>
        //                    </div>
        //                     <div class='col-md-3'>               
        //                        <b>Title 2</b>
        //                    </div>
        //                </div>";

        //    var tree = new HtmlTree(new TextStream(source));

        //    tree.Build();

        //    IList<IHtmlValidationError> compiled = validator.ValidateElement(tree.RootNode.Children[0].Children[0]);

        //    int expected = 1;

        //    Assert.AreEqual(expected, compiled.Count);
        //    Assert.IsTrue(compiled[0].Message.Contains("must not exceed 12"));
        //    Assert.IsTrue(compiled[0].Message.Contains("col-md-*"));
        //}
    }
}
