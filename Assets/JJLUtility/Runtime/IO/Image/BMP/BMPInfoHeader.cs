namespace JJLUtility.IO
{
    /// <summary>
    /// Represents the information header of a BMP file, providing metadata about the dimensions,
    /// color format, and compression details of the BMP image.
    /// </summary>
    public struct BMPInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public const uint Planes = 1;
        public ushort BitCount;
        public BMPCompression Compression;
        public uint SizeImage;
        public int XPixelPerMeter;
        public int YPixelPerMeter;
        public uint ColorUsed;
        public uint ColorImportant;
        public uint RedMask;
        public uint GreenMask;
        public uint BlueMask;
        public uint AlphaMask;
    }
}