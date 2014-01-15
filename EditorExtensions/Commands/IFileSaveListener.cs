using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Commands
{
    ///<summary>A listener called when files are saved in the editor, as well as for compiled files on save or build.</summary>
    public interface IFileSaveListener
    {
        void FileSaved(IContentType contentType, string path);
    }
}
