using Mapper;

namespace MadsKristensen.EditorExtensions
{
    public class ImageInfo : IImageInfo
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public string Name { get; private set; }

        public ImageInfo(int width, int height, string name)
        {
            Width = width;
            Height = height;
            Name = name;
        }
    }
}