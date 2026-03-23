namespace JJLUtility.IO
{
    /// <summary>
    /// DX10 확장 헤더에서 사용되는 리소스 차원 타입 열거형.
    /// </summary>
    public enum D3D10ResourceDimension : uint
    {
        Unknown   = 0,
        Buffer    = 1,
        Texture1D = 2,
        Texture2D = 3,
        Texture3D = 4,
    }
}
