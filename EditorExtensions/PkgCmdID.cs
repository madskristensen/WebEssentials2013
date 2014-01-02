
namespace MadsKristensen.EditorExtensions
{
    enum PkgCmdIDList
    {
        myCommand = 0x100,
        htmlEncode = 0x102,
        htmlDecode = 0x103,
        urlEncode = 0x106,
        urlDecode = 0x107,
        jsEncode = 0x108,
        attrEncode = 0x109,
        titleCaseTransform = 0x115,
        reverseTransform = 0x116,
        normalizeTransform = 0x118,
        md5Transform = 0x120,
        sha1Transform = 0x121,
        sha256Transform = 0x122,
        sha384Transform = 0x123,
        sha512Transform = 0x124,
        sortCssProperties = 0x125,
        addMissingVendor = 0x127,
        addMissingStandard = 0x128,
        cssRemoveDuplicates = 0x129,
        cssHideUnsupported = 0x1033,
        cssHideInheritInitial = 0x1035,
        addNewFeature = 0x334,
        SurroundWith = 0x334,
        ExpandSelection = 0x335,
        ContractSelection = 0x336,
        cmdDiff = 0x1041,
        cmdJsHint = 0x1042,
        cmdProjectSettings = 0x1043,
        cmdSolutionSettings = 0x1044,
        cmdSolutionColors = 0x1045,
        cmdMarkdownStylesheet = 0x1046,
        cmdJavaScriptIntellisense = 0x1047,
        cmdTypeScriptIntellisense = 0x1048,
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
        cmdBuildBundles = 0x1083,
        cmdBuildLess = 0x1084,
        // cmdBuildTypeScript = 0x1085,
        cmdBuildMinify = 0x1086,
        cmdBuildCoffeeScript = 0x1087,
        cmdBuildIcedCoffeeScript = 0x1088,

        //Unused CSS
        cmdUnusedCssSnapshotCommandId = 0x2100,
        cmdUnusedCssResetCommandId = 0x2101,
        cmdUnusedCssRecordAllCommandId = 0x2102,
        cmdUnusedCssStopRecordAllCommandId = 0x2103,

        //Pixel Pushing
        cmdPixelPushingToggleCommandId = 0x2200
    }
}
