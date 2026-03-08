namespace JJLUtility.IO
{
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
