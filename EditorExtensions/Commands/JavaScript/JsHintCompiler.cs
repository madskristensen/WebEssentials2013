using MadsKristensen.EditorExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Threading;

/// <summary>
/// Summary description for Lint
/// </summary>
[ComVisible(true)]
public class JsHintCompiler : ScriptRunnerBase
{
    private string _defaultSettings;
    private JsHintOptions _options;

    public JsHintCompiler(Dispatcher dispatcher)
        : base(dispatcher)
    { }

    protected override string CreateHtml(string source, string filename)
    {
        // I override the meaning of this parameter to
        // get the path so that I can find .jshintrc.
        if (!File.Exists(filename))
            throw new ArgumentException("The state parameter to Compile() must be the full path to the file being linted.", "filename");
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

        string script = ReadResourceFile("MadsKristensen.EditorExtensions.Resources.Scripts.jshint.js") +
                        "var settings = " + (FindLocalSettings(filename) ?? "{" + _defaultSettings + "}") + ";" +   // If this file has no .jshintrc, fall back to the configured settings
                        "JSHINT('" + source + "', settings, settings.globals);" +   // .jshintrc files have an optional globals section, which becomes the third parameter
                        "window.external.Execute(JSON.stringify(JSHINT.errors), '" + filename.Replace("\\", "\\\\") + "')";

        return "<html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=9\" /><script>" + script + "</script></head><html/>";
    }

    private void GenerateGlobalSettings()
    {
        Type type = _options.GetType();
        PropertyInfo[] properties = type.GetProperties();
        List<string> list = new List<string>();

        foreach (PropertyInfo item in properties)
        {
            if (!item.Name.StartsWith("JsHint_"))
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
        while(!File.Exists(Path.Combine(dir, ".jshintrc")))
        {
            dir = Path.GetDirectoryName(dir);
            if (String.IsNullOrEmpty(dir))
                return null;
        }
        return File.ReadAllText(Path.Combine(dir, ".jshintrc"));
    }
}

public class Result
{
    public string id { get; set; }
    public string raw { get; set; }
    public string evidence { get; set; }
    public int line { get; set; }
    public int character { get; set; }
    public string a { get; set; }
    public string b { get; set; }
    public string reason { get; set; }
}