using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(INavigateToItemProviderFactory))]
    internal sealed class CssGoToLineProviderFactory : INavigateToItemProviderFactory, INavigateToItemDisplayFactory
    {
        public bool TryCreateNavigateToItemProvider(IServiceProvider serviceProvider, out INavigateToItemProvider provider)
        {
            provider = new CssGoToLineProvider(this);
            return true;
        }

        public INavigateToItemDisplay CreateItemDisplay(NavigateToItem item)
        {
            return item.Tag as INavigateToItemDisplay;
        }
    }
}