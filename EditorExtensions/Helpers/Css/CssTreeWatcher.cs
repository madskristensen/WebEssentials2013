using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [ContentType("css")]
    [Order(After = "Visual Studio CSS Text View Connection Listener")]
    [Export(typeof(IWpfTextViewConnectionListener))]
    class CssTreeWatcherAdder : IWpfTextViewConnectionListener
    {
        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            foreach (var buffer in subjectBuffers.Where(b => b.ContentType.IsOfType("css")))
            {
                CssTreeWatcher watcher;
                if (buffer.Properties.TryGetProperty(typeof(CssTreeWatcher), out watcher))
                    watcher.Tree = CssEditorDocument.FromTextBuffer(buffer).Tree;
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            foreach (var buffer in subjectBuffers.Where(b => b.ContentType.IsOfType("css")))
            {
                CssTreeWatcher watcher;
                if (buffer.Properties.TryGetProperty(typeof(CssTreeWatcher), out watcher))
                    watcher.Tree = null;
            }
        }
    }
    ///<summary>A persistent wrapper around a CssTree for a single TextBuffer</summary>
    /// <remarks>A ProjectionBuffer's CssTree can be replaced when the buffer 
    /// disconnected during a Format Document operation. This wrapper detects
    /// this and forwards events from the new CssTree.
    /// When using this class, you must only add event handlers to the events
    /// in the wrapper class, not those in CssTree.</remarks>
    public class CssTreeWatcher
    {
        private CssTree _tree;

        public ITextBuffer Buffer { get; private set; }

        public CssTree Tree
        {
            get { return _tree; }

            set
            {
                if (Tree == value) return;
                if (Tree != null)
                {
                    Tree.ItemsChanged -= Tree_ItemsChanged;
                    Tree.TreeUpdated -= Tree_TreeUpdated;
                }
                _tree = value;
                if (value != null)
                {
                    Tree.ItemsChanged += Tree_ItemsChanged;
                    Tree.TreeUpdated += Tree_TreeUpdated;
                }
            }
        }

        public ParseItem StyleSheet { get { return Tree == null ? null : Tree.StyleSheet; } }

        private void Tree_TreeUpdated(object sender, CssTreeUpdateEventArgs e) { if (TreeUpdated != null) TreeUpdated(sender, e); }
        private void Tree_ItemsChanged(object sender, CssItemsChangedEventArgs e) { if (ItemsChanged != null) ItemsChanged(sender, e); }

        private CssTreeWatcher(ITextBuffer buffer)
        {
            Buffer = buffer;
            Tree = CssEditorDocument.FromTextBuffer(buffer).Tree;
        }
        public static CssTreeWatcher ForBuffer(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new CssTreeWatcher(buffer));
        }

        public event EventHandler<CssItemsChangedEventArgs> ItemsChanged;
        public event EventHandler<CssTreeUpdateEventArgs> TreeUpdated;
    }
}
