namespace JJLUtility.IO
{
    /// <summary>
    /// TGA 파일 헤더 정보를 담는 구조체.
    /// </summary>
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
