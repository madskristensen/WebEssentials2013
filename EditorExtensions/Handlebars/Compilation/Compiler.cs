using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using MadsKristensen.EditorExtensions.RtlCss;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Handlebars.Compilation
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType(HandlebarsContentTypeDefinition.HandlebarsContentType)]
    public class HandlebarsCompiler : NodeExecutorBase
    {
        public override string TargetExtension { get { return ".hbs.js"; } }
        public override bool MinifyInPlace
        {
            get { return WESettings.Instance.Handlebars.MinifyInPlace; }
        }

        public override bool GenerateSourceMap
        {
            get { return false; }
        }

        public override string ServiceName
        {
            get { return "HANDLEBARS"; }
        }

        protected override string GetPath(string sourceFileName, string targetFileName)
        {

            var parameters = new NodeServerUtilities.Parameters();

            parameters.Add("service", ServiceName);
            parameters.Add("sourceFileName", sourceFileName);
            parameters.Add("targetFileName", targetFileName);
            parameters.Add("compiledTemplateName", Path.GetFileNameWithoutExtension(sourceFileName));

            return parameters.FlattenParameters();
        }
    }
}
