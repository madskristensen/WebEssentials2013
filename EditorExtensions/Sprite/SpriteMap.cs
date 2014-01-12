using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    internal class SpriteMap
    {
        public string File { get; set; }
        public IEnumerable<SpriteMapConstituent> Constituents { get; set; }
    }
}
