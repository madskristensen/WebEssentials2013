using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    internal static class NodeServerUtilities
    {
        internal class Parameters
        {
            public Dictionary<string, string> UriComponentsDictionary { get; private set; }

            public Parameters()
            {
                UriComponentsDictionary = new Dictionary<string, string>();
            }

            public string FlattenParameters()
            {
                StringBuilder parametersString = new StringBuilder();

                foreach (KeyValuePair<string, string> pair in UriComponentsDictionary)
                {
                    parametersString.Append(pair.Key);

                    if (!string.IsNullOrEmpty(pair.Value))
                        parametersString.Append("=").Append(pair.Value);

                    parametersString.Append("&");
                }

                return parametersString.ToString();
            }

            public void Add(string key, string value = "")
            {
                UriComponentsDictionary.Add(key, value);
            }
        }

        internal class Response
        {
            public string SourceFileName { get; set; }
            public string TargetFileName { get; set; }
            public string MapFileName { get; set; }
            public bool Success { get; set; }
            public string Remarks { get; set; }
            public string Details { get; set; }
            public IEnumerable<CompilerError> Errors { get; set; }
            public string Content { get; set; }
            public string Map { get; set; }

            // RTL variants
            public string RtlSourceFileName { get; set; }
            public string RtlTargetFileName { get; set; }
            public string RtlMapFileName { get; set; }
            public string RtlContent { get; set; }
            public string RtlMap { get; set; }

            internal Response()
            {
                SourceFileName = TargetFileName = MapFileName = Remarks = Details = Content = Map
                               = RtlSourceFileName = RtlTargetFileName = RtlMapFileName = RtlContent = RtlMap = "";
            }

            internal async Task<CompilerResult> GetCompilerResult()
            {
                return await CompilerResultFactory.GenerateResult(SourceFileName, TargetFileName, MapFileName, Success, Content, Map, Errors, false,
                                                                  RtlSourceFileName, RtlTargetFileName, RtlMapFileName, RtlContent, RtlMap);
            }
        }
    }
}
