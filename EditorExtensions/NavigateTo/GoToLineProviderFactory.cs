using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using System;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(INavigateToItemProviderFactory))]
    internal sealed class GoToLineProviderFactory : INavigateToItemProviderFactory, INavigateToItemDisplayFactory
    {
        public bool TryCreateNavigateToItemProvider(IServiceProvider serviceProvider, out INavigateToItemProvider provider)
        {
            provider = new GoToLineProvider(this);
            return true;
        }

        public INavigateToItemDisplay CreateItemDisplay(NavigateToItem item)
        {
            return item.Tag as INavigateToItemDisplay;
        }
    }
}