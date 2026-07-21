using System.Collections.Generic;
using JJLUtility;
using TMPro;
using XLua;

namespace UnityXOPS.Modding
{
    /// <summary>
    /// Lua가 생성한 TMP 폰트 에셋을 안전한 메서드로만 제어하는 핸들.
    /// 내부 TMP_FontAsset은 직접 노출하지 않으며, 아틀라스는 항상 동적(Dynamic)으로 고정된다.
    /// </summary>
    [LuaCallCSharp]
    public class TMPFontHandle
    {
        private TMP_FontAsset m_font;

        /// <summary>
        /// 핸들을 생성한다.
        /// </summary>
        /// <param name="font">대상 TMP_FontAsset. 생성에 실패한 경우 null이 들어오며 이때 IsValid()가 false가 된다.</param>
        public TMPFontHandle(TMP_FontAsset font)
        {
            m_font = font;
        }

        internal TMP_FontAsset Asset => m_font;

        /// <summary>
        /// 지정 굵기의 기울임(Italic) 대체 폰트를 등록한다. 텍스트에 italic 스타일이 걸리면 이 폰트로 그려진다.
        /// </summary>
        /// <param name="weight">굵기(100~900, 100 단위. 보통 400=보통, 700=굵게)</param>
        /// <param name="font">그 굵기에 쓸 기울임 폰트 핸들</param>
        public void SetItalic(int weight, TMPFontHandle font)
        {
            AssignWeight(weight, font, true);
        }

        /// <summary>
        /// 지정 굵기의 일반(Regular) 대체 폰트를 등록한다. 텍스트 굵기가 그 값일 때 이 폰트로 그려진다.
        /// </summary>
        /// <param name="weight">굵기(100~900, 100 단위. 보통 400=보통, 700=굵게)</param>
        /// <param name="font">그 굵기에 쓸 폰트 핸들</param>
        public void SetRegular(int weight, TMPFontHandle font)
        {
            AssignWeight(weight, font, false);
        }

        /// <summary>
        /// 이 폰트에 없는 글자를 대신 그릴 대체(Fallback) 폰트를 체인 끝에 추가한다. 이미 있으면 무시한다.
        /// </summary>
        /// <param name="font">대체 폰트 핸들</param>
        public void AddFallback(TMPFontHandle font)
        {
            if (m_font == null || font == null || font.Asset == null || font.Asset == m_font)
            {
                return;
            }

            m_font.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
            if (!m_font.fallbackFontAssetTable.Contains(font.Asset))
            {
                m_font.fallbackFontAssetTable.Add(font.Asset);
            }
        }

        /// <summary>
        /// 폰트 이름을 반환한다(로그 확인용).
        /// </summary>
        /// <returns>폰트 이름. 폰트가 없으면 빈 문자열.</returns>
        public string GetName()
        {
            return m_font != null ? m_font.name : string.Empty;
        }

        /// <summary>
        /// 폰트가 정상적으로 만들어졌는지 반환한다. 경로나 폰트 이름이 틀리면 false다.
        /// </summary>
        /// <returns>사용 가능하면 true</returns>
        public bool IsValid()
        {
            return m_font != null;
        }

        /// <summary>
        /// 굵기 테이블의 지정 칸에 대체 폰트를 써 넣는다.
        /// </summary>
        /// <param name="weight">굵기(100~900)</param>
        /// <param name="font">등록할 폰트 핸들</param>
        /// <param name="italic">true면 기울임 칸, false면 일반 칸</param>
        private void AssignWeight(int weight, TMPFontHandle font, bool italic)
        {
            if (m_font == null || font == null || font.Asset == null)
            {
                return;
            }

            int index = weight / 100;
            if (weight % 100 != 0 || index < 1 || index > 9)
            {
                Debugger.LogWarning($"[Font] 지원하지 않는 굵기입니다: {weight} (100~900, 100 단위)");
                return;
            }

            TMP_FontWeightPair[] table = m_font.fontWeightTable;
            if (table == null || table.Length <= index)
            {
                return;
            }

            if (italic)
            {
                table[index].italicTypeface = font.Asset;
            }
            else
            {
                table[index].regularTypeface = font.Asset;
            }
        }
    }
}
