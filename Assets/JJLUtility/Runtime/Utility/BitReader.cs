using System.Text;

namespace System.IO
{
    /// <summary>
    /// Provides functionality to read individual bits or a specified number of bits from a stream.
    /// Inherits from <see cref="BinaryReader"/>.
    /// </summary>
    public class BitReader : BinaryReader
    {
        private byte m_currentData;
        private int m_currentBit;
        
        public BitReader(Stream input) : this(input, Encoding.Default)
        {
        }

        public BitReader(Stream input, Encoding encoding) : this(input, encoding, false)
        {
            
        }

        public BitReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
            m_currentData = 0;
            m_currentBit = 0;
        }

        /// <summary>
        /// Reads a single bit from the current position in the stream.
        /// </summary>
        /// <returns>
        /// A single bit represented as a byte. The value will be either 0 or 1.
        /// </returns>
        public byte ReadBit()
        {
            if (m_currentBit <= 0)
            {
                m_currentData = ReadByte();
                m_currentBit = 8;
            }
            return (byte)((m_currentData >> --m_currentBit) & 1);
        }

        /// <summary>
        /// Reads a specified number of bits from the current position in the stream.
        /// </summary>
        /// <param name="count">
        /// The number of bits to read. Count limit is 1~32.
        /// </param>
        /// <returns>
        /// An unsigned 64-bit integer representing the value of the bits read.
        /// </returns>
        public ulong ReadBits(int count)
        {
            count = count <= 0 ? 1 : count;
            count = count > 32 ? 32 : count;

            ulong value = 0UL;
            for (int i = count - 1; i >= 0; i--)
            {
                value |= (ulong)ReadBit() << i;
            }

            return value;
        }

        /// <summary>
        /// Resets the internal bit buffer, clearing all currently buffered bits
        /// and preparing the reader to align with the next byte boundary in the stream.
        /// </summary>
        public void ResetBitBuffer()
        {
            m_currentData = 0;
            m_currentBit = 0;
        }
    }
}