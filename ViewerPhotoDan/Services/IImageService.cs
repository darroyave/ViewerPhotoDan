using System.Collections.Generic;
using System.Drawing;

namespace ViewerPhotoDan.Services
{
    public interface IImageService
    {
        IEnumerable<string> GetImageFiles(string directoryPath);
        Image CreateThumbnail(string imagePath, Size size);
        Image LoadImageFromFile(string imagePath);
        bool TryGetCachedThumbnail(string originalImagePath, Size size, out Image? thumbnail);
        void SaveThumbnailToCache(string originalImagePath, Size size, Image thumbnail);
    }
}
