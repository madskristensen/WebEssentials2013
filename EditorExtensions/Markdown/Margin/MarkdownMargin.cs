using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Text;
using mshtml;

namespace MadsKristensen.EditorExtensions.Markdown
{
    internal class MarkdownMargin : CompilingMarginBase
    {
        private WebBrowser _browser;

        private const string _htmlTemplate = "WE-Markdown.html";
        private const string _stylesheet = "WE-Markdown.css";
        private readonly string _globalStylesheetPath;
        private double _cachedPosition = 0,
                       _cachedHeight = 0,
                       _positionPercentage = 0;

        public MarkdownMargin(ITextDocument document)
            : base(WESettings.Instance.Markdown, document)
        {
            _globalStylesheetPath = WESettings.Instance.Markdown.GlobalPreviewCSSFile;
        }

        /// <summary>
        /// Combine a relative filename with a list of folders and return each one of them in order.
        /// </summary>
        /// <param name="relativePath">The relative path to combine to each item in the folders parameter</param>
        /// <param name="folders">
        /// The folders where to look for the file. If an item in this array is 
        /// null, it will be skipped.
        /// </param>
        /// <returns></returns>
        public static IEnumerable<string> ResolvePaths(string relativePath, params string[] folders)
        {
            foreach (var folder in folders)
            {
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    yield return Path.GetFullPath(Path.Combine(folder, relativePath));
                }
            }
        }

        /// <summary>
        /// Probe a list of folder to check if a file exist 
        /// and if it does yield its full path.
        /// </summary>
        /// <param name="fileName">The file name that will be combined with each folder</param>
        /// <param name="folders">
        /// The folders where to look for the file. If an item in this array is 
        /// null, it will be skipped.
        /// </param>
        /// <returns>
        /// A sequence containig at most (folders.Length) elements. 
        /// Each item will be the path of an existing file. The order in witch the items 
        /// are returned are the same in witch they are discovered.
        /// </returns>
        public static IEnumerable<string> GetExistingFilesInFolders(string fileName, params string[] folders)
        {
            return ResolvePaths(fileName, folders).Where(File.Exists);
        }

        /// <summary>
        /// Convert a path(relative or absolute) into an url.
        /// Eg. C:\Test\File\Path will be converted to file:///C:/Test/File/Path/
        /// </summary>
        /// <param name="absoluteOrRelativePath"></param>
        /// <param name="ensureTrailingSlash">Ensure that the result ends with a trailing slash</param>
        /// <returns></returns>
        private static string ConvertLocalDirectoryPathToUrl(string absoluteOrRelativePath, bool ensureTrailingSlash = true)
        {
            if (ensureTrailingSlash && !absoluteOrRelativePath.EndsWith("\\", StringComparison.Ordinal))
                absoluteOrRelativePath = absoluteOrRelativePath + "\\";

            return string.Format(
                CultureInfo.InvariantCulture,
                "file:///{0}",
                absoluteOrRelativePath.Replace("\\", "/")
            );
        }

        /// <summary>
        /// Create an absolute path relative to the solution directory.
        /// </summary>
        /// <param name="fileName">The relative path of the file that need to be resolved.</param>
        /// <returns>The absolute file path</returns>
        private static string ResolvePathRelativeToSolution(string fileName)
        {
            return ResolvePaths(fileName, ProjectHelpers.GetSolutionFolderPath()).FirstOrDefault();
        }

        /// <summary>
        /// Get the default path of the solution custom stylesheet
        /// </summary>
        /// <returns></returns>
        public static string GetCustomSolutionStylesheetFilePath()
        {
            return ResolvePathRelativeToSolution(_stylesheet);
        }

        /// <summary>
        /// Get the default path of the solution custom html template
        /// </summary>
        /// <returns></returns>
        public static string GetCustomSolutionHtmlTemplateFilePath()
        {
            return ResolvePathRelativeToSolution(_htmlTemplate);
        }

        /// <summary>
        /// Save the current document position.
        /// </summary>
        /// <param name="browser"></param>
        private void SaveDocumentVerticalPosition(WebBrowser browser)
        {
            var document = browser.Document as HTMLDocument;
            if (document == null)
                return;

            _cachedPosition = document.documentElement.getAttribute("scrollTop");
            _cachedHeight = Math.Max(1.0, document.body.offsetHeight);
            _positionPercentage = _cachedPosition * 100 / _cachedHeight;
        }

        /// <summary>
        /// Restore the vertical position saved by the 
        /// last call To SaveDocumentVerticalPosition()
        /// in the current loaded html document.
        /// </summary>
        /// <param name="browser"></param>
        private void RestoreDocumentVerticalPosition(WebBrowser browser)
        {
            var document = browser.Document as HTMLDocument;
            if (document == null)
                return;

            _cachedHeight = document.body.offsetHeight;
            document.documentElement.setAttribute("scrollTop", _positionPercentage * _cachedHeight / 100);
        }

        /// <summary>
        /// If a custom solution stylesheet is found in the default location 
        /// this method returns a html link element string literal that reference it.
        /// Otherwise it return a default html style element string literal.
        /// </summary>
        /// <returns></returns>
        public static string GetStylesheet(string globalStylesheetPath, params string[] folders)
        {
            string filePath = GetExistingFilesInFolders(_stylesheet, folders).FirstOrDefault();

            if (null == filePath)
                filePath = globalStylesheetPath;

            if (filePath != null)
            {
                string linkFormat = "<link rel=\"stylesheet\" href=\"{0}\" />";
                return string.Format(CultureInfo.CurrentCulture, linkFormat, ConvertLocalDirectoryPathToUrl(filePath, false));
            }

            return "<style>body{font: 1.1em 'Century Gothic'}</style>";
        }

        /// <summary>
        /// Create a default stylesheet in the default location
        /// and add it to the current solution.
        /// </summary>
        /// <returns></returns>
        public async static Task CreateStylesheet()
        {
            string file = GetCustomSolutionStylesheetFilePath();
            await FileHelpers.WriteAllTextRetry(file, "body { background: yellow; }");
            ProjectHelpers.GetSolutionItemsProject().ProjectItems.AddFromFile(file);
        }

        /// <summary>
        /// Create a default html template in the location 
        /// and add it to the current solution.
        /// </summary>
        /// <returns></returns>
        public async static Task CreateHtmlTemplate()
        {
            const string _defaultHtmlTemplate = @"<!DOCTYPE html>
<html lang="""" en"""" xmlns=""http //www.w3.org/1999/xhtml"">
<head>
    <meta charset=""utf-8"" />
    <base href=""{##DOCUMENT_PATH_PLACEHOLDER##}""/>
    <title>Markdown Preview</title>
</head>
<body>
    <div class=""markdown-body"">{##MARKDOWN_HTML_PLACEHOLDER##}</div>
</body>
</html>";
            string file = GetCustomSolutionHtmlTemplateFilePath();
            await FileHelpers.WriteAllTextRetry(file, _defaultHtmlTemplate);
            ProjectHelpers.GetSolutionItemsProject().ProjectItems.AddFromFile(file);
        }

        /// <summary>
        /// Retrieve the string that rapresent the html to display in 
        /// the preview.
        /// If an html template is found in the project folder or in the solution folder, it 
        /// is returned after the place holders have been substituted.
        /// If no html template is found, the old behavior that was in place before 
        /// the html template was introduced is used (a minimal html is returned with a 
        /// reference to a custom stylesheet). The only change is that a custom stylesheet 
        /// found in the project folder will take precedence over the one in the solution 
        /// folder.
        /// </summary>
        /// <remarks>
        /// The supported placeholders are:
        /// 
        /// {##SOLUTION_PATH_PLACEHOLDER##}
        /// {##PROJECT_PATH_PLACEHOLDER##}
        /// {##DOCUMENT_PATH_PLACEHOLDER##}
        /// {##MARKDOWN_HTML_PLACEHOLDER##}
        /// 
        /// Note that to keep things simple, a placeholder must appear in the html template
        /// literally. there is no parsing involved.
        /// </remarks>
        /// <param name="compilerResult">The result of the markdown compilation</param>
        /// <returns>The html string</returns>
        public static string CompileHtmlDocumentString(string currentDocumentPath, string globalStylesheetPath, CompilerResult compilerResult)
        {
            // The Markdown compiler cannot return errors
            var solutionPath = ProjectHelpers.GetSolutionFolderPath();
            var projectPath = ProjectHelpers.GetProjectFolder(currentDocumentPath);
            var htmlTemplateFilePath = GetExistingFilesInFolders(_htmlTemplate, projectPath, solutionPath).FirstOrDefault();

            var currentDocumentPathUrl = ConvertLocalDirectoryPathToUrl(Path.GetDirectoryName(currentDocumentPath));

            if (htmlTemplateFilePath != null)
            {
                var solutionPathUrl = solutionPath == null ? currentDocumentPathUrl : ConvertLocalDirectoryPathToUrl(solutionPath);
                var projectPathUrl = projectPath == null ? currentDocumentPathUrl : ConvertLocalDirectoryPathToUrl(projectPath);

                //Load the template and replace the placeholder with the generated html
                var htmlTemplate = File.ReadAllText(htmlTemplateFilePath);

                // TODO: Find a better way to do this(Handlerbar, HTML parsing, etc...)
                return htmlTemplate
                    .Replace("{##SOLUTION_PATH_PLACEHOLDER##}", solutionPathUrl)
                    .Replace("{##PROJECT_PATH_PLACEHOLDER##}", projectPathUrl)
                    .Replace("{##DOCUMENT_PATH_PLACEHOLDER##}", currentDocumentPathUrl)
                    .Replace("{##MARKDOWN_HTML_PLACEHOLDER##}", compilerResult.Result);
            }

            // Keep the legacy behavior
            return string.Format(
                CultureInfo.InvariantCulture,
                @"<!DOCTYPE html>
                    <html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
                        <head>
                            <meta charset=""utf-8"" />
                            <base href=""{0}"">                                  
                            <title>Markdown Preview</title>
                            {1}
                        </head>
                        <body>
                        {2}
                        </body>
                    </html>",
                currentDocumentPathUrl,
                GetStylesheet(globalStylesheetPath, projectPath, solutionPath),
                compilerResult.Result
            );
        }

        protected override void UpdateMargin(CompilerResult result)
        {
            if (_browser == null)
                return;

            var htmlString = CompileHtmlDocumentString(Document.FilePath, _globalStylesheetPath, result);
            if (_browser.Document == null)
            {
                _browser.NavigateToString(htmlString);
                return;
            }

            SaveDocumentVerticalPosition(_browser);
            _browser.NavigateToString(htmlString);
        }

        protected override FrameworkElement CreatePreviewControl()
        {
            ConfigureRequiredInternetExplorerFeatures();

            _browser = new WebBrowser();
            _browser.HorizontalAlignment = HorizontalAlignment.Stretch;

            // This can be done only once
            _browser.ObjectForScripting = new JavaScriptToManagedConnector((errorMsg, document, line) =>
            {
                // Unfortunatly, when the error is in a file other than the main document the errorMsg 
                // and line are not available.
                // Instead they always contain : line:"0" errorMsg:"script Error"
                // This is another IE "feature" to "protect" you :).
                Log("Error in ({0}:{1}): {2}", document, line, errorMsg);
            });

            _browser.Navigated += (sender, ev) =>
            {
                if (!ShouldHandleNavigationEvent(ev))
                    return;

                var browser = sender as WebBrowser;
                InjectJavascriptErrorsRedirection(browser);
            };

            _browser.LoadCompleted += (sender, ev) =>
            {
                if (!ShouldHandleNavigationEvent(ev))
                    return;

                var browser = sender as WebBrowser;
                RestoreDocumentVerticalPosition(browser);
            };

            return _browser;
        }

        /// <summary>
        /// Configure the current user settings for Internet Explorer when 
        /// used through the BrowserControl by changing the user 
        /// registry.
        /// </summary>
        private static void ConfigureRequiredInternetExplorerFeatures()
        {
            // This is really required only if we have an html template, but we
            // need to enable it here since the WebBrowser control implementation 
            // read the IE feature registry keys only when a control
            // is instantiated the first time in a process.
            // This will log if something goes wrong(eg. no access to the registry) and hopefully,
            // help the user figure out what to do (he probably need to launch visual studio
            // with elevate privileges the first time).
            // This should probably be called by a menu command but if the user already 
            // opened a markdown file, then Visual studio need to be restarted for 
            // the setting to take effect.
            try
            {
                InternetExplorerBrowserFeatureControl.DisableBlockCrossProtocolFileNavigation();
                InternetExplorerBrowserFeatureControl.AutoSetBrowserEmulationVersion();
            }
            catch (InternetExplorerFeatureControlSecurityException exception)
            {
                Log("Cannot configure the internet explorer features required to correctly display the markdown preview. " +
                    "You can try restarting visual studio as an Administrator and open a " +
                    "markdown document again. You will need to this only once.");
                Log(exception.Message);
            }
            catch (Exception exception)
            {
                Log(exception, "Cannot configure the internet explorer features required to correctly display the markdown preview.");
            }
        }

        /// <summary>
        /// This method will return true only for events fired 
        /// after NavigateToString() has been programatically invoked.
        /// Eg. It will return false if the user click on a link in the 
        /// previewed html.
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        private static bool ShouldHandleNavigationEvent(System.Windows.Navigation.NavigationEventArgs @event)
        {
            // If the user click on a link in the preview window, @event.Uri is null.
            return @event.Uri == null;
        }

        /// <summary>
        /// Hook the the DOM onerror event and redirect it to the managed host that 
        /// will then write it to the visual studio output pane.
        /// Note that this need to be done on each new page loaded by the browser control.
        /// </summary>
        /// <param name="browser"></param>
        private static void InjectJavascriptErrorsRedirection(WebBrowser browser)
        {
            string disableScriptError = @"function externalError(errorMsg, document, lineNumber) 
            {
                window.external.OnError(errorMsg, document, lineNumber);
                return true;
            }
            window.onerror = externalError;";

            var doc = browser.Document as HTMLDocument;
            if (doc != null)
            {
                var heads = doc.getElementsByTagName("head").OfType<HTMLHeadElement>();
                foreach (var head in heads)
                {
                    var scriptErrorSuppressed = (IHTMLScriptElement)doc.createElement("SCRIPT");
                    scriptErrorSuppressed.type = "text/javascript";
                    scriptErrorSuppressed.text = disableScriptError;
                    head.appendChild((IHTMLDOMNode)scriptErrorSuppressed);
                }
            }
        }

        private static void Log(string format, params object[] args)
        {
            Logger.Log(string.Format(CultureInfo.InvariantCulture, "[markdown]: " + format, args));
        }
        private static void Log(Exception ex, string format, params object[] args)
        {
            Log(format, args);
            Logger.Log(ex);
        }
    }

    /// <summary>
    /// Thrown when the user does not have the right or permission to change a 
    /// Internet Explorer Feature Control entry in the registry.
    /// </summary>
    [Serializable]
    public class InternetExplorerFeatureControlSecurityException : Exception
    {
        public InternetExplorerFeatureControlSecurityException()
        {
        }

        public InternetExplorerFeatureControlSecurityException(string message)
            : base(message)
        {
        }

        public InternetExplorerFeatureControlSecurityException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected InternetExplorerFeatureControlSecurityException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Provide methods to change the registry Internet Explorer behavior when embedded in an 
    /// application by changing the Feature Control settings in the windows registry.
    /// </summary>
    internal class InternetExplorerBrowserFeatureControl
    {

        private const string InternetExplorerRootKey = @"Software\Microsoft\Internet Explorer";
        private const string BrowserEmulationKey = InternetExplorerRootKey + @"\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
        private const string BlockCrossProtocolFileNavigation = InternetExplorerRootKey + @"\MAIN\FeatureControl\FEATURE_BLOCK_CROSS_PROTOCOL_FILE_NAVIGATION";

        private enum BrowserEmulationVersion
        {
            Default = 0,
            Version7 = 7000,
            Version8 = 8000,
            Version8Standards = 8888,
            Version9 = 9000,
            Version9Standards = 9999,
            Version10 = 10000,
            Version10Standards = 10001,
            Version11 = 11000,
            Version11Edge = 11001
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key">The parent registry key.</param>
        /// <param name="subkeyName">Name or path of the subkey to open.</param>
        /// <param name="writable">
        /// Set to true if you need write access to the key. 
        /// If this parameter is true and the subkey does not exist a new one 
        /// will be created.
        /// </param>
        /// <returns>
        /// The RegistryKey instance that can be used to access 
        /// the registry sub key
        /// </returns>
        private static Microsoft.Win32.RegistryKey OpenOrCreateKey(Microsoft.Win32.RegistryKey key,
            string subkeyName, bool writable)
        {
            var subKey = key.OpenSubKey(subkeyName, writable);
            if (subKey == null && writable)
                subKey = key.CreateSubKey(subkeyName);
            return subKey;
        }

        /// <summary>
        /// Open the subkey specified by subKeyName, 
        /// call the provided function passing the RegistryKey instance 
        /// and dispose it automatically aferward.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="key">The parent registry key.</param>
        /// <param name="subkeyName">Name or path of the subkey to open.</param>
        /// <param name="writable">
        /// Set to true if you need write access to the key. 
        /// If this parameter is true and the subkey does not exist a new one 
        /// will be created.
        /// </param>
        /// <param name="useKey">the function where the key can be used</param>
        /// <returns>
        /// The return value of the function provided in the useKey parameter 
        /// after its execution.
        /// </returns>
        /// <exception cref="InternetExplorerFeatureControlSecurityException">
        /// The used does not have the required permission or rights to 
        /// execute this operation.
        /// </exception>
        private static T UseRegistryKey<T>(
            Microsoft.Win32.RegistryKey key, string subkeyName,
            bool writable, Func<Microsoft.Win32.RegistryKey, T> useKey
        )
        {
            try
            {
                using (var subKey = OpenOrCreateKey(key, subkeyName, writable))
                {
                    return useKey(subKey);
                }
            }
            catch (System.Security.SecurityException se)
            {
                // The user does not have the permissions required to read from the registry key.
                throw new InternetExplorerFeatureControlSecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "The current user does not have the rights to open the registry key: '{0}\\{1}'", key, subkeyName),
                    se
                );

            }
            catch (UnauthorizedAccessException uaEx)
            {
                // The user does not have the necessary registry rights.
                throw new InternetExplorerFeatureControlSecurityException(
                    string.Format(CultureInfo.CurrentCulture, "The current user is not authorized to access the registry key: '{0}\\{1}'", key, subkeyName),
                    uaEx
                );
            }
        }

        private static int GetInternetExplorerMajorVersion()
        {
            return UseRegistryKey(Microsoft.Win32.Registry.LocalMachine, InternetExplorerRootKey, false, key =>
            {
                var result = 0;
                if (key != null)
                {
                    var value = key.GetValue("svcVersion", null) ?? key.GetValue("Version", null);

                    if (value != null)
                    {
                        var version = value.ToString();
                        var separator = version.IndexOf('.');
                        if (separator != -1)
                        {
                            if (!int.TryParse(version.Substring(0, separator), out result))
                            {
                                // Make CodeAnalisys happy :)
                                result = 0;
                            }
                        }
                    }
                }
                return result;
            });
        }

        private static bool IsBrowserEmulationSet()
        {
            return GetBrowserEmulationVersion() != BrowserEmulationVersion.Default;
        }

        private static BrowserEmulationVersion GetBrowserEmulationVersion()
        {
            return UseRegistryKey(Microsoft.Win32.Registry.CurrentUser, BrowserEmulationKey, false, key =>
            {
                if (key != null)
                {
                    var programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                    var value = key.GetValue(programName, null);

                    if (value != null)
                    {
                        return (BrowserEmulationVersion)Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    }
                }
                return BrowserEmulationVersion.Default;
            });
        }

        private static bool SetBrowserEmulationVersion(BrowserEmulationVersion browserEmulationVersion)
        {
            return UseRegistryKey(Microsoft.Win32.Registry.CurrentUser, BrowserEmulationKey, true, key =>
            {
                if (key != null)
                {
                    var programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);

                    if (browserEmulationVersion != BrowserEmulationVersion.Default)
                    {
                        // if it's a valid value, update or create the value
                        key.SetValue(programName, (int)browserEmulationVersion, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                    else
                    {
                        // otherwise, remove the existing value
                        key.DeleteValue(programName, false);
                    }

                    return true;
                }
                return false;
            });
        }

        public static bool DisableBlockCrossProtocolFileNavigation()
        {
            return UseRegistryKey(Microsoft.Win32.Registry.CurrentUser, BlockCrossProtocolFileNavigation, true, key =>
            {
                if (key != null)
                {
                    var programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                    var current = key.GetValue(programName);
                    if (current is int && (int)current == 0)
                        return true;
                    // Update or create the value
                    key.SetValue(programName, 0, Microsoft.Win32.RegistryValueKind.DWord);
                    return true;
                }
                return false;
            });
        }

        /// <summary>
        /// Set the Browser Emulation Mode for the current user equal to the installed internet 
        /// explorer version in this system.
        /// Note that this require the user identity under witch the process is running 
        /// to have the appropriate permissions to access the registry (HKEY_LOCAL_MACHINE: read only, 
        /// HKEY_CURRENT_USER: read/write).
        /// </summary>
        /// <returns>true if the operation succeed.</returns>
        public static bool AutoSetBrowserEmulationVersion()
        {
            if (IsBrowserEmulationSet())
                return true;

            BrowserEmulationVersion emulationCode;
            var ieVersion = GetInternetExplorerMajorVersion();

            if (ieVersion >= 11)
            {
                emulationCode = BrowserEmulationVersion.Version11;
            }
            else
            {
                switch (ieVersion)
                {
                    case 10:
                        emulationCode = BrowserEmulationVersion.Version10;
                        break;
                    case 9:
                        emulationCode = BrowserEmulationVersion.Version9;
                        break;
                    case 8:
                        emulationCode = BrowserEmulationVersion.Version8;
                        break;
                    default:
                        emulationCode = BrowserEmulationVersion.Version7;
                        break;
                }
            }

            return SetBrowserEmulationVersion(emulationCode);
        }
    }

    /// <summary>
    /// Used to proxy javascript error to the managed host.
    /// MUST be public.
    /// </summary>
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class JavaScriptToManagedConnector
    {
        private readonly Action<object, object, object> _errorHandler;

        public JavaScriptToManagedConnector()
        {
        }

        public JavaScriptToManagedConnector(Action<object, object, object> errorHandler)
        {
            _errorHandler = errorHandler;
        }

        public void OnError(object arg1, object arg2, object arg3)
        {
            _errorHandler(arg1, arg2, arg3);
        }
    }
}