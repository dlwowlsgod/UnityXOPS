namespace JJLUtility.IO
{
    public struct BMPFileHeader
    {
        public const ushort Type = 0x4D42;
        public uint Size;
        public uint Offset;
    }
}