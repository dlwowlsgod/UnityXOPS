using System;

namespace JJLUtility.IO
{
    /// <summary>
    /// DDS 헤더의 유효 필드를 나타내는 플래그 열거형.
    /// </summary>
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
