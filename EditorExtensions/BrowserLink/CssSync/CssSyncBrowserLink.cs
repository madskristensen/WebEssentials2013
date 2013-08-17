using Microsoft.VisualStudio.Web.BrowserLink;
using System.ComponentModel.Composition;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(BrowserLinkExtensionFactory))]
    [BrowserLinkFactoryName("CssSync")]
    public class CssSyncFactory : BrowserLinkExtensionFactory
    {
        public override BrowserLinkExtension CreateInstance(BrowserLinkConnection connection)
        {
            return new CssSync(connection);
        }

        public override string Script
        {
            get
            {
                using (Stream stream = GetType().Assembly.GetManifestResourceStream("MadsKristensen.EditorExtensions.BrowserLink.CssSync.CssSyncBrowserLink.js"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

    public class CssSync : BrowserLinkExtension
    {
        private BrowserLinkConnection _connection;
        private FileSystemWatcher _fsw;

        public CssSync(BrowserLinkConnection connection)
        {
            _connection = connection;
            Watch();
        }

        private void Watch()
        {
            string path = _connection.Project.Properties.Item("FullPath").Value.ToString();

            if (File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }

            _fsw = new FileSystemWatcher(path, "*.css");
            _fsw.Changed += fsw_Changed;
            _fsw.Created += fsw_Changed;
            _fsw.Deleted += fsw_Changed;
            _fsw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.DirectoryName;
            _fsw.IncludeSubdirectories = true;
            _fsw.EnableRaisingEvents = true;
        }

        void fsw_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.EndsWith(".css"))
            {
                Clients.CallAll("refresh");
            }
        }
    }
}