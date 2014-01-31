using System.Threading.Tasks;
using System.Windows.Threading;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.IntegrationTests.Compilation
{
    [TestClass]
    public class ZenCodingTest
    {
        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task HtmlZenCodingTest()
        {
            var textView = await VSHost.TypeText(".html", "#id.class\t");
            await VSHost.Dispatcher.NextFrame(DispatcherPriority.ApplicationIdle);
            textView.GetText().Should().Be("<div id=\"id\" class=\"class\"></div>");
        }

        [HostType("VS IDE")]
        //[TestMethod, TestCategory("Completion")]
        public async Task AspxZenCodingTest()
        {
            var textView = await VSHost.TypeText(".aspx", "#id.class\t");
            await VSHost.Dispatcher.NextFrame(DispatcherPriority.ApplicationIdle);
            textView.GetText().Should().Be("<div id=\"id\" class=\"class\"></div>");
        }
    }
}