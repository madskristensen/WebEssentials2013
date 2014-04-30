using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    public interface IBundleDocument
    {
        string FileName { get; }
        IEnumerable<string> BundleAssets { get; }
    }
}
