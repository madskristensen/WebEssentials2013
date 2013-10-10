using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Core;

namespace MadsKristensen.EditorExtensions
{
    public static class HtmlExtensions
    {
        public static bool CompareCurrent(this CharacterStream stream, string text, bool ignoreCase = false)
        {
            return stream.CompareTo(stream.Position, text.Length, text, ignoreCase);
        }
    }
}
