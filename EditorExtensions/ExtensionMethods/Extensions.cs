using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    static class Extensions
    {
        ///<summary>Runs a callback when an iamge is fully downloaded, or immediately if the image has already been downloaded.</summary>
        public static void OnDownloaded(this BitmapSource image, Action callback)
        {
            if (image.IsDownloading)
                image.DownloadCompleted += (s, e) => callback();
            else
                callback();
        }

        ///<summary>Replaces a TextBuffer's entire content with the specified text.</summary>
        public static void SetText(this ITextBuffer buffer, string text)
        {
            buffer.Replace(new Span(0, buffer.CurrentSnapshot.Length), text);
        }

        ///<summary>Test the numericality of sequence.</summary>
        public static bool IsNumeric(this string input)
        {
            return input.All(digit => char.IsDigit(digit) || digit.Equals('.'));
        }

        ///<summary>Execute process asyncronously.</summary>
        public static Task<Process> ExecuteAsync(this ProcessStartInfo startInfo, StringBuilder error)
        {
            var process = Process.Start(startInfo);
            var processTaskCompletionSource = new TaskCompletionSource<Process>();

            //note: if we don't also read from the standard output, we don't receive the error output... ?
            process.OutputDataReceived += (_, __) => { };
            process.ErrorDataReceived += (sender, line) =>
            {
                error.AppendLine(line.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.EnableRaisingEvents = true;
            EventHandler exitHandler = (s, e) =>
            {
                // WaitForExit() ensures that the StandardError stream has been drained
                process.WaitForExit();
                processTaskCompletionSource.TrySetResult(process);
            };

            process.Exited += exitHandler;

            if (process.HasExited) exitHandler(process, null);
            return processTaskCompletionSource.Task;
        }
    }
}
