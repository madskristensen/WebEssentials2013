using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ConfOxide.MemberAccess;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    static class Extensions
    {
        ///<summary>Returns a strongly-typed Settings object for the specified ContentType, or null if <see cref="WESettings"/> has no properties with that name (including base types).</summary>
        /// <typeparam name="T">The interface to return.</typeparam>
        public static T ForContentType<T>(this WESettings settings, IContentType contentType)
        {
            var prop = TypeAccessor<WESettings>.Properties.FirstOrDefault(p => p.Property.Name.Equals(contentType.TypeName, StringComparison.OrdinalIgnoreCase));
            if (prop == null)
                return contentType.BaseTypes.Select(settings.ForContentType<T>)
                                            .FirstOrDefault(o => o != null);
            return ((ITypedPropertyAccessor<WESettings, T>)prop).GetValue(settings);
        }


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

        ///<summary>Execute process asynchronously.</summary>
        public static Task<Process> ExecuteAsync(this ProcessStartInfo startInfo)
        {
            var process = Process.Start(startInfo);
            var processTaskCompletionSource = new TaskCompletionSource<Process>();

            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => {
                process.WaitForExit();
                processTaskCompletionSource.TrySetResult(process);
            };

            return processTaskCompletionSource.Task;
        }
    }
}
