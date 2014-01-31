using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.IntegrationTests.Compilation
{
    [TestClass]
    public class JsIntelliSenseTest
    {

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task UseStrictEmptyFile()
        {
            var textView = await VSHost.TypeText(".js", "'u\t");
            textView.GetText().Should().Be("'use strict';");
        }
        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task UseAsmFunction()
        {
            var textView = await VSHost.TypeText(".js", "var a=function(){\n\"use a\"");
            textView.GetText().Should().EndWith("\"use asm\"\r\n}");
        }
        // TODO: Test that "use strict" completion doesn't trigger elsewhere, and that deleting the quote closes completion.
        // To do this, we need to check whether the completion window is open.

        // TODO: Test require() completion with fixtures, including nested paths

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task GetElementsByTagName()
        {
            var textView = await VSHost.TypeText(".js", "document.body.getElementsByTagName('ta')");
            textView.GetText().Should().Be("document.body.getElementsByTagName('table')");
        }
    }
}
