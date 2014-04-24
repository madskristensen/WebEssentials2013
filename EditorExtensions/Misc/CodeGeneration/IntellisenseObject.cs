using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    public class IntellisenseObject : IEquatable<IntellisenseObject>
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string BaseNamespace { get; set; }
        public string BaseName { get; set; }
        public string FullName { get; set; }
        public bool IsEnum { get; set; }
        public string Summary { get; set; }
        public IList<IntellisenseProperty> Properties { get; private set; }
        public HashSet<string> References { get; private set; }

        public IntellisenseObject()
        {
            Properties = new List<IntellisenseProperty>();
            References = new HashSet<string>();
        }

        public IntellisenseObject(IList<IntellisenseProperty> properties)
        {
            Properties = properties;
        }

        public IntellisenseObject(IList<IntellisenseProperty> properties, HashSet<string> references)
        {
            Properties = properties;
            References = references;
        }

        public void UpdateReferences(IEnumerable<string> moreReferences)
        {
            References.UnionWith(moreReferences);
        }

        public bool Equals(IntellisenseObject other)
        {
            return !ReferenceEquals(other, null) &&
                   other.Name == Name &&
                   other.Namespace == Namespace &&
                   other.BaseName == BaseName &&
                   other.BaseNamespace == BaseNamespace &&
                   other.FullName == FullName;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IntellisenseObject);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^
                   Namespace.GetHashCode() ^
                   FullName.GetHashCode();
        }
    }
}