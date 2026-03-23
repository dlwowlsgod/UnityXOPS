namespace JJLUtility.IO
{
    /// <summary>
    /// DDS 파일의 기본 헤더 정보를 담는 구조체.
    /// </summary>
    public struct DDSHeader
    {
        public uint Size;
        public DDSFlags Flags;
        public uint Height;
        public uint Width;
        public uint PitchOrLinearSize;
        public uint Depth;
        public uint MipMapCount;
        // Reserved1[11] — 파일 읽기 시 스킵
        public DDSPixelFormat PixelFormat;
        public DDSCaps Caps;
        public DDSCaps2 Caps2;
        public uint Caps3;
        public uint Caps4;
        // Reserved2 — 파일 읽기 시 스킵

        /// <summary>
        /// 지정된 DDS 플래그가 설정되어 있는지 확인한다.
        /// </summary>
        public bool HasFlag(DDSFlags flag) => (Flags & flag) != 0;
        /// <summary>
        /// 지정된 Caps2 플래그가 설정되어 있는지 확인한다.
        /// </summary>
        public bool HasCaps2(DDSCaps2 flag) => (Caps2 & flag) != 0;
    }
}
