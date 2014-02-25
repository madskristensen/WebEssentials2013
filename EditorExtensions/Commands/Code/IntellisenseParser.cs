using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSharp")]
    [ContentType("VisualBasic")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class IntellisenseParser : IWpfTextViewCreationListener
    {
        private const string DefaultModuleName = "server";
        private const string ModuleNameAttributeName = "TypeScriptModule";
        private static readonly Regex IsNumber = new Regex("^[0-9a-fx]+[ul]{0,2}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static class Ext
        {
            public const string JavaScript = ".js";
            public const string TypeScript = ".d.ts";
        }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        private ITextDocument _document;

        public void TextViewCreated(IWpfTextView textView)
        {
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out _document))
            {
                _document.FileActionOccurred += document_FileActionOccurred;
            }
        }

        private void document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            ITextDocument document = (ITextDocument)sender;

            if (document.TextBuffer == null || e.FileActionType != FileActionTypes.ContentSavedToDisk)
                return;

            Process(e.FilePath);
        }

        private static Task<bool> Process(string filePath)
        {
            if (!File.Exists(filePath + Ext.JavaScript) && !File.Exists(filePath + Ext.TypeScript))
                return Task.FromResult(false);

            return Dispatcher.CurrentDispatcher.InvokeAsync(new Func<bool>(() =>
            {
                var item = ProjectHelpers.GetProjectItem(filePath);

                if (item == null)
                    return false;

                List<IntellisenseObject> list = null;

                try
                {
                    list = ProcessFile(item);
                }
                catch (Exception ex)
                {
                    Logger.Log("An error occurred while processing code in " + filePath + "\n" + ex
                             + "\n\nPlease report this bug at https://github.com/madskristensen/WebEssentials2013/issues, and include the source of the file.");
                }

                if (list == null)
                    return false;

                AddScript(filePath, Ext.JavaScript, list);
                AddScript(filePath, Ext.TypeScript, list);

                return true;
            }), DispatcherPriority.ApplicationIdle).Task;
        }

        private static void AddScript(string filePath, string extension, IEnumerable<IntellisenseObject> list)
        {
            string resultPath = filePath + extension;

            if (!File.Exists(resultPath))
                return;

            IntellisenseWriter.Write(list, resultPath);

            var item = ProjectHelpers.AddFileToProject(filePath, resultPath);

            if (extension.Equals(Ext.TypeScript, StringComparison.OrdinalIgnoreCase))
                item.Properties.Item("ItemType").Value = "TypeScriptCompile";
            else
            {
                item.Properties.Item("ItemType").Value = "None";
            }
        }

        internal static List<IntellisenseObject> ProcessFile(ProjectItem item)
        {
            if (item.FileCodeModel == null)
                return null;

            List<IntellisenseObject> list = new List<IntellisenseObject>();

            foreach (CodeElement element in item.FileCodeModel.CodeElements)
            {
                if (element.Kind == vsCMElement.vsCMElementNamespace)
                {
                    CodeNamespace cn = (CodeNamespace)element;
                    foreach (CodeElement member in cn.Members)
                    {
                        if (ShouldProcess(member))
                        {
                            ProcessElement(member, list);
                        }
                    }
                }
                else if (ShouldProcess(element))
                {
                    ProcessElement(element, list);
                }
            }

            return list;
        }

        private static void ProcessElement(CodeElement element, List<IntellisenseObject> list)
        {
            if (element.Kind == vsCMElement.vsCMElementEnum)
            {
                ProcessEnum((CodeEnum)element, list);
            }
            else if (element.Kind == vsCMElement.vsCMElementClass)
            {
                ProcessClass((CodeClass)element, list);
            }
        }

        private static bool ShouldProcess(CodeElement member)
        {
            return
                    member.Kind == vsCMElement.vsCMElementClass
                    || member.Kind == vsCMElement.vsCMElementEnum;
        }

        private static void ProcessEnum(CodeEnum element, List<IntellisenseObject> list)
        {
            IntellisenseObject data = new IntellisenseObject
            {
                Name = element.Name,
                IsEnum = element.Kind == vsCMElement.vsCMElementEnum,
                FullName = element.FullName,
                Namespace = GetNamespace(element),
                Summary = GetSummary(element)
            };

            foreach (var codeEnum in element.Members.OfType<CodeVariable>())
            {
                var prop = new IntellisenseProperty
                {
                    Name = codeEnum.Name,
                    Summary = GetSummary(codeEnum),
                    InitExpression = GetInitializer(codeEnum.InitExpression)
                };

                data.Properties.Add(prop);
            }

            if (data.Properties.Count > 0)
                list.Add(data);
        }

        private static void ProcessClass(CodeClass cc, List<IntellisenseObject> list)
        {
            var references = new HashSet<string>();
            var properties = GetProperties(cc.Members, new HashSet<string>(), references).ToList();
            var dataContractAttribute = cc.Attributes.Cast<CodeAttribute>().Where(a => a.Name == "DataContract");
            string className = cc.Name;
            string nsName = GetNamespace(cc);

            if (dataContractAttribute.Any())
            {
                var keyValues = dataContractAttribute.First().Children.OfType<CodeAttributeArgument>()
                               .ToDictionary(a => a.Name, a => (a.Value ?? "").Trim('\"', '\''));

                if (keyValues.ContainsKey("Name"))
                    className = keyValues["Name"];

                if (keyValues.ContainsKey("Namespace"))
                    nsName = keyValues["Namespace"];
            }

            if (properties.Any())
            {
                var intellisenseObject = new IntellisenseObject(properties, references.ToList())
                {
                    Namespace = nsName,
                    Name = className,
                    FullName = cc.FullName,
                    Summary = GetSummary(cc)
                };

                list.Add(intellisenseObject);
            }
        }

        private static IEnumerable<IntellisenseProperty> GetProperties(CodeElements props, HashSet<string> traversedTypes, HashSet<string> references = null)
        {
            return from p in props.OfType<CodeProperty>()
                   where !p.Attributes.Cast<CodeAttribute>().Any(a => a.Name == "IgnoreDataMember")
                   where p.Getter != null && !p.Getter.IsShared && p.Getter.Access == vsCMAccess.vsCMAccessPublic
                   select new IntellisenseProperty
                   {
                       Name = GetName(p),
                       Type = GetType(p.Parent, p.Type, traversedTypes, references),
                       Summary = GetSummary(p)
                   };
        }

        private static string GetNamespace(CodeClass e) { return GetNamespace(e.Attributes); }
        private static string GetNamespace(CodeEnum e) { return GetNamespace(e.Attributes); }

        private static string GetNamespace(CodeElements attrs)
        {
            if (attrs == null) return DefaultModuleName;
            var namespaceFromAttr = from a in attrs.Cast<CodeAttribute2>()
                                    where a.Name.EndsWith(ModuleNameAttributeName, StringComparison.OrdinalIgnoreCase)
                                    from arg in a.Arguments.Cast<CodeAttributeArgument>()
                                    let v = (arg.Value ?? "").Trim('\"')
                                    where !string.IsNullOrWhiteSpace(v)
                                    select v;

            return namespaceFromAttr.FirstOrDefault() ?? DefaultModuleName;
        }

        private static IntellisenseType GetType(CodeClass rootElement, CodeTypeRef codeTypeRef, HashSet<string> traversedTypes, HashSet<string> references)
        {
            var isArray = codeTypeRef.TypeKind == vsCMTypeRef.vsCMTypeRefArray;
            var isCollection = codeTypeRef.AsString.StartsWith("System.Collections", StringComparison.Ordinal);

            var effectiveTypeRef = codeTypeRef;
            if (isArray && codeTypeRef.ElementType != null) effectiveTypeRef = effectiveTypeRef.ElementType;
            else if (isCollection) effectiveTypeRef = TryToGuessGenericArgument(rootElement, effectiveTypeRef);

            var codeClass = effectiveTypeRef.CodeType as CodeClass2;
            var codeEnum = effectiveTypeRef.CodeType as CodeEnum;
            var isPrimitive = IsPrimitive(effectiveTypeRef);

            var result = new IntellisenseType
            {
                IsArray = isArray || isCollection,
                CodeName = effectiveTypeRef.AsString,
                ClientSideReferenceName =
                    effectiveTypeRef.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType &&
                    effectiveTypeRef.CodeType.InfoLocation == vsCMInfoLocation.vsCMInfoLocationProject
                    ?
                        (codeClass != null && HasIntellisense(codeClass.ProjectItem, Ext.TypeScript, references) ? (GetNamespace(codeClass) + "." + codeClass.Name) : null) ??
                        (codeEnum != null && HasIntellisense(codeEnum.ProjectItem, Ext.TypeScript, references) ? (GetNamespace(codeEnum) + "." + codeEnum.Name) : null)
                    : null
            };

            if (!isPrimitive && codeClass != null && !traversedTypes.Contains(effectiveTypeRef.CodeType.FullName) && !isCollection)
            {
                traversedTypes.Add(effectiveTypeRef.CodeType.FullName);
                result.Shape = GetProperties(effectiveTypeRef.CodeType.Members, traversedTypes, references).ToList();
                traversedTypes.Remove(effectiveTypeRef.CodeType.FullName);
            }

            return result;
        }

        private static CodeTypeRef TryToGuessGenericArgument(CodeClass rootElement, CodeTypeRef codeTypeRef)
        {
            var codeTypeRef2 = codeTypeRef as CodeTypeRef2;
            if (codeTypeRef2 == null || !codeTypeRef2.IsGeneric) return codeTypeRef;

            // There is no way to extract generic parameter as CodeTypeRef or something similar
            // (see http://social.msdn.microsoft.com/Forums/vstudio/en-US/09504bdc-2b81-405a-a2f7-158fb721ee90/envdte-envdte80-codetyperef2-and-generic-types?forum=vsx)
            // but we can make it work at least for some simple case with the following heuristic:
            //  1) get the argument's local name by parsing the type reference's full text
            //  2) if it's a known primitive (i.e. string, int, etc.), return that
            //  3) otherwise, guess that it's a type from the same namespace and same project,
            //     and use the project CodeModel to retrieve it by full name
            //  4) if CodeModel returns null - well, bad luck, don't have any more guesses
            var typeNameAsInCode = codeTypeRef2.AsString.Split('<', '>').ElementAtOrDefault(1) ?? "";
            CodeModel projCodeModel;

            try
            {
                projCodeModel = rootElement.ProjectItem.ContainingProject.CodeModel;
            }
            catch (COMException)
            {
                projCodeModel = ProjectHelpers.GetActiveProject().CodeModel;
            }

            var codeType = projCodeModel.CodeTypeFromFullName(TryToGuessFullName(typeNameAsInCode));

            if (codeType != null) return projCodeModel.CreateCodeTypeRef(codeType);
            return codeTypeRef;
        }

        private static readonly Dictionary<string, Type> _knownPrimitiveTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase) {
            { "string", typeof( string ) },
            { "int", typeof( int ) },
            { "long", typeof( long ) },
            { "short", typeof( short ) },
            { "byte", typeof( byte ) },
            { "uint", typeof( uint ) },
            { "ulong", typeof( ulong ) },
            { "ushort", typeof( ushort ) },
            { "sbyte", typeof( sbyte ) },
            { "float", typeof( float ) },
            { "double", typeof( double ) },
            { "decimal", typeof( decimal ) },
        };

        private static string TryToGuessFullName(string typeName)
        {
            Type primitiveType;
            if (_knownPrimitiveTypes.TryGetValue(typeName, out primitiveType)) return primitiveType.FullName;
            else return typeName;
        }

        private static bool IsPrimitive(CodeTypeRef codeTypeRef)
        {
            if (codeTypeRef.TypeKind != vsCMTypeRef.vsCMTypeRefOther && codeTypeRef.TypeKind != vsCMTypeRef.vsCMTypeRefCodeType)
                return true;

            if (codeTypeRef.AsString.EndsWith("DateTime", StringComparison.Ordinal))
                return true;

            return false;
        }

        private static bool HasIntellisense(ProjectItem projectItem, string ext, HashSet<string> references)
        {
            for (short i = 0; i < projectItem.FileCount; i++)
            {
                var fileName = projectItem.FileNames[i] + ext;

                if (File.Exists(fileName))
                {
                    references.Add(fileName);
                    return true;
                }
            }
            return false;
        }

        // Maps attribute name to array of attribute properties to get resultant name from
        private static readonly IReadOnlyDictionary<string, string[]> nameAttributes = new Dictionary<string, string[]>
        {
            { "DataMember", new [] { "Name" } },
            { "JsonProperty", new [] { "", "PropertyName" } }
        };
        private static string GetName(CodeProperty property)
        {
            foreach (CodeAttribute attr in property.Attributes)
            {
                var className = Path.GetExtension(attr.Name);
                if (string.IsNullOrEmpty(className)) className = attr.Name;

                string[] argumentNames;
                if (!nameAttributes.TryGetValue(className, out argumentNames))
                    continue;

                var value = attr.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => argumentNames.Contains(a.Name));

                if (value == null)
                    break;

                // Strip the leading & trailing quotes
                return value.Value.Substring(1, value.Value.Length - 2);
            }

            return property.Name;
        }

        // External items throw an exception from the DocComment getter
        private static string GetSummary(CodeProperty property) { return property.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject ? null : GetSummary(property.InfoLocation, property.DocComment, property.Comment, property.FullName); }

        private static string GetSummary(CodeClass property) { return GetSummary(property.InfoLocation, property.DocComment, property.Comment, property.FullName); }

        private static string GetSummary(CodeEnum property) { return GetSummary(property.InfoLocation, property.DocComment, property.Comment, property.FullName); }

        private static string GetSummary(CodeVariable property) { return GetSummary(property.InfoLocation, property.DocComment, property.Comment, property.FullName); }

        private static string GetSummary(vsCMInfoLocation location, string xmlComment, string inlineComment, string fullName)
        {
            if (location != vsCMInfoLocation.vsCMInfoLocationProject || (string.IsNullOrWhiteSpace(xmlComment) && string.IsNullOrWhiteSpace(inlineComment)))
                return null;

            try
            {
                string summary = XElement.Parse(xmlComment)
                               .Descendants("summary")
                               .Select(x => x.Value)
                               .FirstOrDefault();
                if (!string.IsNullOrEmpty(summary)) return summary.Trim();
                if (!string.IsNullOrWhiteSpace(inlineComment)) return inlineComment.Trim();
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log("Couldn't parse XML Doc Comment for " + fullName + ":\n" + ex);
                return null;
            }
        }

        private static string GetInitializer(object initExpression)
        {
            if (initExpression != null)
            {
                string initializer = initExpression.ToString();
                if (IsNumber.IsMatch(initializer)) return initializer;
            }
            return null;
        }
    }
}
