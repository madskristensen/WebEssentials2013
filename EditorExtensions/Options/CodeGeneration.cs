using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class CodeGenerationOptions : DialogPage
    {
        public CodeGenerationOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.JavaScriptCamelCasePropertyNames, JavaScriptCamelCasePropertyNames);
            Settings.SetValue(WESettings.Keys.JavaScriptCamelCaseClassNames, JavaScriptCamelCaseClassNames);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            JavaScriptCamelCasePropertyNames = WESettings.GetBoolean(WESettings.Keys.JavaScriptCamelCasePropertyNames);
            JavaScriptCamelCaseClassNames = WESettings.GetBoolean(WESettings.Keys.JavaScriptCamelCaseClassNames);
        }

        [LocDisplayName("Camel Casing Property Names")]
        [Description("When generating an Intellisense File for JavaScript or TypeScript the property names will generated in camelCase instead of PascalCase")]
        [Category("Intellisense Generation")]
        public bool JavaScriptCamelCasePropertyNames { get; set; }

        [LocDisplayName("Camel Casing of 'Class' Names")]
        [Description("When generating an Intellisense File for JavaScript or TypeSript the interface or function names will generated in camelCase instead of PascalCase")]
        [Category("Intellisense Generation")]
        public bool JavaScriptCamelCaseClassNames { get; set; }


    }
}
