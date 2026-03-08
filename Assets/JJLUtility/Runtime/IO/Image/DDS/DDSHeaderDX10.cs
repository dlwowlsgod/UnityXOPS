namespace JJLUtility.IO
{
    public struct DDSHeaderDX10
    {
        public DXGIFormat DxgiFormat;
        public D3D10ResourceDimension ResourceDimension;
        public uint MiscFlag;
        public uint ArraySize;
        public uint MiscFlags2;

        public bool IsCubeMap => (MiscFlag & 0x4) != 0;
    }
}
