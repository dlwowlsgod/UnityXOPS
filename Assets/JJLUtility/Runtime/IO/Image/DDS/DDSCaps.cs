using System;

namespace JJLUtility.IO
{
    [Flags]
    public enum DDSCaps : uint
    {
        Complex = 0x000008,
        MipMap  = 0x400000,
        Texture = 0x001000,
    }
}
