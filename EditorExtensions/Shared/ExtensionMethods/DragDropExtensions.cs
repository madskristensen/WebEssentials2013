using System.Windows.Forms;
using Microsoft.VisualStudio.Text.Editor.DragDrop;

namespace MadsKristensen.EditorExtensions
{
    public static class DragDropExtensions
    {
        /// <summary>
        /// Get the file path from a drag & drop operation info.
        /// Return null if it was not possibile to retrieve a file path (for instance 
        /// if the data format is not handled).
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static string GetFilePath(this DragDropInfo info)
        {
            var data = new DataObject(info.Data);

            if (info.Data.GetDataPresent("FileDrop"))
            {
                // The drag and drop operation came from the file system
                var files = data.GetFileDropList();

                if (files != null && files.Count == 1)
                    return files[0];
            }
            else if (info.Data.GetDataPresent("CF_VSSTGPROJECTITEMS"))
                return data.GetText(); // The drag and drop operation came from the VS solution explorer
            else if (info.Data.GetDataPresent("MultiURL"))
                return data.GetText();

            return null;
        }
    }
}
