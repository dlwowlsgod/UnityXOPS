namespace JJLUtility.IO
{
    /// <summary>
    /// DDS 파일의 픽셀 포맷 정보를 담는 구조체.
    /// </summary>
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

        /// <summary>
        /// 지정된 픽셀 포맷 플래그가 설정되어 있는지 확인한다.
        /// </summary>
        public bool HasFlag(DDSPixelFormatFlags flag) => (Flags & flag) != 0;
    }
}
