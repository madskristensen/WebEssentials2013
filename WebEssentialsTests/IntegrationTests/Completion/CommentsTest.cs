using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.IntegrationTests.Compilation
{
    [TestClass]
    public class CommentsTest
    {
        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task BlockCommentCompletion()
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                JavaScript = { BlockCommentCompletion = true }
            });
            var textView = await VSHost.TypeText(".js", "/*");
            textView.GetText().Should().Be("/**/");
        }

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task BlockCommentCompletionDisabled()
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                JavaScript = { BlockCommentCompletion = false }
            });

            var textView = await VSHost.TypeText(".js", "/*");
            textView.GetText().Should().Be("/*");
        }

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task BlockCommentStarCompletion()
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                JavaScript = { BlockCommentCompletion = true }
            });

            var textView = await VSHost.TypeText(".js", "/*\n");
            textView.GetText().Should().Be("/*\r\n * \r\n */");
        }

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task BlockCommentStarCompletionDisabled()
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                JavaScript = { BlockCommentCompletion = false }
            });

            var textView = await VSHost.TypeText(".js", "/*\n");
            textView.GetText().Should().Be("/*\r\n");
        }
    }
}
