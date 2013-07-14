
namespace MadsKristensen.EditorExtensions
{
    static class PkgCmdIDList
    {
        public const uint myCommand = 0x100;
        public const uint htmlEncode = 0x102;
        public const uint htmlDecode = 0x103;
        public const uint urlEncode = 0x106;
        public const uint urlDecode = 0x107;
        public const uint jsEncode = 0x108;
        public const uint attrEncode = 0x109;
        public const uint upperCaseTransform = 0x111;
        public const uint lowerCaseTransform = 0x114;
        public const uint titleCaseTransform = 0x115;
        public const uint reverseTransform = 0x116;
        public const uint normalizeTransform = 0x118;
        public const uint md5Transform = 0x120;
        public const uint sha1Transform = 0x121;
        public const uint sha256Transform = 0x122;
        public const uint sha384Transform = 0x123;
        public const uint sha512Transform = 0x124;
        public const uint sortCssProperties = 0x125;
        public const uint addMissingVendor = 0x127;
        public const uint addMissingStandard = 0x128;
        public const uint cssRemoveDuplicates = 0x129;
        public const uint cssHideUnsupported = 0x1033;
        public const uint cssHideInheritInitial = 0x1035;
        public const uint addNewFeature = 0x334;
        public const uint SurroundWith = 0x334;
        public const uint ExpandSelection = 0x335;
        public const uint ContractSelection = 0x336;
        public const uint cmdDiff = 0x1041;
        public const uint cmdJsHint = 0x1042;
        public const uint cmdProjectSettings = 0x1043;
        public const uint cmdSolutionSettings = 0x1044;
        public const uint cmdSolutionColors = 0x1045;
        public const uint cmdMarkdownStylesheet = 0x1046;
        public const uint CssIntellisenseSubMenu = 0x1031;
        public const uint MinifyCss = 0x1051;
        public const uint MinifyJs = 0x1052;
        public const uint MinifySelection = 0x1053;
        public const uint ExtractSelection = 0x1054;
        public const uint SelectBrowsers = 0x1055;
        public const uint ExtractVariable = 0x1056;
        public const uint ExtractMixin = 0x1057;
        public const uint BundleCss = 0x1071;
        public const uint BundleJs = 0x1072;

        // Lines
        public const uint SortAsc = 0x0003;
        public const uint SortDesc = 0x0004;
        public const uint RemoveDuplicateLines = 0x0005;
        public const uint RemoveEmptyLines = 0x0007;

        // Build
        public const uint cmdBuildBundles = 0x1083;
        public const uint cmdBuildLess = 0x1084;
        //public const uint cmdBuildTypeScript = 0x1085;
        public const uint cmdBuildMinify = 0x1086;
        public const uint cmdBuildCoffeeScript = 0x1087;
    };
}
