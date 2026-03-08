using System;

namespace JJLUtility.IO
{
    [Flags]
    public enum DDSFlags : uint
    {
        Caps        = 0x000001,
        Height      = 0x000002,
        Width       = 0x000004,
        Pitch       = 0x000008,
        PixelFormat = 0x001000,
        MipMapCount = 0x020000,
        LinearSize  = 0x080000,
        Depth       = 0x800000,
    }
}
