namespace JJLUtility.IO
{
    /// <summary>
    /// BMP 파일의 정보 헤더를 나타내는 구조체. BMP 이미지의 크기, 색상 포맷,
    /// 압축 방식 등의 메타데이터를 담는다.
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