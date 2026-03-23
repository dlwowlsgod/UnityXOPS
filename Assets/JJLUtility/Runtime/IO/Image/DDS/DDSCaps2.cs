using System;

namespace JJLUtility.IO
{
    /// <summary>
    /// DDS 큐브맵 및 볼륨 텍스처 여부를 나타내는 추가 플래그 열거형.
    /// </summary>
    [Flags]
    public enum DDSCaps2 : uint
    {
        CubeMap          = 0x000200,
        CubeMapPositiveX = 0x000400,
        CubeMapNegativeX = 0x000800,
        CubeMapPositiveY = 0x001000,
        CubeMapNegativeY = 0x002000,
        CubeMapPositiveZ = 0x004000,
        CubeMapNegativeZ = 0x008000,
        Volume           = 0x200000,
    }
}
