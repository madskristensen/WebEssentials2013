using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    public class IntellisenseObject
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public bool IsEnum { get; set; }
        public string Summary { get; set; }
        public IList<IntellisenseProperty> Properties { get; private set; }
        public IList<string> References { get; private set; }

        public IntellisenseObject()
        {
            Properties = new List<IntellisenseProperty>();
            References = new List<string>();
        }

        public IntellisenseObject(IList<IntellisenseProperty> properties)
        {
            Properties = properties;
        }

        public IntellisenseObject(IList<IntellisenseProperty> properties, IList<string> references)
        {
            Properties = properties;
            References = references;
        }
    }
}