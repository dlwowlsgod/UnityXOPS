using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace JJLUtility.IO
{
    /// <summary>
    /// 파싱된 BMP 파일 데이터를 담는 컨테이너 클래스.
    /// </summary>
    public class BMPFile
    {
        public BMPFileHeader FileHeader;
        public BMPInfoHeader InfoHeader;
        public Color32[] Palettes;
        public Color32[] Pixels;
    }

    public partial class ImageLoader
    {
        /// <summary>
        /// 지정 경로의 BMP 파일을 파싱해 BMPFile 객체로 반환한다.
        /// </summary>
        /// <param name="filepath">BMP 파일 경로.</param>
        /// <returns>파싱된 BMPFile. 실패 시 null.</returns>
        private static BMPFile LoadBMPFile(string filepath)
        {
            if (Path.GetExtension(filepath).ToLower() != ".bmp")
            {
                Debugger.LogError($"File is not .bmp file: {filepath}", Instance, nameof(ImageLoader));
                return null;
                ;
            }

            if (!File.Exists(filepath))
            {
                Debugger.LogError($"File not found: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            var stream = File.OpenRead(filepath);
            var binaryReader = new BinaryReader(stream);

            if (binaryReader.ReadUInt16() != BMPFileHeader.Type)
            {
                Debugger.LogError($"File is not .bmp file: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            var bmpFile = new BMPFile();

            bmpFile.FileHeader = LoadBMPFileHeader(binaryReader);

            var bmpInfoHeader = LoadBMPInfoHeader(binaryReader);

            if (bmpInfoHeader.Compression != BMPCompression.BI_RGB &&
                bmpInfoHeader.Compression != BMPCompression.BI_RLE8 &&
                bmpInfoHeader.Compression != BMPCompression.BI_RLE4 &&
                bmpInfoHeader.Compression != BMPCompression.BI_BITFIELDS &&
                bmpInfoHeader.Compression != BMPCompression.BI_ALPHABITFIELDS)
            {
                Debugger.LogError($"Unsupported .bmp compression method: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            // 비트마스크 읽기
            if (bmpInfoHeader.BitCount > 8 && (bmpInfoHeader.Compression == BMPCompression.BI_BITFIELDS ||
                                               bmpInfoHeader.Compression == BMPCompression.BI_ALPHABITFIELDS))
            {
                bmpInfoHeader.RedMask = binaryReader.ReadUInt32();
                bmpInfoHeader.GreenMask = binaryReader.ReadUInt32();
                bmpInfoHeader.BlueMask = binaryReader.ReadUInt32();
                if (bmpInfoHeader.Compression == BMPCompression.BI_ALPHABITFIELDS ||
                    bmpInfoHeader.Size >= 56)
                {
                    bmpInfoHeader.AlphaMask = binaryReader.ReadUInt32();
                }
            }
            else if (bmpInfoHeader.BitCount == 16)
            {
                bmpInfoHeader.RedMask = 0x7C00;
                bmpInfoHeader.GreenMask = 0x03E0;
                bmpInfoHeader.BlueMask = 0x001F;
                bmpInfoHeader.AlphaMask = 0;
            }
            else
            {
                bmpInfoHeader.BlueMask = 0x000000FF;
                bmpInfoHeader.GreenMask = 0x0000FF00;
                bmpInfoHeader.RedMask = 0x00FF0000;
                bmpInfoHeader.AlphaMask = 0;
            }

            long headerEnd = 14 + bmpInfoHeader.Size;
            if (binaryReader.BaseStream.Position > headerEnd)
                headerEnd = binaryReader.BaseStream.Position;
            binaryReader.BaseStream.Seek(headerEnd, SeekOrigin.Begin);

            if (bmpInfoHeader.BitCount <= 8)
            {
                if (bmpInfoHeader.ColorUsed == 0)
                {
                    bmpInfoHeader.ColorUsed = 1u << bmpInfoHeader.BitCount;
                }

                bmpFile.Palettes = new Color32[bmpInfoHeader.ColorUsed];

                for (int i = 0; i < bmpFile.Palettes.Length; i++)
                {
                    bmpFile.Palettes[i] = new Color32
                    {
                        b = binaryReader.ReadByte(),
                        g = binaryReader.ReadByte(),
                        r = binaryReader.ReadByte(),
                        a = 255
                    };
                    binaryReader.ReadByte();
                }
            }

            bmpFile.InfoHeader = bmpInfoHeader;

            binaryReader.BaseStream.Seek(bmpFile.FileHeader.Offset, SeekOrigin.Begin);

            bool uncompressed = bmpInfoHeader.Compression == BMPCompression.BI_RGB ||
                                bmpInfoHeader.Compression == BMPCompression.BI_BITFIELDS ||
                                bmpInfoHeader.Compression == BMPCompression.BI_ALPHABITFIELDS;

            if (uncompressed)
            {
                if (bmpInfoHeader.BitCount == 32)
                {
                    if (Load32BitBMPFile(binaryReader, ref bmpFile) == false)
                    {
                        Debugger.LogError(
                            $"File is corrupted: {filepath}\nCompression: {bmpInfoHeader.Compression}\nMode: {bmpInfoHeader.BitCount}bit",
                            Instance, nameof(ImageLoader));
                        return null;
                    }

                    return bmpFile;
                }
                if (bmpInfoHeader.BitCount == 24)
                {
                    if (Load24BitBMPFile(binaryReader, ref bmpFile) == false)
                    {
                        Debugger.LogError(
                            $"File is corrupted: {filepath}\nCompression: {bmpInfoHeader.Compression}\nMode: {bmpInfoHeader.BitCount}bit",
                            Instance, nameof(ImageLoader));
                        return null;
                    }

                    return bmpFile;
                }
                if (bmpInfoHeader.BitCount == 16)
                {
                    if (Load16BitBMPFile(binaryReader, ref bmpFile) == false)
                    {
                        Debugger.LogError(
                            $"File is corrupted: {filepath}\nCompression: {bmpInfoHeader.Compression}\nMode: {bmpInfoHeader.BitCount}bit",
                            Instance, nameof(ImageLoader));
                        return null;
                    }

                    return bmpFile;
                }
                if (bmpInfoHeader.BitCount <= 8 && bmpFile.Palettes != null)
                {
                    if (LoadIndexedBMPFile(binaryReader, ref bmpFile) == false)
                    {
                        Debugger.LogError(
                            $"File is corrupted: {filepath}\nCompression: {bmpInfoHeader.Compression}\nMode: {bmpInfoHeader.BitCount}bit",
                            Instance, nameof(ImageLoader));
                        return null;
                    }

                    return bmpFile;
                }
            }
            if (bmpFile.Palettes != null)
            {
                if (bmpInfoHeader.Compression == BMPCompression.BI_RLE4 && bmpInfoHeader.BitCount == 4)
                {
                    if (LoadRLE4BMPFile(binaryReader, ref bmpFile) == false)
                    {
                        Debugger.LogError(
                            $"File is corrupted: {filepath}\nCompression: {bmpInfoHeader.Compression}\nMode: {bmpInfoHeader.BitCount}bit",
                            Instance, nameof(ImageLoader));
                        return null;
                    }

                    return bmpFile;
                }

                if (bmpInfoHeader.Compression == BMPCompression.BI_RLE8 && bmpInfoHeader.BitCount == 8)
                {
                    if (LoadRLE8BMPFile(binaryReader, ref bmpFile) == false)
                    {
                        Debugger.LogError(
                            $"File is corrupted: {filepath}\nCompression: {bmpInfoHeader.Compression}\nMode: {bmpInfoHeader.BitCount}bit",
                            Instance, nameof(ImageLoader));
                        return null;
                    }

                    return bmpFile;
                }
            }

            Debugger.LogError(
                $"File is corrupted: {filepath}\nCompression: Unknown \nMode: Unknown",
                Instance, nameof(ImageLoader));
            return null;
        }

        /// <summary>
        /// BinaryReader로부터 BMP 파일 헤더를 읽어 반환한다.
        /// </summary>
        private static BMPFileHeader LoadBMPFileHeader(BinaryReader binaryReader)
        {
            var fileHeader = new BMPFileHeader();
            fileHeader.Size = binaryReader.ReadUInt32();
            binaryReader.ReadInt32();
            fileHeader.Offset = binaryReader.ReadUInt32();
            return fileHeader;
        }

        /// <summary>
        /// BinaryReader로부터 BMP 정보 헤더를 읽어 반환한다.
        /// </summary>
        private static BMPInfoHeader LoadBMPInfoHeader(BinaryReader binaryReader)
        {
            var infoHeader = new BMPInfoHeader();
            infoHeader.Size = binaryReader.ReadUInt32();
            infoHeader.Width = binaryReader.ReadInt32();
            infoHeader.Height = binaryReader.ReadInt32();
            binaryReader.ReadInt16();
            infoHeader.BitCount = binaryReader.ReadUInt16();
            infoHeader.Compression = (BMPCompression)binaryReader.ReadUInt32();
            infoHeader.SizeImage = binaryReader.ReadUInt32();
            infoHeader.XPixelPerMeter = binaryReader.ReadInt32();
            infoHeader.YPixelPerMeter = binaryReader.ReadInt32();
            infoHeader.ColorUsed = binaryReader.ReadUInt32();
            infoHeader.ColorImportant = binaryReader.ReadUInt32();

            return infoHeader;
        }

        /// <summary>
        /// 32비트 BMP 픽셀 데이터를 읽어 Color32 배열로 채운다.
        /// </summary>
        private static bool Load32BitBMPFile(BinaryReader binaryReader, ref BMPFile bmpFile)
        {
            int width = Mathf.Abs(bmpFile.InfoHeader.Width);
            int height = Mathf.Abs(bmpFile.InfoHeader.Height);
            bmpFile.Pixels = new Color32[width * height];

            long rowCount = (long)width * height * 4;
            
            if (binaryReader.BaseStream.Position + rowCount > binaryReader.BaseStream.Length)
            {
                Debugger.LogError(
                    $"Unexpected end of file.\nHave: {binaryReader.BaseStream.Position + rowCount}\nExpected: {binaryReader.BaseStream.Length}",
                    Instance, nameof(ImageLoader));
                return false;
            }

            int shiftR = GetShiftCount(bmpFile.InfoHeader.RedMask);
            int shiftG = GetShiftCount(bmpFile.InfoHeader.GreenMask);
            int shiftB = GetShiftCount(bmpFile.InfoHeader.BlueMask);
            int shiftA = GetShiftCount(bmpFile.InfoHeader.AlphaMask);

            int bitsR = CountSetBits(bmpFile.InfoHeader.RedMask);
            int bitsG = CountSetBits(bmpFile.InfoHeader.GreenMask);
            int bitsB = CountSetBits(bmpFile.InfoHeader.BlueMask);
            int bitsA = CountSetBits(bmpFile.InfoHeader.AlphaMask);

            uint maxR = (1u << bitsR) - 1;
            if (maxR == 0) maxR = 255;
            uint maxG = (1u << bitsG) - 1;
            if (maxG == 0) maxG = 255;
            uint maxB = (1u << bitsB) - 1;
            if (maxB == 0) maxB = 255;
            uint maxA = (1u << bitsA) - 1;
            if (maxA == 0) maxA = 255;

            for (int y = 0; y < height; y++)
            {
                //flip
                int pixelY = (bmpFile.InfoHeader.Height > 0) ? y : (height - 1 - y);

                for (int x = 0; x < width; x++)
                {
                    uint value = binaryReader.ReadUInt32();

                    byte r = (byte)((((value & bmpFile.InfoHeader.RedMask) >> shiftR) * 255) / maxR);
                    byte g = (byte)((((value & bmpFile.InfoHeader.GreenMask) >> shiftG) * 255) / maxG);
                    byte b = (byte)((((value & bmpFile.InfoHeader.BlueMask) >> shiftB) * 255) / maxB);
                    byte a;

                    if (bitsA > 0)
                    {
                        a = (byte)((((value & bmpFile.InfoHeader.AlphaMask) >> shiftA) * 255) / maxA);
                    }
                    else
                    {
                        a = 255;
                    }

                    bmpFile.Pixels[pixelY * width + x] = new Color32(r, g, b, a);
                }
            }

            return true;
        }


        /// <summary>
        /// 24비트 BMP 픽셀 데이터를 읽어 Color32 배열로 채운다.
        /// </summary>
        private static bool Load24BitBMPFile(BinaryReader binaryReader, ref BMPFile bmpFile)
        {
            int width = Mathf.Abs(bmpFile.InfoHeader.Width);
            int height = Mathf.Abs(bmpFile.InfoHeader.Height);

            int rowLength = ((24 * width + 31) / 32) * 4;
            int rowCount = rowLength * height;
            int rowPadding = rowLength - width * 3;
            bmpFile.Pixels = new Color32[width * height];

            if (binaryReader.BaseStream.Position + rowCount > binaryReader.BaseStream.Length)
            {
                Debugger.LogError(
                    $"Unexpected end of file.\nHave: {binaryReader.BaseStream.Position + rowCount}\nExpected: {binaryReader.BaseStream.Length}",
                    Instance, nameof(ImageLoader));
                return false;
            }

            int shiftR = GetShiftCount(bmpFile.InfoHeader.RedMask);
            int shiftG = GetShiftCount(bmpFile.InfoHeader.GreenMask);
            int shiftB = GetShiftCount(bmpFile.InfoHeader.BlueMask);

            int bitsR = CountSetBits(bmpFile.InfoHeader.RedMask);
            int bitsG = CountSetBits(bmpFile.InfoHeader.GreenMask);
            int bitsB = CountSetBits(bmpFile.InfoHeader.BlueMask);

            uint maxR = (1u << bitsR) - 1;
            if (maxR == 0) maxR = 255;
            uint maxG = (1u << bitsG) - 1;
            if (maxG == 0) maxG = 255;
            uint maxB = (1u << bitsB) - 1;
            if (maxB == 0) maxB = 255;

            for (int y = 0; y < height; y++)
            {
                //flip
                int pixelY = (bmpFile.InfoHeader.Height > 0) ? y : (height - 1 - y);

                for (int x = 0; x < width; x++)
                {
                    uint value = (uint)(binaryReader.ReadByte() | binaryReader.ReadByte() << 8 |
                                        binaryReader.ReadByte() << 16);

                    byte r = (byte)((((value & bmpFile.InfoHeader.RedMask) >> shiftR) * 255) / maxR);
                    byte g = (byte)((((value & bmpFile.InfoHeader.GreenMask) >> shiftG) * 255) / maxG);
                    byte b = (byte)((((value & bmpFile.InfoHeader.BlueMask) >> shiftB) * 255) / maxB);

                    bmpFile.Pixels[pixelY * width + x] = new Color32(r, g, b, 255);
                }

                for (int i = 0; i < rowPadding; i++)
                {
                    binaryReader.ReadByte();
                }
            }

            return true;
        }


        /// <summary>
        /// 16비트 BMP 픽셀 데이터를 읽어 Color32 배열로 채운다.
        /// </summary>
        private static bool Load16BitBMPFile(BinaryReader binaryReader, ref BMPFile bmpFile)
        {
            int width = Mathf.Abs(bmpFile.InfoHeader.Width);
            int height = Mathf.Abs(bmpFile.InfoHeader.Height);

            int rowLength = ((16 * width + 31) / 32) * 4;
            int rowCount = rowLength * height;
            int rowPadding = rowLength - width * 2;
            bmpFile.Pixels = new Color32[width * height];

            if (binaryReader.BaseStream.Position + rowCount > binaryReader.BaseStream.Length)
            {
                Debugger.LogError(
                    $"Unexpected end of file.\nHave: {binaryReader.BaseStream.Position + rowCount}\nExpected: {binaryReader.BaseStream.Length}",
                    Instance, nameof(ImageLoader));
                return false;
            }

            int shiftR = GetShiftCount(bmpFile.InfoHeader.RedMask);
            int shiftG = GetShiftCount(bmpFile.InfoHeader.GreenMask);
            int shiftB = GetShiftCount(bmpFile.InfoHeader.BlueMask);
            int shiftA = GetShiftCount(bmpFile.InfoHeader.AlphaMask);

            int bitsR = CountSetBits(bmpFile.InfoHeader.RedMask);
            int bitsG = CountSetBits(bmpFile.InfoHeader.GreenMask);
            int bitsB = CountSetBits(bmpFile.InfoHeader.BlueMask);
            int bitsA = CountSetBits(bmpFile.InfoHeader.AlphaMask);

            uint maxR = (1u << bitsR) - 1;
            if (maxR == 0) maxR = 255;
            uint maxG = (1u << bitsG) - 1;
            if (maxG == 0) maxG = 255;
            uint maxB = (1u << bitsB) - 1;
            if (maxB == 0) maxB = 255;
            uint maxA = (1u << bitsA) - 1;
            if (maxA == 0) maxA = 255;

            for (int y = 0; y < height; y++)
            {
                //flip
                int pixelY = (bmpFile.InfoHeader.Height > 0) ? y : (height - 1 - y);

                for (int x = 0; x < width; x++)
                {
                    uint value = (uint)(binaryReader.ReadByte() | binaryReader.ReadByte() << 8);
                    byte r = (byte)((((value & bmpFile.InfoHeader.RedMask) >> shiftR) * 255) / maxR);
                    byte g = (byte)((((value & bmpFile.InfoHeader.GreenMask) >> shiftG) * 255) / maxG);
                    byte b = (byte)((((value & bmpFile.InfoHeader.BlueMask) >> shiftB) * 255) / maxB);
                    byte a;

                    if (bitsA > 0)
                    {
                        a = (byte)((((value & bmpFile.InfoHeader.AlphaMask) >> shiftA) * 255) / maxA);
                    }
                    else
                    {
                        a = 255;
                    }

                    bmpFile.Pixels[pixelY * width + x] = new Color32(r, g, b, a);
                }

                for (int i = 0; i < rowPadding; i++)
                {
                    binaryReader.ReadByte();
                }
            }

            return true;
        }

        /// <summary>
        /// 팔레트 인덱스 방식 BMP 픽셀 데이터를 읽어 Color32 배열로 채운다.
        /// </summary>
        private static bool LoadIndexedBMPFile(BinaryReader binaryReader, ref BMPFile bmpFile)
        {
            int width = Mathf.Abs(bmpFile.InfoHeader.Width);
            int height = Mathf.Abs(bmpFile.InfoHeader.Height);

            int rowLength = ((bmpFile.InfoHeader.BitCount * width + 31) / 32) * 4;
            int rowCount = rowLength * height;
            int rowPadding = rowLength - (width * bmpFile.InfoHeader.BitCount + 7) / 8;
            bmpFile.Pixels = new Color32[width * height];
            
            if (binaryReader.BaseStream.Position + rowCount > binaryReader.BaseStream.Length)
            {
                Debugger.LogError(
                    $"Unexpected end of file.\nHave: {binaryReader.BaseStream.Position + rowCount}\nExpected: {binaryReader.BaseStream.Length}",
                    Instance, nameof(ImageLoader));
                return false;
            }

            var bitReader = new BitReader(binaryReader.BaseStream, Encoding.Default, true);
            for (int y = 0; y < height; y++)
            {
                //flip
                int pixelY = (bmpFile.InfoHeader.Height > 0) ? y : (height - 1 - y);
                
                for (int x = 0; x < width; x++)
                {
                    int value = (int)bitReader.ReadBits(bmpFile.InfoHeader.BitCount);
                    if (value >= bmpFile.Palettes.Length)
                    {
                        Debugger.LogError(
                            "Palette index out of range.",
                            Instance, nameof(ImageLoader));
                        return false;
                    }

                    bmpFile.Pixels[pixelY * width + x] = bmpFile.Palettes[value];
                }

                bitReader.ResetBitBuffer();

                for (int i = 0; i < rowPadding; i++)
                {
                    binaryReader.ReadByte();
                }
            }

            return true;
        }

        /// <summary>
        /// RLE4 압축 BMP 픽셀 데이터를 디코딩해 Color32 배열로 채운다.
        /// </summary>
        private static bool LoadRLE4BMPFile(BinaryReader binaryReader, ref BMPFile bmpFile)
        {
            int width = Mathf.Abs(bmpFile.InfoHeader.Width);
            int height = Mathf.Abs(bmpFile.InfoHeader.Height);
            bmpFile.Pixels = new Color32[width * height];

            int x = 0, y = 0, yOffset = 0;

            while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length - 1)
            {
                int count = binaryReader.ReadByte();
                byte d = binaryReader.ReadByte();
                if (count > 0)
                {
                    for (int i = (count / 2); i > 0; i--)
                    {
                        bmpFile.Pixels[x++ + yOffset] = bmpFile.Palettes[(d >> 4) & 0x0F];
                        bmpFile.Pixels[x++ + yOffset] = bmpFile.Palettes[d & 0x0F];
                    }

                    if ((count & 0x01) > 0)
                    {
                        bmpFile.Pixels[x++ + yOffset] = bmpFile.Palettes[(d >> 4) & 0x0F];
                    }
                }
                else
                {
                    if (d == 0)
                    {
                        x = 0;
                        y += 1;
                        yOffset = y * width;
                    }
                    else if (d == 1)
                    {
                        break;
                    }
                    else if (d == 2)
                    {
                        x += binaryReader.ReadByte();
                        y += binaryReader.ReadByte();
                        yOffset = y * width;
                    }
                    else
                    {
                        for (int i = (d / 2); i > 0; i--)
                        {
                            byte d2 = binaryReader.ReadByte();
                            bmpFile.Pixels[x++ + yOffset] = bmpFile.Palettes[(d2 >> 4) & 0x0F];
                            if (x + 1 < width)
                            {
                                bmpFile.Pixels[x++ + yOffset] = bmpFile.Palettes[d2 & 0x0F];
                            }
                        }

                        if ((d & 0x01) > 0)
                        {
                            bmpFile.Pixels[x++ + yOffset] =
                                bmpFile.Palettes[(binaryReader.ReadByte() >> 4) & 0x0F];
                        }

                        if ((((d - 1) / 2) & 1) == 0)
                        {
                            binaryReader.ReadByte();
                        }
                    }
                }
            }

            FlipHeight(ref bmpFile);

            return true;
        }

        /// <summary>
        /// RLE8 압축 BMP 픽셀 데이터를 디코딩해 Color32 배열로 채운다.
        /// </summary>
        private static bool LoadRLE8BMPFile(BinaryReader binaryReader, ref BMPFile bmpFile)
        {
            int width = Mathf.Abs(bmpFile.InfoHeader.Width);
            int height = Mathf.Abs(bmpFile.InfoHeader.Height);
            bmpFile.Pixels = new Color32[width * height];

            int x = 0, y = 0, yOffset = 0;
            while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length - 1)
            {
                int count = binaryReader.ReadByte();
                byte d = binaryReader.ReadByte();
                if (count > 0)
                {
                    for (int i = count; i > 0; i--)
                    {
                        bmpFile.Pixels[x++ + yOffset] = bmpFile.Palettes[d];
                    }
                }
                else
                {
                    if (d == 0)
                    {
                        x = 0;
                        y += 1;
                        yOffset = y * width;
                    }
                    else if (d == 1)
                    {
                        break;
                    }
                    else if (d == 2)
                    {
                        x += binaryReader.ReadByte();
                        y += binaryReader.ReadByte();
                        yOffset = y * width;
                    }
                    else
                    {
                        for (int i = d; i > 0; i--)
                        {
                            bmpFile.Pixels[x++ + yOffset] = bmpFile.Palettes[binaryReader.ReadByte()];
                        }

                        if ((d & 0x01) > 0)
                        {
                            binaryReader.ReadByte();
                        }
                    }
                }
            }

            FlipHeight(ref bmpFile);
            
            return true;
        }

        /// <summary>
        /// BMP 픽셀 배열을 수직으로 뒤집어 상하 방향을 보정한다.
        /// </summary>
        private static void FlipHeight(ref BMPFile bmpFile)
        {
            int width = Mathf.Abs(bmpFile.InfoHeader.Width);
            int height = Mathf.Abs(bmpFile.InfoHeader.Height);
            int center = height / 2;

            for (int y = 0; y < center; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var offset0 = y * width;
                    var offset1 = (height - y - 1) * width;
                    (bmpFile.Pixels[offset0], bmpFile.Pixels[offset1]) =
                        (bmpFile.Pixels[offset1], bmpFile.Pixels[offset0]);
                }
            }

            bmpFile.InfoHeader.Height = height;
        }

        /// <summary>
        /// 비트마스크에서 최하위 세트 비트의 위치(시프트 수)를 반환한다.
        /// </summary>
        private static int GetShiftCount(uint mask)
        {
            if (mask == 0) return 0;

            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// 정수에서 1로 설정된 비트 수를 계산해 반환한다.
        /// </summary>
        private static int CountSetBits(uint n)
        {
            int count = 0;
            while (n > 0)
            {
                n &= (n - 1);
                count++;
            }

            return count;
        }
    }
}