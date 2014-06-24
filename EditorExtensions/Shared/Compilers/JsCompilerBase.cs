using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    ///<summary>A base class for a compiler that rewrites CSS source maps.</summary>
    public abstract class JsCompilerBase : NodeExecutorBase
    {
        public override string TargetExtension { get { return ".js"; } }

        protected async override Task MoveOutputContentToCorrectTarget(string targetFileName)
        {
            if (!targetFileName.EndsWith(".min.js", System.StringComparison.OrdinalIgnoreCase))
                return;

            var tempName = targetFileName.Replace(".min.js", ".js");

            if (!File.Exists(tempName))
                return;

            string newContent = await FileHelpers.ReadAllTextRetry(tempName);

            if (!File.Exists(targetFileName) || !ReferenceEquals(string.Intern(newContent), string.Intern(await FileHelpers.ReadAllTextRetry(targetFileName))))
                await FileHelpers.WriteAllTextRetry(targetFileName, newContent);
        }
    }
}
