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
        IEnumerable<ImageInfo> rectangles;
        
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
            Mapper.Canvas canvas = new Mapper.Canvas();
            MapperOptimalEfficiency<Sprite> mapper = new MapperOptimalEfficiency<Sprite>(canvas);

            Sprite sprite = mapper.Mapping(rectangles);

            GenerateSpriteImage(sprite, spriteImageFile);

            GenerateSpriteMap(spriteImageFile, sprite.MappedImages);
        }

        private void GenerateSpriteImage(ISprite sprite, string spriteImageFile)
        {
            using (Bitmap bitmap = new Bitmap(sprite.Width, sprite.Height, PixelFormat.Format32bppPArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    // Draw border around the entire image. [Mads] Why?
                    DrawBorder(graphics, sprite.Width, sprite.Height, 0, 0);

                    foreach (IMappedImageInfo map in sprite.MappedImages)
                    {
                        ImageInfo info = map.ImageInfo as ImageInfo;
                        Image image = Image.FromFile(info.Name);

                        graphics.DrawImage(image, map.X, map.Y, info.Width, info.Height);
                    }
                }

                bitmap.Save(spriteImageFile, ImageFormat.Png);
            }
        }

        private static void DrawBorder(Graphics graphics, int width, int height, int xOffset, int yOffset)
        {
            // Fill the rectangle
            Color color = Color.White;

            using (SolidBrush brush = new SolidBrush(color))
            {
                graphics.FillRectangle(brush, xOffset, yOffset, width, height);
            }
            // Draw border 
            using (Pen pen = new Pen(Color.Black))
            {
                graphics.DrawRectangle(pen, xOffset, yOffset, width - 1, height - 1);
            }
        }

        private static void GenerateSpriteMap(string spriteFileName, IEnumerable<IMappedImageInfo> mappedImages)
        {
            SpriteMap map = new SpriteMap()
            {
                File = spriteFileName,
                Constituents =
                        mappedImages.Select(mapped =>
                        {
                            SpriteMapConstituent image = new SpriteMapConstituent();
                            var info = mapped.ImageInfo as ImageInfo;
                            image.Name = Path.GetFileName(info.Name);
                            image.Width = info.Width;
                            image.Height = info.Height;
                            image.OffsetX = mapped.X;
                            image.OffsetY = mapped.Y;
                            return image;
                        })
            };

            File.WriteAllText(spriteFileName + ".sprite", JObject.Parse(Json.Encode(map)).ToString());
        }
    }
}
