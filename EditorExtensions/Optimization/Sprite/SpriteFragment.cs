
namespace MadsKristensen.EditorExtensions
{
    internal class SpriteFragment
    {
        public SpriteFragment(string fileName, int width, int height, int x, int y)
        {
            FileName = fileName;
            Width = width;
            Height = height;
            X = x;
            Y = y;
        }

        public string FileName { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}