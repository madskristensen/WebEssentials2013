using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Html.Core;

namespace WebEssentialsTests
{
    [TestClass]
    public class BootstrapValidationTests
    {
        [TestMethod]
        public void PathCompilationTest()
        {
            BootstrapClassValidator validator = new BootstrapClassValidator();

            /* Not sure how to create ElementNode from string */

           // ElementNode node = new ElementNode();
          //  IList<IHtmlValidationError> compiled = validator.ValidateElement(); 
            
            //  int expected = 0;

          //  Assert.AreEqual(expected, compiled.Count);
        }

    }
}
