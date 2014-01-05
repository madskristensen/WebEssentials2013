using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// This is not compiled (ItemType=None) but is invoked by the Inline Task (http://msdn.microsoft.com/en-us/library/dd722601) in the csproj file.
    /// </summary>
    public class ExtractZipTask : Microsoft.Build.Utilities.Task
    {
        public string ZipPath { get; set; }
        public string ArchivePath { get; set; }
        public string Destination { get; set; }
        public override bool Execute()
        {
            using (var zip = ZipFile.Open(ZipPath, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    if (!entry.FullName.StartsWith(ArchivePath))
                        continue;

                    var targetPath = Path.GetFullPath(Path.Combine(Destination, entry.FullName));
                    if (File.Exists(targetPath))
                        continue;

                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    entry.ExtractToFile(targetPath, overwrite: true);
                }
            }
            return true;
        }
    }
}
