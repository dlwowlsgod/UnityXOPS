using System;

namespace JJLUtility.IO
{
    [Flags]
    public enum DDSPixelFormatFlags : uint
    {
        AlphaPixels = 0x00001,
        Alpha       = 0x00002,
        FourCC      = 0x00004,
        RGB         = 0x00040,
        YUV         = 0x00200,
        Luminance   = 0x20000,
    }
}
