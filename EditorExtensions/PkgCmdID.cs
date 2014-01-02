
namespace MadsKristensen.EditorExtensions
{
    enum PkgCmdIDList
    {
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
        CreateSolutionSettings = 0x1044,
        CreateSolutionColorPalete = 0x1045,
        CreateMarkdownStylesheet = 0x1046,
        CreateJavaScriptIntellisenseFile = 0x1047,
        CreateTypeScriptIntellisenseFile = 0x1048,
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
        BuildMinify = 0x1086,
        BuildCoffeeScript = 0x1087,
        BuildIcedCoffeeScript = 0x1088,

        //Unused CSS
        UnusedCssSnapshot = 0x2100,
        UnusedCssReset = 0x2101,
        UnusedCssRecordAll = 0x2102,
        UnusedCssStopRecordAll = 0x2103,

        //Pixel Pushing
        PixelPushingToggle = 0x2200
    }
}
