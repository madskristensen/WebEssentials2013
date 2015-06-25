using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace WebEssentials.BuildTasks.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var task = new NodeInstaller() { BuildEngine = new Engine() };
            task.Execute();

            var basePath = Path.GetFullPath(@"resources\nodejs\tools\node_modules");
            var directories = Directory.EnumerateDirectories(basePath, "*", SearchOption.AllDirectories)
                .OrderBy(d => d.Length)
                .Select(d => d.Substring(basePath.Length))
                .Select(d => d.Length.ToString().PadLeft(3) + ": " + d);

            Debug.WriteLine(string.Join("\r\n", directories));
            Console.WriteLine(directories.Count() + " directories created");

            Console.ReadLine();
        }

        class Engine : IBuildEngine
        {
            public int ColumnNumberOfTaskNode { get { return 0; } }

            public bool ContinueOnError { get { return false; } }

            public int LineNumberOfTaskNode { get { return 0; } }

            public string ProjectFileOfTaskNode { get { throw new NotImplementedException(); } }

            public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
            {
                throw new NotImplementedException();
            }

            public void LogCustomEvent(CustomBuildEventArgs e) { Log(e.Message); }
            public void LogErrorEvent(BuildErrorEventArgs e) { Log(e.Message); }

            public void LogMessageEvent(BuildMessageEventArgs e) { Log(e.Message); }

            public void LogWarningEvent(BuildWarningEventArgs e) { Log(e.Message); }

            void Log(string text)
            {
                Console.WriteLine(text);
                Debug.WriteLine(text);
            }
        }
    }
}
