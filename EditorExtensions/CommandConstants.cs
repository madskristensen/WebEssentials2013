using System;

namespace MadsKristensen.EditorExtensions
{
    static class CommandGuids
    {
        public const string guidEditorExtensionsPkgString = "5fb7364d-2e8c-44a4-95eb-2a382e30fec8";
        public const string guidEditorExtensionsCmdSetString = "e396b698-e00e-444b-9f5f-3dcb1ef74e41";
        public const string guidCssCmdSetString = "e396b698-e00e-444b-9f5f-3dcb1ef74e50";
        public const string guidCssIntellisensCmdSetString = "e396b698-e00e-444b-9f5f-3dcb1ef74e51";
        public const string guidDiffCmdSetString = "e396b698-e00e-444b-9f5f-3dcb1ef74e59";
        public const string guidMinifyCmdSetString = "e396b698-e00e-444b-9f5f-3dcb1ef74e61";
        public const string guidBundleCmdSetString = "e396b698-e00e-444b-9f5f-3dcb1ef74e63";
        public const string guidExtractCmdSetString = "e396b698-e00e-444b-9f5f-3dcb1ef74e64";
        public const string guidBuildCmdSetString = "e396b698-e00e-444b-9f5f-3dcb1ef74e65";
        public const string guidFormattingCmdSetString = "e396b698-e00e-444b-9f5f-3dcb1ef74e66";
        public const string guidEditorLinesCmdSetString = "e396b698-e00e-444b-9f5f-3dcb1ef74e67";
        public const string guidImageCmdSetString = "e396b698-e00e-444b-9f5f-3dcb1ef74e68";
        public const string guidUnusedCssCmdSetString = "47BA41E6-C7AB-49F1-984A-30E672AFF9FC";
        public const string guidPixelPushingCmdSetString = "EE755B3C-F6ED-414B-86BA-1AADB7DAE216";
        public const string guidTopMenuString = "{30947ebe-9147-45f9-96cf-401bfc671a01}";

        public static readonly Guid guidEditorExtensionsCmdSet = new Guid(guidEditorExtensionsCmdSetString);
        public static readonly Guid guidCssCmdSet = new Guid(guidCssCmdSetString);
        public static readonly Guid guidCssIntellisenseCmdSet = new Guid(guidCssIntellisensCmdSetString);
        public static readonly Guid guidDiffCmdSet = new Guid(guidDiffCmdSetString);
        public static readonly Guid guidMinifyCmdSet = new Guid(guidMinifyCmdSetString);
        public static readonly Guid guidBundleCmdSet = new Guid(guidBundleCmdSetString);
        public static readonly Guid guidExtractCmdSet = new Guid(guidExtractCmdSetString);
        public static readonly Guid guidBuildCmdSet = new Guid(guidBuildCmdSetString);
        public static readonly Guid guidFormattingCmdSet = new Guid(guidFormattingCmdSetString);
        public static readonly Guid guidEditorLinesCmdSet = new Guid(guidEditorLinesCmdSetString);
        public static readonly Guid guidImageCmdSet = new Guid(guidImageCmdSetString);
        public static readonly Guid guidUnusedCssCmdSet = new Guid(guidUnusedCssCmdSetString);
        public static readonly Guid guidPixelPushingCmdSet = new Guid(guidPixelPushingCmdSetString);
        public static readonly Guid guidTopMenu = new Guid(guidTopMenuString);
    }

    enum CommandId
    {
        TopMenu = 0x3001,
        HtmlEncode = 0x102,
        HtmlDecode = 0x103,
        UrlEncode = 0x106,
        UrlDecode = 0x107,
        JsEncode = 0x108,
        AttrEncode = 0x109,
        TitleCaseTransform = 0x115,
        ReverseTransform = 0x116,
        NormalizeTransform = 0x118,
        MD5Transform = 0x120,
        SHA1Transform = 0x121,
        SHA256Transform = 0x122,
        SHA384Transform = 0x123,
        SHA512Transform = 0x124,
        SortCssProperties = 0x125,
        AddMissingVendor = 0x127,
        AddMissingStandard = 0x128,
        CssRemoveDuplicates = 0x129,
        CssHideUnsupported = 0x1033,
        CssHideInheritInitial = 0x1035,
        SurroundWith = 0x334,
        ExpandSelection = 0x335,
        ContractSelection = 0x336,
        RunDiff = 0x1041,
        RunJsHint = 0x1042,
        RunTsLint = 0x1092,
        CreateSolutionSettings = 0x1044,
        CreateSolutionColorPalete = 0x1045,
        CreateMarkdownStylesheet = 0x1046,
        CreateJavaScriptIntellisenseFile = 0x1047,
        CreateTypeScriptIntellisenseFile = 0x1048,
        MarkdownCompile = 0x1039,
        EditGlobalJsHint = 0x1038,
        EditGlobalTsLint = 0x1098,
        CssIntellisenseSubMenu = 0x1031,
        MinifyCss = 0x1051,
        MinifyJs = 0x1052,
        MinifyHtml = 0x1058,
        MinifySelection = 0x1053,
        ExtractSelection = 0x1054,
        SelectBrowsers = 0x1055,
        ExtractVariable = 0x1056,
        ExtractMixin = 0x1057,
        BundleCss = 0x1071,
        BundleJs = 0x1072,
        BundleHtml = 0x1074,
        ReferenceJs = 0x333,

        // Lines
        SortAsc = 0x0003,
        SortDesc = 0x0004,
        RemoveDuplicateLines = 0x0005,
        RemoveEmptyLines = 0x0007,

        // Build
        BuildBundles = 0x1083,
        BuildLess = 0x1084,
        BuildSass = 0x1085,
        BuildMinify = 0x1086,
        BuildCoffeeScript = 0x1087,

        // Images
        CompressImage = 0x1091,
        SpriteImage = 0x1093,

        //Unused CSS
        UnusedCssSnapshot = 0x2100,
        UnusedCssReset = 0x2101,
        UnusedCssRecordAll = 0x2102,
        UnusedCssStopRecordAll = 0x2103,

        //Pixel Pushing
        PixelPushingToggle = 0x2200
    }
}
