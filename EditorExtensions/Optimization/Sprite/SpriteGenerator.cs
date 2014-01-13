using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web.Helpers;
using Mapper;
using Newtonsoft.Json.Linq;

namespace MadsKristensen.EditorExtensions
{
    public class SpriteGenerator
    {
        private IEnumerable<ImageInfo> rectangles;
        private Sprite sprite;
        public const string MapExtension = ".sprite";

        public SpriteGenerator(IEnumerable<ImageInfo> imageInfo)
        {
            rectangles = imageInfo;
        }

        /// <summary>
        /// Generate Sprite image with map file
        /// </summary>
        /// <param name="spriteImageFile">Full path to destination file name</param>
        public void GenerateSpriteWithMaps(string spriteImageFile)
        {
            MapperOptimalEfficiency<Sprite> mapper = new MapperOptimalEfficiency<Sprite>(new Canvas());

            sprite = mapper.Mapping(rectangles);

            GenerateSpriteImage(spriteImageFile);

            GenerateSpriteMap(spriteImageFile);
        }

        private void GenerateSpriteImage(string spriteImageFile)
        {
            using (Bitmap bitmap = new Bitmap(sprite.Width, sprite.Height, PixelFormat.Format32bppPArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    foreach (IMappedImageInfo map in sprite.MappedImages)
                    {
                        ImageInfo info = map.ImageInfo as ImageInfo;
                        Image image = Image.FromFile(info.Name);

                        graphics.DrawImage(image, map.X, map.Y, info.Width, info.Height);
                    }
                }

                ImageFormat format = PasteImage.GetImageFormat(Path.GetExtension(spriteImageFile));
                bitmap.Save(spriteImageFile, format);
            }
        }

        private void GenerateSpriteMap(string spriteFileName)
        {
            var map = new
            {
                Constituents = sprite.MappedImages.Select(mapped =>
                {
                    var info = mapped.ImageInfo as ImageInfo;
                    SpriteMapConstituent image = new SpriteMapConstituent()
                    {
                        Name = Path.GetFileName(info.Name),
                        Width = info.Width,
                        Height = info.Height,
                        OffsetX = mapped.X,
                        OffsetY = mapped.Y,
                    };

                    return image;
                })
            };

            File.WriteAllText(spriteFileName + MapExtension, JObject.Parse(Json.Encode(map)).ToString());
        }
    }
}