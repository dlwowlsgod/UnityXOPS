namespace JJLUtility.IO
{
    public struct DDSPixelFormat
    {
        public uint Size;
        public DDSPixelFormatFlags Flags;
        public DDSFourCC FourCC;
        public uint RGBBitCount;
        public uint RBitMask;
        public uint GBitMask;
        public uint BBitMask;
        public uint ABitMask;

        public bool HasFlag(DDSPixelFormatFlags flag) => (Flags & flag) != 0;
    }
}
