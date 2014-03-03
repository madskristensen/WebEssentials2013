//using System.Collections.Generic;
//using System.ComponentModel.Design;
//using System.IO;
//using System.Linq;
//using EnvDTE80;
//using Microsoft.VisualStudio.Shell;

//namespace MadsKristensen.EditorExtensions
//{
//    internal class ChainCompilationMenu
//    {
//        public OleMenuCommand Command { get; private set; }

//        private DTE2 _dte;
//        private OleMenuCommandService _mcs;
//        private readonly IEnumerable<string> _sourceExtensions = Mef.GetChainedCompileExtensions();
//        private IEnumerable<string> selectedFiles;

//        public ChainCompilationMenu(DTE2 dte, OleMenuCommandService mcs)
//        {
//            _dte = dte;
//            _mcs = mcs;
//        }

//        public void SetupCommands()
//        {
//            Mef.SatisfyImportsOnce(this);

//            Command = new OleMenuCommand((s, e) => Execute(),
//                new CommandID(CommandGuids.guidEditorExtensionsCmdSet, (int)CommandId.ChainCompile));
//            Command.BeforeQueryStatus += (s, e) => CheckVisible();
//            _mcs.AddCommand(Command);
//        }

//        private void CheckVisible()
//        {
//            selectedFiles = ProjectHelpers.GetSelectedFilePaths()
//                .Where(p => _sourceExtensions.Contains(Path.GetExtension(p)));
//            Command.Enabled = selectedFiles.Any();
//        }
//        private void Execute()
//        {
//            foreach (var file in selectedFiles)
//            {
//                ProjectHelpers.ChainIgnoreList.Add(file);
//            }
//        }
//    }
//}