using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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

            SpriteExporter formatter = new SpriteExporter();
            formatter.ExportToCodeFile(sprite, spriteImageFile, SpriteFormatter.CSS);
            formatter.ExportToCodeFile(sprite, spriteImageFile, SpriteFormatter.LESS);
            formatter.ExportToCodeFile(sprite, spriteImageFile, SpriteFormatter.SCSS);
                     
        }

        private void GenerateSpriteImage(string spriteImageFile)
        {
            using (Bitmap bitmap = new Bitmap(sprite.Width, sprite.Height, PixelFormat.Format32bppPArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.HighSpeed;
                    graphics.CompositingQuality = CompositingQuality.HighSpeed;

                    foreach (IMappedImageInfo map in sprite.MappedImages)
                    {
                        ImageInfo info = map.ImageInfo as ImageInfo;
                        Image image = Image.FromFile(info.Name);

                        var ext = Path.GetExtension(info.Name);

                        if (ext.Equals(".jpg") || ext.Equals(".jpeg"))
                            image = ScaleJpeg(Image.FromFile(info.Name), 70);

                        graphics.DrawImage(image, map.X, map.Y, info.Width, info.Height);
                    }
                }

                ImageFormat format = PasteImage.GetImageFormat(Path.GetExtension(spriteImageFile));

                bitmap.Save(spriteImageFile, format);
            }
        }

        public static Image ScaleJpeg(Image image, int quality)
        {
            using (var qualityParam = new EncoderParameter(Encoder.Quality, quality))
            using (var encoderParams = new EncoderParameters(1))
            using (var stream = new MemoryStream())
            {
                ImageCodecInfo jpegCodec = ImageCodecInfo.GetImageEncoders()
                                                         .FirstOrDefault(t => t.MimeType == "image/jpeg");


                encoderParams.Param[0] = qualityParam;

                image.Save(stream, jpegCodec, encoderParams);

                stream.Position = 0;

                return Image.FromStream(stream);
            }
        }

        private void GenerateSpriteMap(string spriteFileName)
        {
            var map = new
            {
                Images = sprite.MappedImages.Select(mapped =>
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