using System;

namespace JJLUtility.IO
{
    /// <summary>
    /// DDS 텍스처 복잡도와 MipMap 여부를 나타내는 플래그 열거형.
    /// </summary>
    [Flags]
    public enum DDSCaps : uint
    {
        Complex = 0x000008,
        MipMap  = 0x400000,
        Texture = 0x001000,
    }
}
