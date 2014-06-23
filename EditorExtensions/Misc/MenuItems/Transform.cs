using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

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

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public void SetupCommands()
        {
            SetupCommand(CommandId.TitleCaseTransform, new Replacement(x => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(x)));
            SetupCommand(CommandId.ReverseTransform, new Replacement(x => new string(x.Reverse().ToArray())));
            SetupCommand(CommandId.NormalizeTransform, new Replacement(x => RemoveDiacritics(x)));
            SetupCommand(CommandId.MD5Transform, new Replacement(x => Hash(x, new MD5CryptoServiceProvider())));
            SetupCommand(CommandId.SHA1Transform, new Replacement(x => Hash(x, new SHA1CryptoServiceProvider())));
            SetupCommand(CommandId.SHA256Transform, new Replacement(x => Hash(x, new SHA256CryptoServiceProvider())));
            SetupCommand(CommandId.SHA384Transform, new Replacement(x => Hash(x, new SHA384CryptoServiceProvider())));
            SetupCommand(CommandId.SHA512Transform, new Replacement(x => Hash(x, new SHA512CryptoServiceProvider())));
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
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture).ToLowerInvariant());
            }

            return sb.ToString();
        }

        private void SetupCommand(CommandId command, Replacement callback)
        {
            CommandID commandId = new CommandID(CommandGuids.guidEditorExtensionsCmdSet, (int)command);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => Replace(callback), commandId);

            menuCommand.BeforeQueryStatus += (s, e) =>
            {
                var document = GetTextDocument();

                if (document == null)
                    return;

                string selection = document.Selection.Text;
                menuCommand.Enabled = selection.Length > 0 && callback(selection) != selection;
            };

            _mcs.AddCommand(menuCommand);
        }

        private TextDocument GetTextDocument()
        {
            if (_dte.ActiveDocument != null)
                return _dte.ActiveDocument.Object("TextDocument") as TextDocument;

            return null;
        }

        private void Replace(Replacement callback)
        {
            TextDocument document = GetTextDocument();

            if (document == null)
                return;

            string replacement = callback(document.Selection.Text);

            using (WebEssentialsPackage.UndoContext((callback.Method.Name)))
                document.Selection.Insert(replacement, 0);
        }
    }
}