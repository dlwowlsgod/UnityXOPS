namespace JJLUtility.IO
{
    /// <summary>
    /// BMP 파일의 파일 헤더 정보를 담는 구조체.
    /// </summary>
    public struct BMPFileHeader
    {
        public const ushort Type = 0x4D42;
        public uint Size;
        public uint Offset;
    }
}