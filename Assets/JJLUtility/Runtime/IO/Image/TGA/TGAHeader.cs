namespace JJLUtility.IO
{
    public struct TGAHeader
    {
        public byte IDLength;
        public byte ColorMapType;
        public TGAImageType ImageType;

        public ushort ColorMapStart;
        public ushort ColorMapLength;
        public byte ColorMapDepth;

        public ushort XOrigin;
        public ushort YOrigin;
        public ushort Width;
        public ushort Height;
        public byte PixelDepth;
        public byte ImageDescriptor;

        public int AlphaBits => ImageDescriptor & 0x0F;
        public bool IsTopToBottom => (ImageDescriptor & 0x20) != 0;
    }
}
