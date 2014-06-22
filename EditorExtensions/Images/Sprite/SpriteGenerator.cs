using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.Images
{
    internal static class SpriteGenerator
    {
        public async static Task<IEnumerable<SpriteFragment>> MakeImage(SpriteDocument document, string imageFile, Func<string, bool, Task> updateSprite)
        {
            ProjectHelpers.CheckOutFileFromSourceControl(imageFile);

            Dictionary<string, Image> images = await WatchFiles(document, updateSprite);

            int width = document.IsVertical ? images.Values.Max(i => i.Width) : images.Values.Sum(i => i.Width);
            int height = document.IsVertical ? images.Values.Sum(img => img.Height) : images.Values.Max(img => img.Height);

            List<SpriteFragment> fragments = new List<SpriteFragment>();

            using (var bitmap = new Bitmap(width, height))
            {
                using (Graphics canvas = Graphics.FromImage(bitmap))
                {
                    if (document.IsVertical)
                        Vertical(images, fragments, canvas);
                    else
                        Horizontal(images, fragments, canvas);
                }

                bitmap.Save(imageFile, PasteImage.GetImageFormat("." + document.FileExtension));
            }

            return fragments;
        }

        public async static Task<Dictionary<string, Image>> WatchFiles(SpriteDocument document, Func<string, bool, Task> updateSprite)
        {
            Dictionary<string, Image> images = GetImages(document);

            await new BundleFileObserver().AttachFileObserver(document, document.FileName, updateSprite);

            foreach (string file in images.Keys)
            {
                await new BundleFileObserver().AttachFileObserver(document, file, updateSprite);
            }

            return images;
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

            foreach (string file in sprite.BundleAssets)
            {
                Image image = Image.FromFile(file);

                // Only touch the resolution of the image if it isn't 96. 
                // That way we keep the original image 'as is' in all other cases.
                if (Math.Round(image.VerticalResolution) != 96F || Math.Round(image.HorizontalResolution) != 96F)
                    image = new Bitmap(image);

                images.Add(file, image);
            }

            return images;
        }
    }
}
