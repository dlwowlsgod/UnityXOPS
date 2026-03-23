using UnityEngine;
using System.IO;

namespace JJLUtility.IO
{
    /// <summary>
    /// 파싱된 DDS 파일 데이터를 담는 컨테이너 클래스.
    /// </summary>
    public class DDSFile
    {
        public DDSHeader Header;
        public DDSHeaderDX10 DX10Header;
        public bool HasDX10Header;
        public Color32[] Pixels;
    }

    public partial class ImageLoader
    {
        private const uint DDSMagic = 0x20534444;

        /// <summary>
        /// 지정 경로의 DDS 파일을 파싱해 DDSFile 객체로 반환한다.
        /// </summary>
        /// <param name="filepath">DDS 파일 경로.</param>
        /// <returns>파싱된 DDSFile. 실패 시 null.</returns>
        private static DDSFile LoadDDSFile(string filepath)
        {
            if (Path.GetExtension(filepath).ToLower() != ".dds")
            {
                Debugger.LogError($"File is not .dds file: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            if (!File.Exists(filepath))
            {
                Debugger.LogError($"File not found: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            var stream = File.OpenRead(filepath);
            var reader = new BinaryReader(stream);

            if (reader.ReadUInt32() != DDSMagic)
            {
                Debugger.LogError($"File is not a DDS file: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            var ddsFile = new DDSFile();
            ddsFile.Header = LoadDDSHeader(reader);

            if (ddsFile.Header.Size != 124)
            {
                Debugger.LogError($"Invalid DDS header size: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            if (ddsFile.Header.PixelFormat.HasFlag(DDSPixelFormatFlags.FourCC) &&
                ddsFile.Header.PixelFormat.FourCC == DDSFourCC.DX10)
            {
                ddsFile.DX10Header = LoadDDSHeaderDX10(reader);
                ddsFile.HasDX10Header = true;
            }

            if (ddsFile.HasDX10Header)
            {
                if (ddsFile.DX10Header.ResourceDimension != D3D10ResourceDimension.Texture2D)
                {
                    Debugger.LogError($"Only 2D textures are supported: {filepath}", Instance, nameof(ImageLoader));
                    return null;
                }
                if (ddsFile.DX10Header.ArraySize > 1)
                {
                    Debugger.LogError($"Texture arrays are not supported: {filepath}", Instance, nameof(ImageLoader));
                    return null;
                }
                if (ddsFile.DX10Header.IsCubeMap)
                {
                    Debugger.LogError($"Cubemaps are not supported: {filepath}", Instance, nameof(ImageLoader));
                    return null;
                }
            }
            else
            {
                if (ddsFile.Header.HasCaps2(DDSCaps2.CubeMap))
                {
                    Debugger.LogError($"Cubemaps are not supported: {filepath}", Instance, nameof(ImageLoader));
                    return null;
                }
                if (ddsFile.Header.HasCaps2(DDSCaps2.Volume))
                {
                    Debugger.LogError($"Volume textures are not supported: {filepath}", Instance, nameof(ImageLoader));
                    return null;
                }
            }

            var pf = ddsFile.Header.PixelFormat;
            bool success;

            if (pf.HasFlag(DDSPixelFormatFlags.FourCC))
                success = LoadFourCCDDS(reader, ref ddsFile);
            else if (pf.HasFlag(DDSPixelFormatFlags.RGB))
                success = LoadUncompressedDDS(reader, ref ddsFile);
            else if (pf.HasFlag(DDSPixelFormatFlags.Luminance))
                success = LoadLuminanceDDS(reader, ref ddsFile);
            else
            {
                Debugger.LogError($"Unsupported DDS pixel format flags: {pf.Flags} ({filepath})", Instance, nameof(ImageLoader));
                return null;
            }

            if (!success)
            {
                Debugger.LogError($"File is corrupted: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            return ddsFile;
        }

        /// <summary>
        /// BinaryReader로부터 DDS 기본 헤더를 읽어 반환한다.
        /// </summary>
        private static DDSHeader LoadDDSHeader(BinaryReader reader)
        {
            var h = new DDSHeader();
            h.Size              = reader.ReadUInt32();
            h.Flags             = (DDSFlags)reader.ReadUInt32();
            h.Height            = reader.ReadUInt32();
            h.Width             = reader.ReadUInt32();
            h.PitchOrLinearSize = reader.ReadUInt32();
            h.Depth             = reader.ReadUInt32();
            h.MipMapCount       = reader.ReadUInt32();
            for (int i = 0; i < 11; i++) reader.ReadUInt32();
            h.PixelFormat       = LoadDDSPixelFormat(reader);
            h.Caps              = (DDSCaps)reader.ReadUInt32();
            h.Caps2             = (DDSCaps2)reader.ReadUInt32();
            h.Caps3             = reader.ReadUInt32();
            h.Caps4             = reader.ReadUInt32();
            reader.ReadUInt32();
            return h;
        }

        /// <summary>
        /// BinaryReader로부터 DDS 픽셀 포맷 구조체를 읽어 반환한다.
        /// </summary>
        private static DDSPixelFormat LoadDDSPixelFormat(BinaryReader reader)
        {
            var pf = new DDSPixelFormat();
            pf.Size        = reader.ReadUInt32();
            pf.Flags       = (DDSPixelFormatFlags)reader.ReadUInt32();
            pf.FourCC      = (DDSFourCC)reader.ReadUInt32();
            pf.RGBBitCount = reader.ReadUInt32();
            pf.RBitMask    = reader.ReadUInt32();
            pf.GBitMask    = reader.ReadUInt32();
            pf.BBitMask    = reader.ReadUInt32();
            pf.ABitMask    = reader.ReadUInt32();
            return pf;
        }

        /// <summary>
        /// BinaryReader로부터 DX10 확장 헤더를 읽어 반환한다.
        /// </summary>
        private static DDSHeaderDX10 LoadDDSHeaderDX10(BinaryReader reader)
        {
            var dx10 = new DDSHeaderDX10();
            dx10.DxgiFormat        = (DXGIFormat)reader.ReadUInt32();
            dx10.ResourceDimension = (D3D10ResourceDimension)reader.ReadUInt32();
            dx10.MiscFlag          = reader.ReadUInt32();
            dx10.ArraySize         = reader.ReadUInt32();
            dx10.MiscFlags2        = reader.ReadUInt32();
            return dx10;
        }

        /// <summary>
        /// FourCC 값에 따라 적합한 BC 포맷 디코더를 선택해 픽셀 데이터를 로드한다.
        /// </summary>
        private static bool LoadFourCCDDS(BinaryReader reader, ref DDSFile ddsFile)
        {
            switch (ddsFile.Header.PixelFormat.FourCC)
            {
                case DDSFourCC.DXT1:
                    return LoadBC1DDS(reader, ref ddsFile);
                case DDSFourCC.DXT2:
                case DDSFourCC.DXT3:
                    return LoadBC2DDS(reader, ref ddsFile);
                case DDSFourCC.DXT4:
                case DDSFourCC.DXT5:
                    return LoadBC3DDS(reader, ref ddsFile);
                case DDSFourCC.ATI1:
                case DDSFourCC.BC4U:
                case DDSFourCC.BC4S:
                    return LoadBC4DDS(reader, ref ddsFile);
                case DDSFourCC.ATI2:
                case DDSFourCC.BC5U:
                case DDSFourCC.BC5S:
                    return LoadBC5DDS(reader, ref ddsFile);
                case DDSFourCC.DX10:
                    return LoadDX10DDS(reader, ref ddsFile);
                default:
                    Debugger.LogError($"Unsupported FourCC: {ddsFile.Header.PixelFormat.FourCC}", Instance, nameof(ImageLoader));
                    return false;
            }
        }

        /// <summary>
        /// DXGI 포맷에 따라 DX10 확장 DDS 픽셀 데이터를 로드한다.
        /// </summary>
        private static bool LoadDX10DDS(BinaryReader reader, ref DDSFile ddsFile)
        {
            switch (ddsFile.DX10Header.DxgiFormat)
            {
                case DXGIFormat.BC1_UNorm:
                case DXGIFormat.BC1_UNorm_SRGB:
                    return LoadBC1DDS(reader, ref ddsFile);
                case DXGIFormat.BC2_UNorm:
                case DXGIFormat.BC2_UNorm_SRGB:
                    return LoadBC2DDS(reader, ref ddsFile);
                case DXGIFormat.BC3_UNorm:
                case DXGIFormat.BC3_UNorm_SRGB:
                    return LoadBC3DDS(reader, ref ddsFile);
                case DXGIFormat.BC4_UNorm:
                case DXGIFormat.BC4_SNorm:
                    return LoadBC4DDS(reader, ref ddsFile);
                case DXGIFormat.BC5_UNorm:
                case DXGIFormat.BC5_SNorm:
                    return LoadBC5DDS(reader, ref ddsFile);
                case DXGIFormat.R8G8B8A8_UNorm:
                case DXGIFormat.R8G8B8A8_UNorm_SRGB:
                    return LoadDX10RGBA8DDS(reader, ref ddsFile, true);
                case DXGIFormat.B8G8R8A8_UNorm:
                case DXGIFormat.B8G8R8A8_UNorm_SRGB:
                    return LoadDX10BGRA8DDS(reader, ref ddsFile, true);
                case DXGIFormat.B8G8R8X8_UNorm:
                case DXGIFormat.B8G8R8X8_UNorm_SRGB:
                    return LoadDX10BGRA8DDS(reader, ref ddsFile, false);
                case DXGIFormat.B5G6R5_UNorm:
                    return LoadDX10B5G6R5DDS(reader, ref ddsFile);
                case DXGIFormat.B5G5R5A1_UNorm:
                    return LoadDX10B5G5R5A1DDS(reader, ref ddsFile);
                case DXGIFormat.B4G4R4A4_UNorm:
                    return LoadDX10B4G4R4A4DDS(reader, ref ddsFile);
                case DXGIFormat.R8_UNorm:
                    return LoadDX10R8DDS(reader, ref ddsFile, false);
                case DXGIFormat.A8_UNorm:
                    return LoadDX10R8DDS(reader, ref ddsFile, true);
                default:
                    Debugger.LogError($"Unsupported DXGI format: {ddsFile.DX10Header.DxgiFormat}", Instance, nameof(ImageLoader));
                    return false;
            }
        }

        /// <summary>
        /// BC1(DXT1) 압축 DDS를 디코딩해 픽셀 배열로 채운다.
        /// </summary>
        private static bool LoadBC1DDS(BinaryReader reader, ref DDSFile ddsFile)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            ddsFile.Pixels = new Color32[width * height];

            int blocksW = (width  + 3) / 4;
            int blocksH = (height + 3) / 4;
            for (int by = 0; by < blocksH; by++)
            for (int bx = 0; bx < blocksW; bx++)
                DecodeBC1Block(reader, ddsFile.Pixels, bx * 4, by * 4, width, height, false);

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// BC2(DXT2/DXT3) 압축 DDS를 디코딩해 픽셀 배열로 채운다.
        /// </summary>
        private static bool LoadBC2DDS(BinaryReader reader, ref DDSFile ddsFile)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            ddsFile.Pixels = new Color32[width * height];

            int blocksW = (width  + 3) / 4;
            int blocksH = (height + 3) / 4;
            for (int by = 0; by < blocksH; by++)
            for (int bx = 0; bx < blocksW; bx++)
                DecodeBC2Block(reader, ddsFile.Pixels, bx * 4, by * 4, width, height);

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// BC3(DXT4/DXT5) 압축 DDS를 디코딩해 픽셀 배열로 채운다.
        /// </summary>
        private static bool LoadBC3DDS(BinaryReader reader, ref DDSFile ddsFile)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            ddsFile.Pixels = new Color32[width * height];

            int blocksW = (width  + 3) / 4;
            int blocksH = (height + 3) / 4;
            for (int by = 0; by < blocksH; by++)
            for (int bx = 0; bx < blocksW; bx++)
                DecodeBC3Block(reader, ddsFile.Pixels, bx * 4, by * 4, width, height);

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// BC4(ATI1) 압축 DDS를 디코딩해 그레이스케일 픽셀 배열로 채운다.
        /// </summary>
        private static bool LoadBC4DDS(BinaryReader reader, ref DDSFile ddsFile)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            ddsFile.Pixels = new Color32[width * height];

            int blocksW = (width  + 3) / 4;
            int blocksH = (height + 3) / 4;
            for (int by = 0; by < blocksH; by++)
            for (int bx = 0; bx < blocksW; bx++)
                DecodeBC4Block(reader, ddsFile.Pixels, bx * 4, by * 4, width, height);

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// BC5(ATI2) 압축 DDS를 디코딩해 RG 노말맵 픽셀 배열로 채운다.
        /// </summary>
        private static bool LoadBC5DDS(BinaryReader reader, ref DDSFile ddsFile)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            ddsFile.Pixels = new Color32[width * height];

            int blocksW = (width  + 3) / 4;
            int blocksH = (height + 3) / 4;
            for (int by = 0; by < blocksH; by++)
            for (int bx = 0; bx < blocksW; bx++)
                DecodeBC5Block(reader, ddsFile.Pixels, bx * 4, by * 4, width, height);

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// 비압축 RGB/RGBA DDS 픽셀 데이터를 읽어 Color32 배열로 채운다.
        /// </summary>
        private static bool LoadUncompressedDDS(BinaryReader reader, ref DDSFile ddsFile)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            var pf = ddsFile.Header.PixelFormat;
            int bytesPerPixel = ((int)pf.RGBBitCount + 7) / 8;
            bool hasAlpha = pf.HasFlag(DDSPixelFormatFlags.AlphaPixels);

            int shiftR = GetShiftCount(pf.RBitMask);
            int shiftG = GetShiftCount(pf.GBitMask);
            int shiftB = GetShiftCount(pf.BBitMask);
            int shiftA = GetShiftCount(pf.ABitMask);
            int bitsR = CountSetBits(pf.RBitMask);
            int bitsG = CountSetBits(pf.GBitMask);
            int bitsB = CountSetBits(pf.BBitMask);
            int bitsA = CountSetBits(pf.ABitMask);
            uint maxR = bitsR > 0 ? (1u << bitsR) - 1 : 255;
            uint maxG = bitsG > 0 ? (1u << bitsG) - 1 : 255;
            uint maxB = bitsB > 0 ? (1u << bitsB) - 1 : 255;
            uint maxA = bitsA > 0 ? (1u << bitsA) - 1 : 255;

            ddsFile.Pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                uint value = 0;
                for (int i = 0; i < bytesPerPixel; i++)
                    value |= (uint)reader.ReadByte() << (i * 8);

                byte r = (byte)(((value & pf.RBitMask) >> shiftR) * 255 / maxR);
                byte g = (byte)(((value & pf.GBitMask) >> shiftG) * 255 / maxG);
                byte b = (byte)(((value & pf.BBitMask) >> shiftB) * 255 / maxB);
                byte a = hasAlpha ? (byte)(((value & pf.ABitMask) >> shiftA) * 255 / maxA) : (byte)255;
                ddsFile.Pixels[y * width + x] = new Color32(r, g, b, a);
            }

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// 루미넌스(밝기) DDS 픽셀 데이터를 읽어 그레이스케일 Color32 배열로 채운다.
        /// </summary>
        private static bool LoadLuminanceDDS(BinaryReader reader, ref DDSFile ddsFile)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            ddsFile.Pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                byte v = reader.ReadByte();
                ddsFile.Pixels[y * width + x] = new Color32(v, v, v, 255);
            }

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// DX10 RGBA8 포맷 DDS 픽셀 데이터를 읽어 Color32 배열로 채운다.
        /// </summary>
        private static bool LoadDX10RGBA8DDS(BinaryReader reader, ref DDSFile ddsFile, bool hasAlpha)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            ddsFile.Pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                byte r = reader.ReadByte();
                byte g = reader.ReadByte();
                byte b = reader.ReadByte();
                byte a = reader.ReadByte();
                ddsFile.Pixels[y * width + x] = new Color32(r, g, b, hasAlpha ? a : (byte)255);
            }

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// DX10 BGRA8 포맷 DDS 픽셀 데이터를 읽어 RGBA Color32 배열로 채운다.
        /// </summary>
        private static bool LoadDX10BGRA8DDS(BinaryReader reader, ref DDSFile ddsFile, bool hasAlpha)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            ddsFile.Pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                byte b = reader.ReadByte();
                byte g = reader.ReadByte();
                byte r = reader.ReadByte();
                byte a = reader.ReadByte();
                ddsFile.Pixels[y * width + x] = new Color32(r, g, b, hasAlpha ? a : (byte)255);
            }

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// DX10 B5G6R5 포맷 DDS 픽셀 데이터를 읽어 Color32 배열로 채운다.
        /// </summary>
        private static bool LoadDX10B5G6R5DDS(BinaryReader reader, ref DDSFile ddsFile)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            ddsFile.Pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                ushort v = reader.ReadUInt16();
                byte b5 = (byte)((v >> 11) & 0x1F);
                byte g6 = (byte)((v >> 5)  & 0x3F);
                byte r5 = (byte)(v          & 0x1F);
                ddsFile.Pixels[y * width + x] = new Color32(
                    (byte)((r5 << 3) | (r5 >> 2)),
                    (byte)((g6 << 2) | (g6 >> 4)),
                    (byte)((b5 << 3) | (b5 >> 2)),
                    255);
            }

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// DX10 B5G5R5A1 포맷 DDS 픽셀 데이터를 읽어 Color32 배열로 채운다.
        /// </summary>
        private static bool LoadDX10B5G5R5A1DDS(BinaryReader reader, ref DDSFile ddsFile)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            ddsFile.Pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                ushort v = reader.ReadUInt16();
                byte a1 = (byte)((v >> 15) & 0x1);
                byte b5 = (byte)((v >> 10) & 0x1F);
                byte g5 = (byte)((v >> 5)  & 0x1F);
                byte r5 = (byte)(v          & 0x1F);
                ddsFile.Pixels[y * width + x] = new Color32(
                    (byte)((r5 << 3) | (r5 >> 2)),
                    (byte)((g5 << 3) | (g5 >> 2)),
                    (byte)((b5 << 3) | (b5 >> 2)),
                    (byte)(a1 * 255));
            }

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// DX10 B4G4R4A4 포맷 DDS 픽셀 데이터를 읽어 Color32 배열로 채운다.
        /// </summary>
        private static bool LoadDX10B4G4R4A4DDS(BinaryReader reader, ref DDSFile ddsFile)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            ddsFile.Pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                ushort v = reader.ReadUInt16();
                byte a4 = (byte)((v >> 12) & 0xF);
                byte r4 = (byte)((v >> 8)  & 0xF);
                byte g4 = (byte)((v >> 4)  & 0xF);
                byte b4 = (byte)(v          & 0xF);
                ddsFile.Pixels[y * width + x] = new Color32(
                    (byte)(r4 * 17),
                    (byte)(g4 * 17),
                    (byte)(b4 * 17),
                    (byte)(a4 * 17));
            }

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// DX10 R8 또는 A8 포맷 DDS 픽셀 데이터를 읽어 Color32 배열로 채운다.
        /// </summary>
        private static bool LoadDX10R8DDS(BinaryReader reader, ref DDSFile ddsFile, bool isAlphaOnly)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            ddsFile.Pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                byte v = reader.ReadByte();
                ddsFile.Pixels[y * width + x] = isAlphaOnly
                    ? new Color32(0, 0, 0, v)
                    : new Color32(v, v, v, 255);
            }

            FlipDDS(ref ddsFile);
            return true;
        }

        /// <summary>
        /// BC1 블록 하나를 디코딩해 픽셀 배열에 쓴다.
        /// </summary>
        private static void DecodeBC1Block(BinaryReader reader, Color32[] pixels,
            int blockX, int blockY, int width, int height, bool force4Color)
        {
            ushort c0 = reader.ReadUInt16();
            ushort c1 = reader.ReadUInt16();
            uint indices = reader.ReadUInt32();

            Color32[] colors = new Color32[4];
            colors[0] = DDSExpand565(c0);
            colors[1] = DDSExpand565(c1);

            if (force4Color || c0 > c1)
            {
                colors[2] = new Color32(
                    (byte)((2 * colors[0].r + colors[1].r + 1) / 3),
                    (byte)((2 * colors[0].g + colors[1].g + 1) / 3),
                    (byte)((2 * colors[0].b + colors[1].b + 1) / 3),
                    255);
                colors[3] = new Color32(
                    (byte)((colors[0].r + 2 * colors[1].r + 1) / 3),
                    (byte)((colors[0].g + 2 * colors[1].g + 1) / 3),
                    (byte)((colors[0].b + 2 * colors[1].b + 1) / 3),
                    255);
            }
            else
            {
                colors[2] = new Color32(
                    (byte)((colors[0].r + colors[1].r) / 2),
                    (byte)((colors[0].g + colors[1].g) / 2),
                    (byte)((colors[0].b + colors[1].b) / 2),
                    255);
                colors[3] = new Color32(0, 0, 0, 0);
            }

            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
            {
                int px = blockX + col;
                int py = blockY + row;
                if (px >= width || py >= height) continue;
                int idx = (int)((indices >> ((row * 4 + col) * 2)) & 0x3);
                pixels[py * width + px] = colors[idx];
            }
        }

        /// <summary>
        /// BC2 블록 하나를 디코딩해 픽셀 배열에 쓴다.
        /// </summary>
        private static void DecodeBC2Block(BinaryReader reader, Color32[] pixels,
            int blockX, int blockY, int width, int height)
        {
            byte[] alpha = new byte[16];
            for (int i = 0; i < 8; i++)
            {
                byte pair = reader.ReadByte();
                alpha[i * 2]     = (byte)((pair & 0xF) * 17);
                alpha[i * 2 + 1] = (byte)(((pair >> 4) & 0xF) * 17);
            }

            ushort c0 = reader.ReadUInt16();
            ushort c1 = reader.ReadUInt16();
            uint indices = reader.ReadUInt32();

            Color32[] colors = new Color32[4];
            colors[0] = DDSExpand565(c0);
            colors[1] = DDSExpand565(c1);
            colors[2] = new Color32(
                (byte)((2 * colors[0].r + colors[1].r + 1) / 3),
                (byte)((2 * colors[0].g + colors[1].g + 1) / 3),
                (byte)((2 * colors[0].b + colors[1].b + 1) / 3),
                255);
            colors[3] = new Color32(
                (byte)((colors[0].r + 2 * colors[1].r + 1) / 3),
                (byte)((colors[0].g + 2 * colors[1].g + 1) / 3),
                (byte)((colors[0].b + 2 * colors[1].b + 1) / 3),
                255);

            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
            {
                int px = blockX + col;
                int py = blockY + row;
                if (px >= width || py >= height) continue;
                int i = row * 4 + col;
                int idx = (int)((indices >> (i * 2)) & 0x3);
                Color32 color = colors[idx];
                color.a = alpha[i];
                pixels[py * width + px] = color;
            }
        }

        /// <summary>
        /// BC3 블록 하나를 디코딩해 픽셀 배열에 쓴다.
        /// </summary>
        private static void DecodeBC3Block(BinaryReader reader, Color32[] pixels,
            int blockX, int blockY, int width, int height)
        {
            byte[] alpha = DecodeBC3AlphaBlock(reader);

            ushort c0 = reader.ReadUInt16();
            ushort c1 = reader.ReadUInt16();
            uint indices = reader.ReadUInt32();

            Color32[] colors = new Color32[4];
            colors[0] = DDSExpand565(c0);
            colors[1] = DDSExpand565(c1);
            colors[2] = new Color32(
                (byte)((2 * colors[0].r + colors[1].r + 1) / 3),
                (byte)((2 * colors[0].g + colors[1].g + 1) / 3),
                (byte)((2 * colors[0].b + colors[1].b + 1) / 3),
                255);
            colors[3] = new Color32(
                (byte)((colors[0].r + 2 * colors[1].r + 1) / 3),
                (byte)((colors[0].g + 2 * colors[1].g + 1) / 3),
                (byte)((colors[0].b + 2 * colors[1].b + 1) / 3),
                255);

            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
            {
                int px = blockX + col;
                int py = blockY + row;
                if (px >= width || py >= height) continue;
                int i = row * 4 + col;
                int idx = (int)((indices >> (i * 2)) & 0x3);
                Color32 color = colors[idx];
                color.a = alpha[i];
                pixels[py * width + px] = color;
            }
        }

        /// <summary>
        /// BC4 블록 하나를 디코딩해 그레이스케일 픽셀을 픽셀 배열에 쓴다.
        /// </summary>
        private static void DecodeBC4Block(BinaryReader reader, Color32[] pixels,
            int blockX, int blockY, int width, int height)
        {
            byte[] values = DecodeBC3AlphaBlock(reader);
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
            {
                int px = blockX + col;
                int py = blockY + row;
                if (px >= width || py >= height) continue;
                byte v = values[row * 4 + col];
                pixels[py * width + px] = new Color32(v, v, v, 255);
            }
        }

        /// <summary>
        /// BC5 블록 하나를 디코딩해 RG 채널 값을 픽셀 배열에 쓴다.
        /// </summary>
        private static void DecodeBC5Block(BinaryReader reader, Color32[] pixels,
            int blockX, int blockY, int width, int height)
        {
            byte[] rVals = DecodeBC3AlphaBlock(reader);
            byte[] gVals = DecodeBC3AlphaBlock(reader);
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
            {
                int px = blockX + col;
                int py = blockY + row;
                if (px >= width || py >= height) continue;
                int i = row * 4 + col;
                pixels[py * width + px] = new Color32(rVals[i], gVals[i], 0, 255);
            }
        }

        /// <summary>
        /// BC3/BC4/BC5에서 사용되는 알파 보간 블록을 디코딩해 16개의 값을 반환한다.
        /// </summary>
        private static byte[] DecodeBC3AlphaBlock(BinaryReader reader)
        {
            byte a0 = reader.ReadByte();
            byte a1 = reader.ReadByte();

            byte[] table = new byte[8];
            table[0] = a0;
            table[1] = a1;
            if (a0 > a1)
            {
                table[2] = (byte)((6 * a0 + 1 * a1) / 7);
                table[3] = (byte)((5 * a0 + 2 * a1) / 7);
                table[4] = (byte)((4 * a0 + 3 * a1) / 7);
                table[5] = (byte)((3 * a0 + 4 * a1) / 7);
                table[6] = (byte)((2 * a0 + 5 * a1) / 7);
                table[7] = (byte)((1 * a0 + 6 * a1) / 7);
            }
            else
            {
                table[2] = (byte)((4 * a0 + 1 * a1) / 5);
                table[3] = (byte)((3 * a0 + 2 * a1) / 5);
                table[4] = (byte)((2 * a0 + 3 * a1) / 5);
                table[5] = (byte)((1 * a0 + 4 * a1) / 5);
                table[6] = 0;
                table[7] = 255;
            }

            byte[] indexBytes = reader.ReadBytes(6);
            ulong bits = (ulong)indexBytes[0]        | ((ulong)indexBytes[1] << 8)  |
                         ((ulong)indexBytes[2] << 16) | ((ulong)indexBytes[3] << 24) |
                         ((ulong)indexBytes[4] << 32) | ((ulong)indexBytes[5] << 40);

            byte[] result = new byte[16];
            for (int i = 0; i < 16; i++)
                result[i] = table[(bits >> (i * 3)) & 0x7];
            return result;
        }

        /// <summary>
        /// RGB565 인코딩 값을 Color32(RGB888)로 변환해 반환한다.
        /// </summary>
        private static Color32 DDSExpand565(ushort value)
        {
            byte r5 = (byte)((value >> 11) & 0x1F);
            byte g6 = (byte)((value >> 5)  & 0x3F);
            byte b5 = (byte)(value          & 0x1F);
            return new Color32(
                (byte)((r5 << 3) | (r5 >> 2)),
                (byte)((g6 << 2) | (g6 >> 4)),
                (byte)((b5 << 3) | (b5 >> 2)),
                255);
        }

        /// <summary>
        /// DDS 픽셀 배열을 수직으로 뒤집어 상하 방향을 보정한다.
        /// </summary>
        private static void FlipDDS(ref DDSFile ddsFile)
        {
            int width  = (int)ddsFile.Header.Width;
            int height = (int)ddsFile.Header.Height;
            int center = height / 2;
            for (int y = 0; y < center; y++)
            for (int x = 0; x < width; x++)
            {
                int a = y * width + x;
                int b = (height - 1 - y) * width + x;
                (ddsFile.Pixels[a], ddsFile.Pixels[b]) = (ddsFile.Pixels[b], ddsFile.Pixels[a]);
            }
        }
    }
}
