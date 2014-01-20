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
        ///<summary>Returns a strongly-typed Settings object for the specified ContentType, or null if <see cref="WESettings"/> has no properties with that name (including base types) or type.</summary>
        ///<typeparam name="T">The interface to return.</typeparam>
        ///<remarks>
        /// If the ContentType has a settings property, but that property 
        /// does not implement <typeparamref name="T"/>, this will return
        /// null, even if its base type has a settings property that does
        /// implement <typeparamref name="T"/>.  For example, this cannot
        /// return <see cref="IMinifierSettings"/> for LESS.
        ///</remarks>
        public static T ForContentType<T>(this WESettings settings, IContentType contentType) where T : class
        {
            var prop = TypeAccessor<WESettings>.Properties.FirstOrDefault(p => p.Property.Name.Equals(contentType.TypeName, StringComparison.OrdinalIgnoreCase));
            if (prop == null)
                return contentType.BaseTypes.Select(settings.ForContentType<T>)
                                            .FirstOrDefault(o => o != null);
            var typedProp = prop as ITypedPropertyAccessor<WESettings, T>;
            if (typedProp == null)
                return null;
            return typedProp.GetValue(settings);
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
