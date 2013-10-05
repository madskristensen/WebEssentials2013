using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.SyntaxCheck;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("UnusedCssTagProvider")]
    [Order(After = "Default Declration")]
    internal class UnusedCssTagProvider : ICssItemChecker
    {
        static UnusedCssTagProvider()
        {
            UsageRegistry.UsageDataUpdated += UsageRegistryOnUsageDataUpdated;
        }

        private static readonly ConcurrentDictionary<string, ConcurrentQueue<TaskCompletionSource<bool>>> PendingUpdates = new ConcurrentDictionary<string, ConcurrentQueue<TaskCompletionSource<bool>>>();

        private static Task QueueUpdateTagsOperation()
        {
            var tcs = new TaskCompletionSource<bool>();
            var filePath = ProjectHelpers.GetActiveFilePath();

            if (filePath == null || !File.Exists(filePath))
            {
                return Task.FromResult(false);
            }

            filePath = filePath.ToLowerInvariant();

            var queue = PendingUpdates.GetOrAdd(filePath, x => new ConcurrentQueue<TaskCompletionSource<bool>>());
            queue.Enqueue(tcs);
            UpdateTags(filePath);
            return tcs.Task;
        }

        private static async void UpdateTags(string filePath)
        {
            var queue = PendingUpdates.GetOrAdd(filePath, x => new ConcurrentQueue<TaskCompletionSource<bool>>());
            TaskCompletionSource<bool> last = null;
            TaskCompletionSource<bool> previous = null;

            while (!queue.IsEmpty)
            {
                do
                {
                    await Task.Delay(1);

                    if (previous != null)
                    {
                        previous.SetResult(true);
                    }

                    previous = last;
                }
                while (queue.TryDequeue(out last));

                if (previous == null)
                {
                    return;
                }

                UpdateTagsInternal(filePath);
                previous.SetResult(true);
            }
        }

        private static void UpdateTagsInternal(string filePath)
        {
            var project = ProjectHelpers.GetProject(filePath);

            if (project == null)
            {
                return;
            }

            filePath = StyleSheetHelpers.GetStyleSheetFileForUrl(filePath, project);

            if (filePath == null)
            {
                return;
            }

            var view = WindowHelpers.GetTextViewForFile(filePath);

            if (view == null)
            {
                return;
            }

            var buffer = view.TextBuffer;
            buffer.PostChanged -= BufferOnPostChanged;
            var editorDocument = CssEditorDocument.FromTextBuffer(buffer);
            var document = DocumentFactory.GetDocument(filePath);

            if (document == null)
            {
                return;
            }

            document.Reparse(editorDocument.Tree.TextProvider.Text);

            using (AmbientRuleContext.GetOrCreate())
            {
                CssErrorTagger.FromTextBuffer(buffer).InvalidateErrors();
            }

            buffer.PostChanged += BufferOnPostChanged;
        }

        private static DateTime _lastUsageRegistryUpdate;

        private static async void UsageRegistryOnUsageDataUpdated(object sender, EventArgs eventArgs)
        {
            if (DateTime.Now - _lastUsageRegistryUpdate > TimeSpan.FromSeconds(.5))
            {
                _lastUsageRegistryUpdate = DateTime.Now;
                await QueueUpdateTagsOperation();
            }
        }

        private static async void BufferOnPostChanged(object sender, EventArgs eventArgs)
        {
            await QueueUpdateTagsOperation();
        }

        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            var ruleSet = item as RuleSet;

            if (ruleSet == null)
            {
                return ItemCheckResult.Continue;
            }

            using (AmbientRuleContext.GetOrCreate())
            {
                var allUnusedRules = UsageRegistry.GetAllUnusedRules();
                var matchingRules = allUnusedRules.Where(x => x.Is(ruleSet));

                foreach(var matchingRule in matchingRules)
                {
                    context.AddError(new SelectorErrorTag(matchingRule.Source.Selectors, string.Format("No usages of the CSS rule \"{0}\" have been found.", matchingRule.DisplaySelectorName)));
                }
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[]  { typeof (RuleSet) }; }
        }
    }
}
