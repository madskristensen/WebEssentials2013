using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal class ImageGenerator
    {
        public static IEnumerable<SpriteFragment> CreateImage(SpriteDocument sprite, ImageFormat format, out string imageFile)
        {
            imageFile = Path.ChangeExtension(sprite.FileName, format.ToString().ToLowerInvariant().Replace("jpeg", "jpg"));
            Dictionary<string, Image> images = GetImages(sprite);

            int width = sprite.IsVertical ? images.Values.Max(i => i.Width) : images.Values.Sum(i => i.Width);
            int height = sprite.IsVertical ? images.Values.Sum(img => img.Height) : images.Values.Max(img => img.Height);

            List<SpriteFragment> fragments = new List<SpriteFragment>();

            using (var bitmap = new Bitmap(width, height))
            {
                using (Graphics canvas = Graphics.FromImage(bitmap))
                {
                    if (sprite.IsVertical)
                        Vertical(images, fragments, canvas);
                    else
                        Horizontal(images, fragments, canvas);
                }

                bitmap.Save(imageFile, format);
            }

            return fragments;
        }

        private static void Vertical(Dictionary<string, Image> images, List<SpriteFragment> fragments, Graphics canvas)
        {
            int currentY = 0;

            foreach (string file in images.Keys)
            {
                Image img = images[file];
                fragments.Add(new SpriteFragment(file, img.Width, img.Height, 0, currentY));

                canvas.DrawImage(img, 0, currentY);
                currentY += img.Height;
            }
        }

        private static void Horizontal(Dictionary<string, Image> images, List<SpriteFragment> fragments, Graphics canvas)
        {
            int currentX = 0;

            foreach (string file in images.Keys)
            {
                Image img = images[file];
                fragments.Add(new SpriteFragment(file, img.Width, img.Height, currentX, 0));

                canvas.DrawImage(img, currentX, 0);
                currentX += img.Width;
            }
        }

        private static Dictionary<string, Image> GetImages(SpriteDocument sprite)
        {
            Dictionary<string, Image> images = new Dictionary<string, Image>();

            foreach (string file in sprite.ImageFiles)
            {
                images.Add(file, Image.FromFile(file));
            }

            return images;
        }
    }
}