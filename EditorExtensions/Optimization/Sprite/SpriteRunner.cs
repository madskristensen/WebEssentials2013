using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web.Helpers;
using Mapper;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Sprite runner class
    /// </summary>
    public class SpriteRunner
    {
        IEnumerable<ImageInfo> rectangles;

        private int _imageZoom = 1;

        public SpriteRunner(IEnumerable<ImageInfo> imageInfo)
        {
            rectangles = imageInfo;
        }

        /// <summary>
        /// Generate Sprite image with map file
        /// </summary>
        /// <param name="spriteImageFile">Full path to destination file name</param>
        public void GenerateSpriteWithMaps(string spriteImageFile)
        {
            Mapper.Canvas _canvas = new Mapper.Canvas();
            MapperOptimalEfficiency<Sprite> mapper = new MapperOptimalEfficiency<Sprite>(_canvas);

            Sprite sprite = mapper.Mapping(rectangles);

            SpriteToImage(sprite, spriteImageFile);

            // Write .sprite file
            GenerateSpriteMap(spriteImageFile, sprite.MappedImages);
        }


        /// <summary>
        /// Creates an image showing the structure of a sprite (that is, its constituent images).
        /// </summary>
        /// <param name="sprite">
        /// Sprite to be shown
        /// </param>
        /// <param name="spriteFileName">
        /// Generate Sprite image with map file
        /// </param>
        /// <returns>
        /// Url of the generated image.
        /// </returns>
        private void SpriteToImage(ISprite sprite, string spriteImageFile)
        {
            using (Bitmap bitmap = new Bitmap(sprite.Width * _imageZoom, sprite.Height * _imageZoom))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    // Draw border around the entire image
                    DrawRectangle(
                        graphics,
                        sprite.Width * _imageZoom, sprite.Height * _imageZoom,
                        0, 0,
                        false, false);

                    foreach (IMappedImageInfo mappedImageInfo in sprite.MappedImages)
                    {
                        DrawRectangle(
                            graphics,
                            mappedImageInfo.ImageInfo.Width * _imageZoom, mappedImageInfo.ImageInfo.Height * _imageZoom,
                            mappedImageInfo.X * _imageZoom, mappedImageInfo.Y * _imageZoom,
                            true, false);
                    }
                }

                bitmap.Save(spriteImageFile, ImageFormat.Png);
            }
        }


        /// <summary>
        /// Draws a rectangle on the canvas. The rectangle denotes a FreeArea.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="occupied"></param>
        private static void DrawRectangle(Graphics graphics, int width, int height, int xOffset, int yOffset, bool occupied, bool newlyOccupied)
        {
            // Fill the rectangle
            Color color = occupied ?
                            newlyOccupied ? Color.DarkGreen : Color.LightGreen :
                            Color.White;

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

            FileHelpers.WriteFile(Path.Combine(spriteFileName, ".map"), Json.Encode(map));
        }
    }
}
