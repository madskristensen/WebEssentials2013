using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Html
{
    [HtmlCompletionProvider(CompletionType.GroupAttributes, "*", "*")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class AngularLogoCompletion : IHtmlCompletionListProvider
    {
        private static BitmapFrame _icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/angular.png", UriKind.RelativeOrAbsolute));

        public CompletionType CompletionType
        {
            get { return CompletionType.GroupAttributes; }
        }

        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            return ChangeIcons(context);
        }

        public static IList<HtmlCompletion> ChangeIcons(HtmlCompletionContext context)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                if (context.Session.CompletionSets.Count == 0)
                    return;

                foreach (var item in context.Session.CompletionSets[0].Completions)
                {
                    if (item.DisplayText.StartsWith("ng-") || item.DisplayText.StartsWith("data-ng-"))
                        item.IconSource = _icon;
                }
            }), DispatcherPriority.Normal, null);

            return new List<HtmlCompletion>();
        }
    }

    [HtmlCompletionProvider(CompletionType.Attributes, "*", "*")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class AngularLogo2Completion : IHtmlCompletionListProvider
    {
        public CompletionType CompletionType
        {
            get { return CompletionType.Attributes; }
        }

        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            return AngularLogoCompletion.ChangeIcons(context);
        }
    }
}
