namespace JJLUtility.IO
{
    /// <summary>
    /// TGA 파일의 이미지 데이터 타입을 나타내는 열거형.
    /// </summary>
    public enum TGAImageType : byte
    {
        NoImage        = 0,
        ColorMapped    = 1,  // 팔레트 인덱스
        TrueColor      = 2,  // RGB
        Grayscale      = 3,
        ColorMappedRLE = 9,
        TrueColorRLE   = 10,
        GrayscaleRLE   = 11,
    }
}
