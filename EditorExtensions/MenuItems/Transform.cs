using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class TransformMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;
        private delegate string Replacement(string original);

        public TransformMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            SetupCommand(PkgCmdIDList.titleCaseTransform, new Replacement(x => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(x)));
            SetupCommand(PkgCmdIDList.reverseTransform, new Replacement(x => new string(x.Reverse().ToArray())));
            SetupCommand(PkgCmdIDList.normalizeTransform, new Replacement(x => RemoveDiacritics(x)));
                using (var md5Hash = new MD5CryptoServiceProvider())
                {
                    SetupCommand(PkgCmdIDList.md5Transform, new Replacement(x => Hash(x, md5Hash)));
                }
                using (var sha1Hash = new MD5CryptoServiceProvider())
                {
                    SetupCommand(PkgCmdIDList.sha1Transform, new Replacement(x => Hash(x, sha1Hash)));
                }
                using (var sha256Hash = new MD5CryptoServiceProvider())
                {
                    SetupCommand(PkgCmdIDList.sha256Transform, new Replacement(x => Hash(x, sha256Hash)));
                }
                using (var sha384Hash = new MD5CryptoServiceProvider())
                {
                    SetupCommand(PkgCmdIDList.sha384Transform, new Replacement(x => Hash(x, sha384Hash)));
                }
                using (var sha512Hash = new MD5CryptoServiceProvider())
                {
                    SetupCommand(PkgCmdIDList.sha512Transform, new Replacement(x => Hash(x, sha512Hash)));
                }
        }

        public static string RemoveDiacritics(string s)
        {
            string stFormD = s.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            for (int ich = 0; ich < stFormD.Length; ich++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(stFormD[ich]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(stFormD[ich]);
                }
            }

            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }

        private static string Hash(string original, HashAlgorithm algorithm)
        {
            byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(original));            
            algorithm.Dispose();

            StringBuilder sb = new StringBuilder();
            
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2", CultureInfo.CurrentCulture).ToLowerInvariant());
            }

            return sb.ToString();
        }

        private void SetupCommand(uint command, Replacement callback)
        {
            CommandID commandId = new CommandID(GuidList.guidEditorExtensionsCmdSet, (int)command);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => Replace(callback), commandId);

            menuCommand.BeforeQueryStatus += (s, e) =>
            {
                string selection = GetTextDocument().Selection.Text;
                menuCommand.Enabled = selection.Length > 0 && callback(selection) != selection;
            };

            _mcs.AddCommand(menuCommand);
        }

        private TextDocument GetTextDocument()
        {
            return _dte.ActiveDocument.Object("TextDocument") as TextDocument;
        }

        private void Replace(Replacement callback)
        {
            TextDocument document = GetTextDocument();
            string replacement = callback(document.Selection.Text);

            _dte.UndoContext.Open(callback.Method.Name);
            document.Selection.Insert(replacement, 0);
            _dte.UndoContext.Close();
        }
    }
}