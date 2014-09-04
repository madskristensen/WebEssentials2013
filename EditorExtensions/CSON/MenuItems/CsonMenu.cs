using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using MadsKristensen.EditorExtensions.Compilers;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.CoffeeScript
{
    internal class CsonMenu
    {
        private OleMenuCommandService _mcs;

        public CsonMenu(OleMenuCommandService mcs)
        {
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandIdCson2Json = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.RunCson2Json);
            OleMenuCommand menuCommandCson2Json = new OleMenuCommand((s, e) => RunCson2Json(), commandIdCson2Json);
            menuCommandCson2Json.BeforeQueryStatus += menuCommand_BeforeQueryStatus_Cson2Json;
            _mcs.AddCommand(menuCommandCson2Json);

            CommandID commandIdJson2Cson = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.RunJson2Cson);
            OleMenuCommand menuCommandJson2Cson = new OleMenuCommand((s, e) => RunJson2Cson(), commandIdJson2Cson);
            menuCommandJson2Cson.BeforeQueryStatus += menuCommand_BeforeQueryStatus_Json2Cson;
            _mcs.AddCommand(menuCommandJson2Cson);
        }

        private List<string> csonFiles, jsonFiles;

        void menuCommand_BeforeQueryStatus_Cson2Json(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            csonFiles = ProjectHelpers.GetSelectedFilePaths()
                    .Where(f => f.EndsWith(".cson", StringComparison.OrdinalIgnoreCase)).ToList();

            menuCommand.Enabled = csonFiles.Count > 0;
        }

        void menuCommand_BeforeQueryStatus_Json2Cson(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            jsonFiles = ProjectHelpers.GetSelectedFilePaths()
                    .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).ToList();

            menuCommand.Enabled = jsonFiles.Count > 0;
        }

        private static string GetTargetPath(string file)
        {
            if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                return file + ".cson";

            return file + ".json";
        }

        private void RunCson2Json()
        {
            foreach (string file in csonFiles)
            {
                new NodeCompilerRunner(Mef.GetContentType(CsonContentTypeDefinition.CsonContentType))
                   .CompileAsync(file, GetTargetPath(file))
                   .DoNotWait("generating JSON from " + file);
            }
        }

        private void RunJson2Cson()
        {
            foreach (string file in jsonFiles)
            {
                new NodeCompilerRunner(Mef.GetContentType(CsonContentTypeDefinition.CsonContentType))
                   .CompileAsync(file, GetTargetPath(file))
                   .DoNotWait("generating CSON from " + file);
            }
        }
    }
}
