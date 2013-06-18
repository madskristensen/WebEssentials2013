using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;

[ComVisible(true)]
public abstract class ScriptRunnerBase : IDisposable
{
    private WebBrowser _browser = new WebBrowser();
    private bool _disposed;
    private Dispatcher _dispatcher;

    public ScriptRunnerBase(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    protected abstract string CreateHtml(string source, string state);

    public void Compile(string source, string state)
    {
        _dispatcher.BeginInvoke(new Action(() =>
        {
            _browser.ObjectForScripting = this;
            _browser.ScriptErrorsSuppressed = true;
            _browser.DocumentText = CreateHtml(source, state);

        }), DispatcherPriority.ApplicationIdle, null);
    }

    public void Execute(string result, string state)
    {
        OnCompleted(result, state);
    }

    protected static string ReadResourceFile(string resourceFile)
    {
        using (Stream s = typeof(JsHintCompiler).Assembly.GetManifestResourceStream(resourceFile))
        using (var reader = new StreamReader(s))
        {
            return reader.ReadToEnd();
        }
    }

    public event EventHandler<CompilerEventArgs> Completed;

    protected void OnCompleted(string message, string data)
    {
        if (Completed != null)
        {
            Completed(this, new CompilerEventArgs() { Result = message, State = data });
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Completed = null;

            if (_browser != null)
            {
                _browser.Dispose();
            }

            _browser = null;
            _disposed = true;
        }
    }
}

public class CompilerEventArgs : EventArgs
{
    public string Result { get; set; }
    public string State { get; set; }
}