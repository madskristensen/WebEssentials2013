using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public interface IResolutionRequiredDataSource
    {
        Task ResolveAsync(UnusedCssExtension extension);
    }
}