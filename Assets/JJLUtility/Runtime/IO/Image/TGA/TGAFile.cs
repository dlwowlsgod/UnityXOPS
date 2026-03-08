using UnityEngine;
using System.IO;

namespace JJLUtility.IO
{
    public class TGAFile
    {
        public TGAHeader Header;
        public Color32[] Palettes;
        public Color32[] Pixels;
    }

    public partial class ImageLoader
    {
        private static TGAFile LoadTGAFile(string filepath)
        {
            if (Path.GetExtension(filepath).ToLower() != ".tga")
            {
                Debugger.LogError($"File is not .tga file: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            if (!File.Exists(filepath))
            {
                Debugger.LogError($"File not found: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            var stream = File.OpenRead(filepath);
            var binaryReader = new BinaryReader(stream);

            var tgaFile = new TGAFile();
            tgaFile.Header = LoadTGAHeader(binaryReader);

            if (tgaFile.Header.ImageType == TGAImageType.NoImage)
            {
                Debugger.LogError($"TGA has no image data: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            if (tgaFile.Header.IDLength > 0)
                binaryReader.BaseStream.Seek(tgaFile.Header.IDLength, SeekOrigin.Current);

            if (tgaFile.Header.ColorMapType == 1)
                LoadTGAColorMap(binaryReader, ref tgaFile);

            bool success;
            switch (tgaFile.Header.ImageType)
            {
                case TGAImageType.TrueColor:
                case TGAImageType.TrueColorRLE:
                    success = LoadTrueColorTGA(binaryReader, ref tgaFile);
                    break;
                case TGAImageType.ColorMapped:
                case TGAImageType.ColorMappedRLE:
                    success = LoadColorMappedTGA(binaryReader, ref tgaFile);
                    break;
                case TGAImageType.Grayscale:
                case TGAImageType.GrayscaleRLE:
                    success = LoadGrayscaleTGA(binaryReader, ref tgaFile);
                    break;
                default:
                    Debugger.LogError($"Unsupported TGA image type: {tgaFile.Header.ImageType} ({filepath})", Instance, nameof(ImageLoader));
                    return null;
            }

            if (!success)
            {
                Debugger.LogError($"File is corrupted: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            return tgaFile;
        }

        private static TGAHeader LoadTGAHeader(BinaryReader binaryReader)
        {
            var header = new TGAHeader();
            header.IDLength        = binaryReader.ReadByte();
            header.ColorMapType    = binaryReader.ReadByte();
            header.ImageType       = (TGAImageType)binaryReader.ReadByte();
            header.ColorMapStart   = binaryReader.ReadUInt16();
            header.ColorMapLength  = binaryReader.ReadUInt16();
            header.ColorMapDepth   = binaryReader.ReadByte();
            header.XOrigin         = binaryReader.ReadUInt16();
            header.YOrigin         = binaryReader.ReadUInt16();
            header.Width           = binaryReader.ReadUInt16();
            header.Height          = binaryReader.ReadUInt16();
            header.PixelDepth      = binaryReader.ReadByte();
            header.ImageDescriptor = binaryReader.ReadByte();
            return header;
        }

        private static void LoadTGAColorMap(BinaryReader binaryReader, ref TGAFile tgaFile)
        {
            int count = tgaFile.Header.ColorMapLength;
            int depth = tgaFile.Header.ColorMapDepth;
            tgaFile.Palettes = new Color32[count];
            for (int i = 0; i < count; i++)
                tgaFile.Palettes[i] = ReadTGAColor(binaryReader, depth, 0);
        }

        private static bool LoadTrueColorTGA(BinaryReader binaryReader, ref TGAFile tgaFile)
        {
            int depth = tgaFile.Header.PixelDepth;
            bool isRLE = tgaFile.Header.ImageType == TGAImageType.TrueColorRLE;
            tgaFile.Pixels = new Color32[tgaFile.Header.Width * tgaFile.Header.Height];

            return isRLE
                ? ReadTGARLEPixels(binaryReader, ref tgaFile, depth, null, false)
                : ReadTGARawPixels(binaryReader, ref tgaFile, depth, null, false);
        }

        private static bool LoadColorMappedTGA(BinaryReader binaryReader, ref TGAFile tgaFile)
        {
            if (tgaFile.Palettes == null)
            {
                Debugger.LogError("Color-mapped TGA has no color map.", Instance, nameof(ImageLoader));
                return false;
            }

            int depth = tgaFile.Header.PixelDepth;
            bool isRLE = tgaFile.Header.ImageType == TGAImageType.ColorMappedRLE;
            tgaFile.Pixels = new Color32[tgaFile.Header.Width * tgaFile.Header.Height];

            return isRLE
                ? ReadTGARLEPixels(binaryReader, ref tgaFile, depth, tgaFile.Palettes, false)
                : ReadTGARawPixels(binaryReader, ref tgaFile, depth, tgaFile.Palettes, false);
        }

        private static bool LoadGrayscaleTGA(BinaryReader binaryReader, ref TGAFile tgaFile)
        {
            bool isRLE = tgaFile.Header.ImageType == TGAImageType.GrayscaleRLE;
            tgaFile.Pixels = new Color32[tgaFile.Header.Width * tgaFile.Header.Height];

            return isRLE
                ? ReadTGARLEPixels(binaryReader, ref tgaFile, 8, null, true)
                : ReadTGARawPixels(binaryReader, ref tgaFile, 8, null, true);
        }

        private static bool ReadTGARawPixels(BinaryReader binaryReader, ref TGAFile tgaFile,
            int depth, Color32[] palette, bool isGrayscale)
        {
            int width = tgaFile.Header.Width;
            int height = tgaFile.Header.Height;
            bool isTopToBottom = tgaFile.Header.IsTopToBottom;
            int alphaBits = tgaFile.Header.AlphaBits;

            for (int y = 0; y < height; y++)
            {
                int pixelY = isTopToBottom ? (height - 1 - y) : y;
                for (int x = 0; x < width; x++)
                {
                    Color32 color;
                    if (isGrayscale)
                        color = ReadTGAGrayscale(binaryReader);
                    else if (palette != null)
                        color = ReadTGAPaletteColor(binaryReader, depth, palette, tgaFile.Header.ColorMapStart);
                    else
                        color = ReadTGAColor(binaryReader, depth, alphaBits);

                    tgaFile.Pixels[pixelY * width + x] = color;
                }
            }

            return true;
        }

        private static bool ReadTGARLEPixels(BinaryReader binaryReader, ref TGAFile tgaFile,
            int depth, Color32[] palette, bool isGrayscale)
        {
            int width = tgaFile.Header.Width;
            int height = tgaFile.Header.Height;
            bool isTopToBottom = tgaFile.Header.IsTopToBottom;
            int alphaBits = tgaFile.Header.AlphaBits;
            int totalPixels = width * height;

            // 파일 읽기 순서대로 선형 배열에 채운 뒤 방향 적용
            var linear = new Color32[totalPixels];
            int pixelIndex = 0;

            while (pixelIndex < totalPixels)
            {
                byte packetHeader = binaryReader.ReadByte();
                int count = (packetHeader & 0x7F) + 1;

                if ((packetHeader & 0x80) != 0)
                {
                    Color32 color;
                    if (isGrayscale)
                        color = ReadTGAGrayscale(binaryReader);
                    else if (palette != null)
                        color = ReadTGAPaletteColor(binaryReader, depth, palette, tgaFile.Header.ColorMapStart);
                    else
                        color = ReadTGAColor(binaryReader, depth, alphaBits);

                    for (int i = 0; i < count; i++)
                        linear[pixelIndex++] = color;
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        Color32 color;
                        if (isGrayscale)
                            color = ReadTGAGrayscale(binaryReader);
                        else if (palette != null)
                            color = ReadTGAPaletteColor(binaryReader, depth, palette, tgaFile.Header.ColorMapStart);
                        else
                            color = ReadTGAColor(binaryReader, depth, alphaBits);

                        linear[pixelIndex++] = color;
                    }
                }
            }

            for (int y = 0; y < height; y++)
            {
                int dstY = isTopToBottom ? (height - 1 - y) : y;
                for (int x = 0; x < width; x++)
                    tgaFile.Pixels[dstY * width + x] = linear[y * width + x];
            }

            return true;
        }

        private static Color32 ReadTGAColor(BinaryReader binaryReader, int depth, int alphaBits)
        {
            switch (depth)
            {
                case 32:
                {
                    byte b = binaryReader.ReadByte();
                    byte g = binaryReader.ReadByte();
                    byte r = binaryReader.ReadByte();
                    byte a = binaryReader.ReadByte();
                    return new Color32(r, g, b, a);
                }
                case 24:
                {
                    byte b = binaryReader.ReadByte();
                    byte g = binaryReader.ReadByte();
                    byte r = binaryReader.ReadByte();
                    return new Color32(r, g, b, 255);
                }
                case 16:
                {
                    // 1-5-5-5: [A]RRRRRGGGGGBBBBB
                    ushort value = binaryReader.ReadUInt16();
                    byte r = (byte)(((value >> 10) & 0x1F) * 255 / 31);
                    byte g = (byte)(((value >> 5) & 0x1F) * 255 / 31);
                    byte b = (byte)((value & 0x1F) * 255 / 31);
                    byte a = alphaBits > 0 ? ((value & 0x8000) != 0 ? (byte)255 : (byte)0) : (byte)255;
                    return new Color32(r, g, b, a);
                }
                case 15:
                {
                    ushort value = binaryReader.ReadUInt16();
                    byte r = (byte)(((value >> 10) & 0x1F) * 255 / 31);
                    byte g = (byte)(((value >> 5) & 0x1F) * 255 / 31);
                    byte b = (byte)((value & 0x1F) * 255 / 31);
                    return new Color32(r, g, b, 255);
                }
                default:
                    Debugger.LogError($"Unsupported TGA pixel depth: {depth}", Instance, nameof(ImageLoader));
                    return new Color32(0, 0, 0, 255);
            }
        }

        private static Color32 ReadTGAGrayscale(BinaryReader binaryReader)
        {
            byte v = binaryReader.ReadByte();
            return new Color32(v, v, v, 255);
        }

        private static Color32 ReadTGAPaletteColor(BinaryReader binaryReader, int indexDepth,
            Color32[] palette, int colorMapStart)
        {
            int index = indexDepth == 16 ? binaryReader.ReadUInt16() : binaryReader.ReadByte();
            index -= colorMapStart;

            if (index < 0 || index >= palette.Length)
            {
                Debugger.LogError($"Palette index out of range: {index}", Instance, nameof(ImageLoader));
                return new Color32(0, 0, 0, 255);
            }

            return palette[index];
        }
    }
}
