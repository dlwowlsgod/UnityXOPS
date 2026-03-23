namespace JJLUtility.IO
{
    /// <summary>
    /// DXGI 텍스처 픽셀 포맷을 나타내는 열거형.
    /// </summary>
    public enum DXGIFormat : uint
    {
        Unknown                = 0,

        R32G32B32A32_Float     = 2,
        R32G32B32A32_UInt      = 3,
        R32G32B32A32_SInt      = 4,
        R32G32B32_Float        = 6,
        R32G32B32_UInt         = 7,
        R32G32B32_SInt         = 8,
        R16G16B16A16_Float     = 10,
        R16G16B16A16_UNorm     = 11,
        R16G16B16A16_UInt      = 12,
        R16G16B16A16_SNorm     = 13,
        R16G16B16A16_SInt      = 14,
        R32G32_Float           = 16,
        R8G8B8A8_UNorm         = 28,
        R8G8B8A8_UNorm_SRGB    = 29,
        R8G8B8A8_UInt          = 30,
        R8G8B8A8_SNorm         = 31,
        R8G8B8A8_SInt          = 32,
        R16G16_Float           = 34,
        R16G16_UNorm           = 35,
        R32_Float              = 41,
        R8G8_UNorm             = 49,
        R16_Float              = 54,
        R16_UNorm              = 56,
        R8_UNorm               = 61,
        A8_UNorm               = 65,

        BC1_UNorm              = 71,
        BC1_UNorm_SRGB         = 72,
        BC2_UNorm              = 74,
        BC2_UNorm_SRGB         = 75,
        BC3_UNorm              = 77,
        BC3_UNorm_SRGB         = 78,
        BC4_UNorm              = 80,
        BC4_SNorm              = 81,
        BC5_UNorm              = 83,
        BC5_SNorm              = 84,
        B5G6R5_UNorm           = 85,
        B5G5R5A1_UNorm         = 86,
        B8G8R8A8_UNorm         = 87,
        B8G8R8X8_UNorm         = 88,
        B8G8R8A8_UNorm_SRGB    = 91,
        B8G8R8X8_UNorm_SRGB    = 93,
        BC6H_UF16              = 95,
        BC6H_SF16              = 96,
        BC7_UNorm              = 98,
        BC7_UNorm_SRGB         = 99,

        B4G4R4A4_UNorm         = 115,
    }
}
