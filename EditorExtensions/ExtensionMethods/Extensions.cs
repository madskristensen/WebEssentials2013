using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ConfOxide.MemberAccess;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    static class Extensions
    {
        ///<summary>Handles errors on a task to prevent VS from crashing.</summary>
        ///<param name="operation">The description of the asynchronous operation to show in the log on failure.  Should start with a lowercase present-tense infinitive (eg,  "compiling someFile.less").</param>
        /// <returns>A successful task, even if the operation fails.</returns>
        ///<remarks>Call this method when aggregating async operations to report and ignore individual failures.</remarks>
        public static Task HandleErrors(this Task task, string operation)
        {
            // Add the error handler, then return the original task.
            // Don't wait on the continuation; it'll become canceled
            // on success.  http://stackoverflow.com/q/6573720/34397
            task.DontWait(operation);
            return task;
        }
        ///<summary>Handles errors on a task to prevent VS from crashing.</summary>
        ///<param name="operation">The description of the asynchronous operation to show in the log on failure.  Should start with a lowercase present-tense infinitive (eg,  "compiling someFile.less").</param>
        ///<remarks>Call this method when you call an async method and don't need to wait for the result.</remarks>
        public static void DontWait(this Task task, string operation)
        {
            if (SettingsStore.InTestMode)
                return; // Don't mask crashes in unit tests.
            task.ContinueWith(t =>
            {
                if (t.Exception == null)
                    return;
                Logger.Log("An exception was thrown when " + operation + ": "
                         + string.Join(Environment.NewLine, t.Exception.InnerExceptions));
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

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
            var name = contentType.TypeName;
            if (name.Equals("HTMLX", StringComparison.OrdinalIgnoreCase)) name = "Html";
            var prop = TypeAccessor<WESettings>.Properties.FirstOrDefault(p => p.Property.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
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

        ///<summary>Returns an awaitable object that resumes execution on the specified Dispatcher.</summary>
        /// <remarks>Copied from <see cref="Dispatcher.Yield"/>, which can only be called on the Dispatcher thread.</remarks>
        public static DispatcherPriorityAwaitable NextFrame(this Dispatcher dispatcher, DispatcherPriority priority = DispatcherPriority.Background)
        {
            return new DispatcherPriorityAwaitable(dispatcher, priority);
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
            process.Exited += (s, e) =>
            {
                process.WaitForExit();
                processTaskCompletionSource.TrySetResult(process);
            };

            return processTaskCompletionSource.Task;
        }
    }
}
