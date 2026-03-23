using System;

namespace JJLUtility.IO
{
    /// <summary>
    /// DDS 픽셀 포맷 구조체의 유효 필드를 나타내는 플래그 열거형.
    /// </summary>
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
