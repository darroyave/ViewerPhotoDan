using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ViewerPhotoDan.Services
{
    public class ImageService : IImageService
    {
        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff" };
        private readonly string _cacheDirectory;

        public ImageService()
        {
            _cacheDirectory = Path.Combine(Path.GetTempPath(), "ViewerPhotoDanThumbnails");
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        public IEnumerable<string> GetImageFiles(string directoryPath)
        {
            try
            {
                return Directory.GetFiles(directoryPath)
                                .Where(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLower()));
            }
            catch (DirectoryNotFoundException)
            {
                return Enumerable.Empty<string>();
            }
        }

        public Image CreateThumbnail(string imagePath, Size size)
        {
            using (Image image = LoadImageFromFile(imagePath))
            {
                int sourceWidth = image.Width;
                int sourceHeight = image.Height;
                float nPercent = 0;
                float nPercentW = 0;
                float nPercentH = 0;

                nPercentW = ((float)size.Width / (float)sourceWidth);
                nPercentH = ((float)size.Height / (float)sourceHeight);

                if (nPercentH < nPercentW)
                    nPercent = nPercentH;
                else
                    nPercent = nPercentW;

                int destWidth = (int)(sourceWidth * nPercent);
                int destHeight = (int)(sourceHeight * nPercent);

                Bitmap b = new Bitmap(size.Width, size.Height);
                using (Graphics g = Graphics.FromImage(b))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, (size.Width - destWidth) / 2, (size.Height - destHeight) / 2, destWidth, destHeight);
                }
                return b;
            }
        }

        public Image LoadImageFromFile(string imagePath)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    return Image.FromStream(ms);
                }
            }
        }

        public bool TryGetCachedThumbnail(string originalImagePath, Size size, out Image? thumbnail)
        {
            thumbnail = null;
            string cacheFilePath = GetCacheFilePath(originalImagePath, size);

            if (File.Exists(cacheFilePath))
            {
                try
                {
                    thumbnail = Image.FromFile(cacheFilePath);
                    return true;
                }
                catch (Exception) { /* Ignore corrupted cache files */ }
            }
            return false;
        }

        public void SaveThumbnailToCache(string originalImagePath, Size size, Image thumbnail)
        {
            string cacheFilePath = GetCacheFilePath(originalImagePath, size);
            try
            {
                thumbnail.Save(cacheFilePath);
            }
            catch (Exception) { /* Ignore errors saving to cache */ }
        }

        private string GetCacheFilePath(string originalImagePath, Size size)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string hashInput = originalImagePath + size.Width + "x" + size.Height;
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
                string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                return Path.Combine(_cacheDirectory, hash + ".png"); // Save as PNG for transparency support
            }
        }
    }
}
