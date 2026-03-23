namespace JJLUtility.IO
{
    /// <summary>
    /// DDS 압축 포맷 식별자(FourCC)를 나타내는 열거형.
    /// </summary>
    public enum DDSFourCC : uint
    {
        DXT1 = 0x31545844,
        DXT2 = 0x32545844,
        DXT3 = 0x33545844,
        DXT4 = 0x34545844,
        DXT5 = 0x35545844,
        ATI1 = 0x31495441,
        ATI2 = 0x32495441,
        BC4U = 0x55344342,
        BC4S = 0x53344342,
        BC5U = 0x55354342,
        BC5S = 0x53354342,
        DX10 = 0x30315844,
    }
}
