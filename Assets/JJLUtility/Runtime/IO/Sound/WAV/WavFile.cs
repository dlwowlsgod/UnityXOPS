using UnityEngine;
using System.IO;
using System.Text;

namespace JJLUtility.IO
{
    /// <summary>
    /// WAV 파일 로딩 기능을 담당하는 부분 클래스.
    /// </summary>
    public partial class SoundLoader
    {
        /// <summary>
        /// WAV 바이너리 파일을 파싱해 AudioClip으로 변환해 반환한다.
        /// </summary>
        /// <param name="filepath">WAV 파일 경로.</param>
        /// <returns>파싱된 AudioClip. 실패 시 null.</returns>
        private static AudioClip LoadWavFile(string filepath)
        {
            using var reader = new BinaryReader(File.OpenRead(filepath));

            // RIFF 헤더 검증
            string riff = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (riff != "RIFF")
            {
                Debugger.LogError($"Not a valid RIFF file: {filepath}", Instance, nameof(SoundLoader));
                return null;
            }
            reader.ReadInt32(); // 파일 크기
            string wave = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (wave != "WAVE")
            {
                Debugger.LogError($"Not a valid WAVE file: {filepath}", Instance, nameof(SoundLoader));
                return null;
            }

            int    channels      = 0;
            int    sampleRate    = 0;
            ushort bitsPerSample = 0;
            byte[] pcmData       = null;

            // chunk 순회 (fmt / data 외 chunk는 건너뜀)
            while (reader.BaseStream.Position < reader.BaseStream.Length - 8)
            {
                string chunkId   = Encoding.ASCII.GetString(reader.ReadBytes(4));
                int    chunkSize = reader.ReadInt32();

                if (chunkId == "fmt ")
                {
                    ushort audioFormat = reader.ReadUInt16();
                    if (audioFormat != 1)
                    {
                        Debugger.LogError($"Only PCM WAV is supported (audioFormat={audioFormat}): {filepath}", Instance, nameof(SoundLoader));
                        return null;
                    }
                    channels      = reader.ReadUInt16();
                    sampleRate    = reader.ReadInt32();
                    reader.ReadInt32();  // byteRate
                    reader.ReadUInt16(); // blockAlign
                    bitsPerSample = reader.ReadUInt16();

                    // fmt chunk 크기가 16보다 크면 나머지 스킵
                    int remaining = chunkSize - 16;
                    if (remaining > 0)
                        reader.ReadBytes(remaining);
                }
                else if (chunkId == "data")
                {
                    pcmData = reader.ReadBytes(chunkSize);
                    break;
                }
                else
                {
                    reader.ReadBytes(chunkSize);
                }

                // RIFF 사양: 홀수 크기 chunk 뒤에는 패딩 바이트 1개
                if (chunkSize % 2 != 0 && reader.BaseStream.Position < reader.BaseStream.Length)
                    reader.ReadByte();
            }

            if (pcmData == null || channels == 0 || sampleRate == 0)
            {
                Debugger.LogError($"WAV data chunk not found or fmt missing: {filepath}", Instance, nameof(SoundLoader));
                return null;
            }

            if (bitsPerSample != 8 && bitsPerSample != 16)
            {
                Debugger.LogError($"Only 8/16-bit PCM WAV is supported (bits={bitsPerSample}): {filepath}", Instance, nameof(SoundLoader));
                return null;
            }

            // PCM bytes → float 샘플 변환
            int     bytesPerSample = bitsPerSample / 8;
            int     sampleCount    = pcmData.Length / bytesPerSample / channels;
            float[] samples        = new float[sampleCount * channels];

            if (bitsPerSample == 8)
            {
                for (int i = 0; i < samples.Length; i++)
                    samples[i] = (pcmData[i] - 128) / 128f;
            }
            else
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    int offset = i * 2;
                    samples[i] = (short)(pcmData[offset] | (pcmData[offset + 1] << 8)) / 32768f;
                }
            }

            string    clipName = Path.GetFileNameWithoutExtension(filepath);
            AudioClip clip     = AudioClip.Create(clipName, sampleCount, channels, sampleRate, stream: false);
            clip.SetData(samples, offsetSamples: 0);
            return clip;
        }
    }
}
