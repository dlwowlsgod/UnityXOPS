namespace JJLUtility.IO
{
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

        public bool HasFlag(DDSFlags flag) => (Flags & flag) != 0;
        public bool HasCaps2(DDSCaps2 flag) => (Caps2 & flag) != 0;
    }
}
