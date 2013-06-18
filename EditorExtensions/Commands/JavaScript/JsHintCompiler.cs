using MadsKristensen.EditorExtensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Threading;

/// <summary>
/// Summary description for Lint
/// </summary>
[ComVisible(true)]
public class JsHintCompiler : ScriptRunnerBase
{
    private string _settings;
    private JsHintOptions _options;

    public JsHintCompiler(Dispatcher dispatcher)
        : base(dispatcher)
    { }

    protected override string CreateHtml(string source, string state)
    {
        if (_options == null)
        {
            _options = new JsHintOptions();
            _options.LoadSettingsFromStorage();
            JsHintOptions.Changed += delegate { _options.LoadSettingsFromStorage(); GenerateSettings(); };

            GenerateSettings();
        }

        source = source
            .Replace("\\", "\\\\")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("'", "\\'");

        string script = ReadResourceFile("MadsKristensen.EditorExtensions.Resources.Scripts.jshint.js") +
                        "JSHINT('" + source + "', {" + _settings + "});" +
                        "window.external.Execute(JSON.stringify(JSHINT.errors), '" + state.Replace("\\", "\\\\") + "')";

        return "<html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=9\" /><script>" + script + "</script></head><html/>";
    }

    private void GenerateSettings()
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

        _settings = string.Join(", ", list);
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