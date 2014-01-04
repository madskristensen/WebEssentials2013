using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    [SuppressMessage("Microsoft.Interoperability", "CA1409:ComVisibleTypesShouldBeCreatable", Justification = "Exposed via WebBrowser ScriptingObject; cannot be created independently")]
    [ComVisible(true)]  // Required to expose this instance to WebBrowser for JS
    public class JsHintCompiler : ScriptRunnerBase
    {
        private string _defaultSettings;
        private JsHintOptions _options;

        public JsHintCompiler(Dispatcher dispatcher)
            : base(dispatcher)
        { }

        protected override string CreateHtml(string source, string fileName)
        {
            // I override the meaning of this parameter to
            // get the path so that I can find .jshintrc.
            if (!File.Exists(fileName))
                throw new ArgumentException("The state parameter to Compile() must be the full path to the file being linted.", "fileName");

            if (_options == null)
            {
                _options = new JsHintOptions();
                _options.LoadSettingsFromStorage();
                JsHintOptions.Changed += delegate { _options.LoadSettingsFromStorage(); GenerateGlobalSettings(); };

                GenerateGlobalSettings();
            }

            source = source
                .Replace("\\", "\\\\")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("'", "\\'");

            string script = ReadResourceFile("MadsKristensen.EditorExtensions.Resources.Scripts.jshint-2.4.0.js") +
                            "var settings = " + (FindLocalSettings(fileName) ?? "{" + _defaultSettings + "}") + ";" +   // If this file has no .jshintrc, fall back to the configured settings
                            "var globals = settings.globals; delete settings.globals;" +  // .jshintrc files have an optional globals section, which becomes the third parameter.  (globals is not a valid option)
                            "JSHINT('" + source + "', settings, globals);" +
                            "window.external.Execute(JSON.stringify(JSHINT.errors), '" + fileName.Replace("\\", "\\\\") + "')";

            return "<html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=9\" /><script>" + script + "</script></head><html/>";
        }

        private void GenerateGlobalSettings()
        {
            Type type = _options.GetType();
            PropertyInfo[] properties = type.GetProperties();
            List<string> list = new List<string>();

            foreach (PropertyInfo item in properties)
            {
                if (!item.Name.StartsWith("JsHint_", StringComparison.Ordinal))
                    continue;

                object value = item.GetValue(_options, null);
                int intValue;
                bool boolValue;

                if (int.TryParse(value.ToString(), out intValue) && intValue > -1)
                {
                    list.Add(item.Name.Replace("JsHint_", string.Empty) + ":" + intValue);
                }
                else if (bool.TryParse(value.ToString(), out boolValue) && boolValue == true)
                {
                    list.Add(item.Name.Replace("JsHint_", string.Empty) + ":true");
                }
            }

            _defaultSettings = string.Join(", ", list);
        }

        private static string FindLocalSettings(string sourcePath)
        {
            string dir = Path.GetDirectoryName(sourcePath);
            while (!File.Exists(Path.Combine(dir, ".jshintrc")))
            {
                dir = Path.GetDirectoryName(dir);
                if (String.IsNullOrEmpty(dir))
                    return null;
            }
            return File.ReadAllText(Path.Combine(dir, ".jshintrc"));
        }
    }

    // Used for JSON serialization
    public class Result
    {
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "id")]
        public string id { get; set; }
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "raw")]
        public string raw { get; set; }
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "evidence")]
        public string evidence { get; set; }
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "line")]
        public int line { get; set; }
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "character")]
        public int character { get; set; }
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a"), SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "a")]
        public string a { get; set; }
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b"), SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "b")]
        public string b { get; set; }
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "reason")]
        public string reason { get; set; }
    }
}