using System.Collections.Generic;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public interface IBundleDocument
    {
        string FileName { get; }
        IEnumerable<string> BundleAssets { get; }

        Task<IBundleDocument> LoadFromFile(string fileName);
    }
}
