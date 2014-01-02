using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lib.Generation
{
    public class CollectionModel
    {
        public string[] AStringArray { get; set; }
        public IEnumerable<string> AStringIEnumerable { get; set; }
        public ICollection<string> AStringICollection { get; set; }
        public IList<string> AStringIList { get; set; }
        public List<string> AStringList { get; set; }
        public Collection<string> AStringCollection { get; set; }
        public List<Simple> ASimpleList { get; set; }
        public List<long> ALongList { get; set; }
    }
}