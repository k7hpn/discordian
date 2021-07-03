using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIan.Helper
{
    public static class ImageHelper
    {
        public async static Task<Bitmap> GetImageFromURI(Uri uri)
        {
            Bitmap response;

            var data = await GetImageData(uri);

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

        private async static Task<byte[]> GetImageData(Uri uri)
        {
            byte[] imageBytes;

            try
            {
                using (var webClient = new WebClient())
                {
                    imageBytes = await webClient.DownloadDataTaskAsync(uri);
                }

                return imageBytes;
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static Bitmap CropImage(Bitmap image, Rectangle cropArea)
        {
            var newImage = new Bitmap(image);
            return newImage.Clone(cropArea, newImage.PixelFormat);
        }
    }
}
