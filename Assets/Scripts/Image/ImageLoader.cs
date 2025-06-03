using UnityEngine;
using B83.Image.BMP;
using System.IO;

namespace UnityXOPS
{
    public static class ImageLoader
    {
        public static BMPLoader bmpLoader;
        
        public static bool isInitialized = false;

        private static void Initialize()
        {
            isInitialized = true;
            bmpLoader = new BMPLoader();
        }

        public static Texture2D LoadImage(string path)
        {
            if (!isInitialized)
            {
                Initialize();
            }
            var extension = Path.GetExtension(path);
            var imageBytes = File.ReadAllBytes(path);
            
            if (extension == ".jpg" || extension == ".png")
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(imageBytes);
                return tex;
            }
            
            if (extension == ".bmp")
            {
                var bmpImage = bmpLoader.LoadBMP(imageBytes);
                var tex = bmpImage.ToTexture2D();
                return tex;
            }

            if (extension == ".dds")
            {
                
                return null;
            }

            return null;
        }
    }
}