using System;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    public interface IRule
    {
        string Message { get; }
        TaskErrorCategory Category { get; }
        string Question { get; }

        void Navigate(object sender, EventArgs e);
    }
}
