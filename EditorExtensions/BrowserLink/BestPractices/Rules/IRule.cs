using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public interface IRule
    {

        string Message { get; }

        TaskErrorCategory Category { get; }

        string Question { get;}
        void Navigate(object sender, EventArgs e);
    }
}
