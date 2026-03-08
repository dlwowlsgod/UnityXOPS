using System;

namespace JJLUtility.IO
{
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
