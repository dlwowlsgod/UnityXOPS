using System;
using System.IO;
using UnityEngine;

//https://github.com/keijiro/DdsFileLoader
//Resharper disable all
namespace UnityDds
{
    public enum DXGIFormat : uint
	{
		DXGI_FORMAT_UNKNOWN = 0,
		DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
		DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
		DXGI_FORMAT_R32G32B32A32_UINT = 3,
		DXGI_FORMAT_R32G32B32A32_SINT = 4,
		DXGI_FORMAT_R32G32B32_TYPELESS = 5,
		DXGI_FORMAT_R32G32B32_FLOAT = 6,
		DXGI_FORMAT_R32G32B32_UINT = 7,
		DXGI_FORMAT_R32G32B32_SINT = 8,
		DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
		DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
		DXGI_FORMAT_R16G16B16A16_UNORM = 11,
		DXGI_FORMAT_R16G16B16A16_UINT = 12,
		DXGI_FORMAT_R16G16B16A16_SNORM = 13,
		DXGI_FORMAT_R16G16B16A16_SINT = 14,
		DXGI_FORMAT_R32G32_TYPELESS = 15,
		DXGI_FORMAT_R32G32_FLOAT = 16,
		DXGI_FORMAT_R32G32_UINT = 17,
		DXGI_FORMAT_R32G32_SINT = 18,
		DXGI_FORMAT_R32G8X24_TYPELESS = 19,
		DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
		DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
		DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
		DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
		DXGI_FORMAT_R10G10B10A2_UNORM = 24,
		DXGI_FORMAT_R10G10B10A2_UINT = 25,
		DXGI_FORMAT_R11G11B10_FLOAT = 26,
		DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
		DXGI_FORMAT_R8G8B8A8_UNORM = 28,
		DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
		DXGI_FORMAT_R8G8B8A8_UINT = 30,
		DXGI_FORMAT_R8G8B8A8_SNORM = 31,
		DXGI_FORMAT_R8G8B8A8_SINT = 32,
		DXGI_FORMAT_R16G16_TYPELESS = 33,
		DXGI_FORMAT_R16G16_FLOAT = 34,
		DXGI_FORMAT_R16G16_UNORM = 35,
		DXGI_FORMAT_R16G16_UINT = 36,
		DXGI_FORMAT_R16G16_SNORM = 37,
		DXGI_FORMAT_R16G16_SINT = 38,
		DXGI_FORMAT_R32_TYPELESS = 39,
		DXGI_FORMAT_D32_FLOAT = 40,
		DXGI_FORMAT_R32_FLOAT = 41,
		DXGI_FORMAT_R32_UINT = 42,
		DXGI_FORMAT_R32_SINT = 43,
		DXGI_FORMAT_R24G8_TYPELESS = 44,
		DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
		DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
		DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
		DXGI_FORMAT_R8G8_TYPELESS = 48,
		DXGI_FORMAT_R8G8_UNORM = 49,
		DXGI_FORMAT_R8G8_UINT = 50,
		DXGI_FORMAT_R8G8_SNORM = 51,
		DXGI_FORMAT_R8G8_SINT = 52,
		DXGI_FORMAT_R16_TYPELESS = 53,
		DXGI_FORMAT_R16_FLOAT = 54,
		DXGI_FORMAT_D16_UNORM = 55,
		DXGI_FORMAT_R16_UNORM = 56,
		DXGI_FORMAT_R16_UINT = 57,
		DXGI_FORMAT_R16_SNORM = 58,
		DXGI_FORMAT_R16_SINT = 59,
		DXGI_FORMAT_R8_TYPELESS = 60,
		DXGI_FORMAT_R8_UNORM = 61,
		DXGI_FORMAT_R8_UINT = 62,
		DXGI_FORMAT_R8_SNORM = 63,
		DXGI_FORMAT_R8_SINT = 64,
		DXGI_FORMAT_A8_UNORM = 65,
		DXGI_FORMAT_R1_UNORM = 66,
		DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
		DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
		DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
		DXGI_FORMAT_BC1_TYPELESS = 70,
		DXGI_FORMAT_BC1_UNORM = 71,
		DXGI_FORMAT_BC1_UNORM_SRGB = 72,
		DXGI_FORMAT_BC2_TYPELESS = 73,
		DXGI_FORMAT_BC2_UNORM = 74,
		DXGI_FORMAT_BC2_UNORM_SRGB = 75,
		DXGI_FORMAT_BC3_TYPELESS = 76,
		DXGI_FORMAT_BC3_UNORM = 77,
		DXGI_FORMAT_BC3_UNORM_SRGB = 78,
		DXGI_FORMAT_BC4_TYPELESS = 79,
		DXGI_FORMAT_BC4_UNORM = 80,
		DXGI_FORMAT_BC4_SNORM = 81,
		DXGI_FORMAT_BC5_TYPELESS = 82,
		DXGI_FORMAT_BC5_UNORM = 83,
		DXGI_FORMAT_BC5_SNORM = 84,
		DXGI_FORMAT_B5G6R5_UNORM = 85,
		DXGI_FORMAT_B5G5R5A1_UNORM = 86,
		DXGI_FORMAT_B8G8R8A8_UNORM = 87,
		DXGI_FORMAT_B8G8R8X8_UNORM = 88,
		DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
		DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
		DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
		DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
		DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
		DXGI_FORMAT_BC6H_TYPELESS = 94,
		DXGI_FORMAT_BC6H_UF16 = 95,
		DXGI_FORMAT_BC6H_SF16 = 96,
		DXGI_FORMAT_BC7_TYPELESS = 97,
		DXGI_FORMAT_BC7_UNORM = 98,
		DXGI_FORMAT_BC7_UNORM_SRGB = 99,
		DXGI_FORMAT_AYUV = 100,
		DXGI_FORMAT_Y410 = 101,
		DXGI_FORMAT_Y416 = 102,
		DXGI_FORMAT_NV12 = 103,
		DXGI_FORMAT_P010 = 104,
		DXGI_FORMAT_P016 = 105,
		DXGI_FORMAT_420_OPAQUE = 106,
		DXGI_FORMAT_YUY2 = 107,
		DXGI_FORMAT_Y210 = 108,
		DXGI_FORMAT_Y216 = 109,
		DXGI_FORMAT_NV11 = 110,
		DXGI_FORMAT_AI44 = 111,
		DXGI_FORMAT_IA44 = 112,
		DXGI_FORMAT_P8 = 113,
		DXGI_FORMAT_A8P8 = 114,
		DXGI_FORMAT_B4G4R4A4_UNORM = 115,
		DXGI_FORMAT_P208 = 130,
		DXGI_FORMAT_V208 = 131,
		DXGI_FORMAT_V408 = 132,
		DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE,
		DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE,
		DXGI_FORMAT_FORCE_UINT = 0xffffffff
	}
    
	public class DdsDX10Extension
	{
		public DXGIFormat dxgiFormat;
		public uint resourceDimension;
		public uint miscFlag; // See D3D11_RESOURCE_MISC_FLAG
		public uint arraySize;
		public uint reserved;

		public static DdsDX10Extension Deserialize(BinaryReader reader)
		{
			var header = new DdsDX10Extension();

			header.dxgiFormat = (DXGIFormat)reader.ReadUInt32();
			header.resourceDimension = reader.ReadUInt32();
			header.miscFlag = reader.ReadUInt32();
			header.arraySize = reader.ReadUInt32();
			header.reserved = reader.ReadUInt32();

			return header;
		}
	}
	
	public class DdsFile
	{
		private const string magicNumber = "DDS ";
		private const string dx10Identifier = "DX10";

		public string dwMagic;
		public DdsHeader header;
		public DdsDX10Extension dx10Extension;
		public byte[] data;

		public static DdsFile Deserialize(BinaryReader reader)
		{
			var file = new DdsFile();

			file.dwMagic = reader.ReadString(4);
			if (file.dwMagic != magicNumber)
				throw new IOException($"Expected header file identifier ({magicNumber}) does not match the deserialized identifier ({file.dwMagic})");

			file.header = DdsHeader.Deserialize(reader);
			if (file.header.ddspf.fourCC == dx10Identifier)
				file.dx10Extension = DdsDX10Extension.Deserialize(reader);

			file.data = reader.ReadRemainingBytes();

			return file;
		}
	}
	
	public class DdsHeader
	{
		public uint size;
		public uint flags;
		public uint height;
		public uint width;
		public uint pitchOrLinearSize;
		public uint depth;
		public uint mipMapCount;
		public uint[] reserved1; // 11 ints
		public DdsPixelFormat ddspf;
		public uint caps;
		public uint caps2;
		public uint caps3;
		public uint caps4;
		public uint reserved2;

		public static DdsHeader Deserialize(BinaryReader reader)
		{
			var header = new DdsHeader();

			header.size = reader.ReadUInt32();
			header.flags = reader.ReadUInt32();
			header.height = reader.ReadUInt32();
			header.width = reader.ReadUInt32();
			header.pitchOrLinearSize = reader.ReadUInt32();
			header.depth = reader.ReadUInt32();
			header.mipMapCount = reader.ReadUInt32();
			header.reserved1 = reader.ReadArray((x) => x.ReadUInt32(), 11);
			header.ddspf = DdsPixelFormat.Deserialize(reader);
			header.caps = reader.ReadUInt32();
			header.caps2 = reader.ReadUInt32();
			header.caps3 = reader.ReadUInt32();
			header.caps4 = reader.ReadUInt32();
			header.reserved2 = reader.ReadUInt32();

			return header;
		}
	}
	
	public class DdsPixelFormat
	{
		public uint size;
		public uint flags;
		public string fourCC;
		public uint RGBBitCount;
		public uint RBitMask;
		public uint GBitMask;
		public uint BBitMask;
		public uint ABitMask;

		public static DdsPixelFormat Deserialize(BinaryReader reader)
		{
			var format = new DdsPixelFormat();

			format.size = reader.ReadUInt32();
			format.flags = reader.ReadUInt32();
			format.fourCC = reader.ReadString(4);
			format.RGBBitCount = reader.ReadUInt32();
			format.RBitMask = reader.ReadUInt32();
			format.GBitMask = reader.ReadUInt32();
			format.BBitMask = reader.ReadUInt32();
			format.ABitMask = reader.ReadUInt32();

			return format;
		}
	}
	
	public static class DdsTextureLoader
	{
		public static Texture2D LoadTexture(string path, bool isLinear = false)
		{
			using (var stream = File.Open(path, FileMode.Open))
				return LoadTexture(stream, isLinear);
		}

		public static Texture2D LoadTexture(Stream stream, bool isLinear = false)
		{
			var file = GetDdsFile(stream);
			var format = DdsUtil.GetTextureFormat(file);
			var hasMipMaps = file.header.mipMapCount > 1;
			
			Flip(file, format);

			var texture = new Texture2D((int)file.header.width, (int)file.header.height, format, hasMipMaps, isLinear);
			texture.LoadRawTextureData(file.data);
			texture.Apply(false, true);

			return texture;
		}

		private static void Flip(DdsFile file, TextureFormat format)
		{
			if (format != TextureFormat.DXT1 && format != TextureFormat.DXT5)
			{
				return;
			}
			
			int blockSize = format == TextureFormat.DXT1 ? 8 : 16;
            byte[] flippedData = new byte[file.data.Length];
            int dataOffset = 0;
            
            uint width = file.header.width;
            uint height = file.header.height;
            uint mipMapCount = file.header.mipMapCount > 0 ? file.header.mipMapCount : 1; 

            for (int i = 0; i < mipMapCount; i++)
            {
                int mipWidth = (int)Math.Max(1, width >> i);
                int mipHeight = (int)Math.Max(1, height >> i);

                int blocksWide = (mipWidth + 3) / 4;
                int blocksHigh = (mipHeight + 3) / 4;

                int mipPitch = blocksWide * blockSize;
                int mipSize = blocksHigh * mipPitch;
                
                if (dataOffset + mipSize > file.data.Length)
                {
                    return;
                }
                
                for (int y = 0; y < blocksHigh; y++)
                {
                    int sourceRowByteOffset = dataOffset + (y * mipPitch);
                    int destRowByteOffset = dataOffset + ((blocksHigh - 1 - y) * mipPitch);
                    
                    for (int x = 0; x < blocksWide; x++)
                    {
                        int sourceBlockByteOffset = sourceRowByteOffset + (x * blockSize);
                        int destBlockByteOffset = destRowByteOffset + (x * blockSize);

                        if (format == TextureFormat.DXT1)
                        {
                            FlipDxt1Block(file.data, sourceBlockByteOffset, flippedData, destBlockByteOffset);
                        }
                        else // DXT5
                        {
                            FlipDxt5Block(file.data, sourceBlockByteOffset, flippedData, destBlockByteOffset);
                        }
                    }
                }
                dataOffset += mipSize;
            }
            file.data = flippedData;
        }

		private static void FlipDxt1Block(byte[] source, int sourceOffset, byte[] dest, int destOffset)
		{
			// Copy color endpoints (bytes 0-3)
			for(int i = 0; i < 4; i++) dest[destOffset + i] = source[sourceOffset + i];

			// Flip lookup table (bytes 4-7)
			dest[destOffset + 4] = source[sourceOffset + 7]; // row 3 -> 0
			dest[destOffset + 5] = source[sourceOffset + 6]; // row 2 -> 1
			dest[destOffset + 6] = source[sourceOffset + 5]; // row 1 -> 2
			dest[destOffset + 7] = source[sourceOffset + 4]; // row 0 -> 3
		}

		private static void FlipDxt5Block(byte[] source, int sourceOffset, byte[] dest, int destOffset)
		{
			// Flip Alpha Block (bytes 0-7)
			dest[destOffset + 0] = source[sourceOffset + 0]; // alpha 0
			dest[destOffset + 1] = source[sourceOffset + 1]; // alpha 1

			ulong alphaBits = 0;
			for (int i = 0; i < 6; i++) alphaBits |= (ulong)source[sourceOffset + 2 + i] << (i * 8);
            
			ulong row0 = alphaBits & 0xFFF;
			ulong row1 = (alphaBits >> 12) & 0xFFF;
			ulong row2 = (alphaBits >> 24) & 0xFFF;
			ulong row3 = (alphaBits >> 36) & 0xFFF;
            
			ulong flippedAlphaBits = (row3 << 0) | (row2 << 12) | (row1 << 24) | (row0 << 36);

			for (int i = 0; i < 6; i++) dest[destOffset + 2 + i] = (byte)(flippedAlphaBits >> (i * 8));
            
			// Flip Color Block (bytes 8-15) - same as DXT1
			int colorSourceOffset = sourceOffset + 8;
			int colorDestOffset = destOffset + 8;
            
			for(int i = 0; i < 4; i++) dest[colorDestOffset + i] = source[colorSourceOffset + i];
            
			dest[colorDestOffset + 4] = source[colorSourceOffset + 7];
			dest[colorDestOffset + 5] = source[colorSourceOffset + 6];
			dest[colorDestOffset + 6] = source[colorSourceOffset + 5];
			dest[colorDestOffset + 7] = source[colorSourceOffset + 4];
		}


		private static DdsFile GetDdsFile(Stream stream)
		{
			using (var reader = new BinaryReader(stream))
			{
				return DdsFile.Deserialize(reader);
			}
		}
	}
	
	internal static class BinaryReaderExtensions
	{
		/// <summary>
		/// Reads an array, using the specified length to indicate the number of elements
		/// </summary>
		public static T[] ReadArray<T>(this BinaryReader reader, Func<BinaryReader, T> deserializeFunc, int length)
		{
			var array = new T[length];

			for (var i = 0; i < length; i++)
				array[i] = deserializeFunc(reader);

			return array;
		}

		/// <summary>
		/// Reads bytes until reaching the end of the file
		/// </summary>
		public static byte[] ReadRemainingBytes(this BinaryReader reader)
		{
			const int chunkSize = 4096;
			using (var ms = new MemoryStream())
			{
				var buf = new byte[chunkSize];
				int count;
				while ((count = reader.Read(buf, 0, buf.Length)) > 0)
					ms.Write(buf, 0, count);

				return ms.ToArray();
			}
		}

		/// <summary>
		/// Reads a string with the specified length
		/// </summary>
		public static string ReadString(this BinaryReader reader, int length)
		{
			var chars = reader.ReadChars(length);
			return new string(chars);
		}
	}
	
	public static class DdsUtil
	{
		public static TextureFormat GetTextureFormat(DdsFile file)
		{
			if (file.dx10Extension != null)
				return GetTextureFormatDX10(file.dx10Extension);
			else
				return GetTextureFormat(file.header);
		}

		private static TextureFormat GetTextureFormatDX10(DdsDX10Extension dx10Extension)
		{
			var format = dx10Extension.dxgiFormat;
			switch (format)
			{
				case DXGIFormat.DXGI_FORMAT_BC6H_UF16:
				case DXGIFormat.DXGI_FORMAT_BC6H_SF16:
					return TextureFormat.BC6H;
				case DXGIFormat.DXGI_FORMAT_BC7_UNORM:
					return TextureFormat.BC7;
				default:
					throw new Exception($"DDS file has an invalid or unsupported texture format ({format})");
			}
		}

		private static TextureFormat GetTextureFormat(DdsHeader header)
		{
			var format = header.ddspf.fourCC;
			switch (format)
			{
				case "DXT1":
					return TextureFormat.DXT1;
				case "DXT5":
					return TextureFormat.DXT5;
				case "BC4U":
					return TextureFormat.BC4;
				case "BC5U":
					return TextureFormat.BC5;
				default:
					throw new Exception($"DDS file has an invalid or unsupported texture format ({format})");
			}
		}
	}
}