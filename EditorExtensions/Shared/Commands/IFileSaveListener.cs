using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Commands
{
    ///<summary>A listener called when files are saved in the editor, as well as for compiled files on save or build.</summary>
    public interface IFileSaveListener
    {
        void FileSaved(IContentType contentType, string path, bool forceSave, bool minifyInPlace);
    }
}
