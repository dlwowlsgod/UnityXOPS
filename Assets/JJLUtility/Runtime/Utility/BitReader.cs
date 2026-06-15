using System.Text;

namespace System.IO
{
    /// <summary>
    /// 스트림에서 개별 비트 또는 지정된 개수의 비트를 읽는 기능을 제공한다.
    /// <see cref="BinaryReader"/>를 상속한다.
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
        /// 스트림의 현재 위치에서 1비트를 읽는다.
        /// </summary>
        /// <returns>읽은 1비트. byte로 표현되며 값은 0 또는 1이다.</returns>
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
        /// 스트림의 현재 위치에서 지정된 개수의 비트를 읽는다.
        /// </summary>
        /// <param name="count">읽을 비트 수. 범위는 1~32로 제한된다.</param>
        /// <returns>읽은 비트 값을 나타내는 부호 없는 64비트 정수.</returns>
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
        /// 내부 비트 버퍼를 초기화한다. 현재 버퍼링된 비트를 모두 비우고
        /// 다음 바이트 경계에 맞춰 읽을 수 있도록 준비한다.
        /// </summary>
        public void ResetBitBuffer()
        {
            m_currentData = 0;
            m_currentBit = 0;
        }
    }
}