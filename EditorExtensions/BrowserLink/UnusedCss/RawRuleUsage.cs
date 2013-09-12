using System.Collections.Generic;
using Newtonsoft.Json;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class RawRuleUsage
    {
        [JsonProperty]
        public string Selector { get; set; }

        [JsonProperty]
        public List<SourceLocation> SourceLocations { get; set; }
    }
}