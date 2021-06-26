using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;

namespace DiscordIan.Helper
{
    public static class ImageHelper
    {
        public static Bitmap GetImageFromURL(string url)
        {
            Bitmap response;

            var data = GetImageData(url);

            var streamBitmap = new MemoryStream(data);
            response = new Bitmap(Image.FromStream(streamBitmap));

            return response;
        }

        public static Bitmap ClipComicSection(Bitmap image, Tuple<int, int> layout, Tuple<int, int> selection)
        {
            var cellSize = new Size()
            {
                Width = image.Width / layout.Item1,
                Height = image.Height / layout.Item2
            };

            var startPos = new Point() {
                X = (selection.Item1 - 1) * cellSize.Width,
                Y = (selection.Item2 - 1) * cellSize.Height
            };

            var rect = new Rectangle(startPos, cellSize);

            return CropImage(image, rect);
        }

        private static byte[] GetImageData(string url)
        {
            byte[] imageBytes;

            using (var webClient = new WebClient())
            {
                imageBytes = webClient.DownloadData(url);
            }

            return imageBytes;
        }

        private static Bitmap CropImage(Bitmap image, Rectangle cropArea)
        {
            var newImage = new Bitmap(image);
            return newImage.Clone(cropArea, newImage.PixelFormat);
        }
    }
}
