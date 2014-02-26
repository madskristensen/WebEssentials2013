using System.Threading.Tasks;
using FluentAssertions;
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
            var textView = await VSHost.TypeText(".js", "'");
            textView.IsCompletionOpen().Should().BeTrue();
            await VSHost.TypeString("\t");
            textView.GetText().Should().Be("'use strict';");
            textView.IsCompletionOpen().Should().BeFalse();
        }

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task UseAsmFunction()
        {
            var textView = await VSHost.TypeText(".js", "var a=function(){\n\"use a\"");
            textView.GetText().Should().EndWith("\"use asm\"\r\n}");
        }

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task DontActivateElsewhere()
        {
            var textView = await VSHost.TypeText(".js", "var x = {\n'u");
            textView.IsCompletionOpen().Should().BeFalse();
        }

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task BackspaceDismisses()
        {
            var textView = await VSHost.TypeText(".js", "'u");
            textView.IsCompletionOpen().Should().BeTrue();
            await VSHost.TypeString("\b");
            textView.IsCompletionOpen().Should().BeTrue();
            await VSHost.TypeString("\b");
            textView.IsCompletionOpen().Should().BeFalse();
        }

        // TODO: Test JsDoc, require() completion with fixtures, including nested paths

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task GetElementsByTagName()
        {
            var textView = await VSHost.TypeText(".js", "document.body.getElementsByTagName('ta')");
            textView.GetText().Should().Be("document.body.getElementsByTagName('table')");
        }
    }
}
