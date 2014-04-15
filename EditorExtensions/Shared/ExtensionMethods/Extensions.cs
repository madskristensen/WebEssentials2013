using System;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ConfOxide.MemberAccess;
using MadsKristensen.EditorExtensions.Settings;
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

        ///<summary>Find the cloumn position in the last line.</summary>
        public static int GetLineColumn(this string text, int targetIndex, int lineNumber)
        {
            var result = targetIndex - text.NthIndexOfCharInString('\n', lineNumber);

            return Math.Max(0, result);
        }

        //<summary>Find the nth occurance of needle in haystack.</summary>.
        public static int NthIndexOfCharInString(this string strHaystack, char charNeedle, int intOccurrenceToFind)
        {
            if (intOccurrenceToFind < 1) return 0;

            int intReturn = -1;
            int count = 0;
            int n = 0;

            while (count < intOccurrenceToFind && (n = strHaystack.IndexOf(charNeedle, n)) != -1)
            {
                n++;
                count++;
            }

            if (count == intOccurrenceToFind) intReturn = n;

            return intReturn;
        }
    }
}
