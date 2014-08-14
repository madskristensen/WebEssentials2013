using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Html
{
    [HtmlCompletionProvider(CompletionType.Values, "*", "class")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class AngularClassCompletion : IHtmlCompletionListProvider, IHtmlTreeVisitor
    {
        private static BitmapFrame _icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/angular.png", UriKind.RelativeOrAbsolute));
        private static List<string> _classes = new List<string>()
        {
            "ng-bind",
            "ng-class",
            "ng-class-even",
            "ng-class-odd",
            "ng-form",
            "ng-hide",
            "ng-include",
            "ng-init",
            "ng-style",
        };

        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }

        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            HashSet<bool> isAngular = new HashSet<bool>();
            context.Document.HtmlEditorTree.RootNode.Accept(this, isAngular);

            if (isAngular.Count == 0)
                return new List<HtmlCompletion>();

            return CreateCompletionItems(context).ToArray();
        }

        private static IEnumerable<HtmlCompletion> CreateCompletionItems(HtmlCompletionContext context)
        {
            foreach (string item in _classes)
            {
                var completion = new SimpleHtmlCompletion(item, context.Session);
                completion.IconSource = _icon;
                yield return completion;
            }
        }

        public bool Visit(ElementNode element, object parameter)
        {
            if (element.Attributes.Any(a => (a.Name.StartsWith("ng-")
                                         || a.Name.StartsWith("data-ng-")
                                         || (a.Name == "class" && a.Value != null && a.Value.StartsWith("ng-")))))
            {
                var list = (HashSet<bool>)parameter;
                list.Add(true);
                return true;
            }

            return true;
        }
    }
}
