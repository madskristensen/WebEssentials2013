using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class DiffMenu
    {
        private OleMenuCommandService _mcs;

        public DiffMenu(OleMenuCommandService mcs)
        {
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(GuidList.guidDiffCmdSet, (int)PkgCmdIDList.cmdDiff);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => Sort(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);
        }

        //private List<string> list = new List<string>()
        //{
        //    ".txt", ".cs", ".aspx", ".ascx", ".asmx", ".master", ".cshtml", ".vbhtml", ".js", ".coffee", ".css", ".less", ".sass", ".scss", ".xml"
        //};

        private List<string> files;

        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            files = new List<string>(ProjectHelpers.GetSelectedItemPaths());

            //if (files.Count == 2)
            //{
            //    if (list.Contains(Path.GetExtension(files[0]).ToLowerInvariant()))
            //    {
            //        if (list.Contains(Path.GetExtension(files[1]).ToLowerInvariant()))
            //        {
            //            menuCommand.Enabled = true;
            //            return;
            //        }
            //    }
            //}

            menuCommand.Enabled = files.Count == 2;
        }

        private void Sort()
        {
            if (files.Count == 2)
                EditorExtensionsPackage.ExecuteCommand("Tools.DiffFiles \"" + files[0] + "\" \"" + files[1] + "\"");
        }
    }
}