using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    public class MaingameUIDynamicLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform state, hp, ammo, weaponName, reload, change, simpleWeaponName;
        [SerializeField] private GameObject normalUI, simpleUI;
        [SerializeField] private RawImage simpleHPLeft, simpleHPRight, simpleHPUp, simpleHPDown;

        private Human m_player;
        private XOPSSpriteText m_stateText, m_hpText, m_ammoText, m_weaponNameText, m_simpleWeaponNameText;

        private float  m_lastHP          = float.NaN;
        private int    m_lastMagazine    = -1;
        private int    m_lastReserveAmmo = -1;
        private string m_lastWeaponName;

        private void Start()
        {
            m_player = MapLoader.Player;
            Vector2 stateFontSize = new Vector2(18f, 24f);
            Vector2 oneZero = new Vector2(1f, 0f);
            Vector2 center = new Vector2(0.5f, 0.5f);

            Color32 stateColor = SetStateColor(m_player.HP);
            m_stateText = FontManager.CreateSpriteText<XOPSSpriteText>(
                state, "STATE", Vector2.zero, Vector2.zero, new Vector2(23, 45), new Vector2(23 * 5, 24), stateFontSize, stateColor, TextAnchor.UpperLeft, 0f);

            var hpPos = SetHPPosition(m_player.HP);
            var hpSize = new Vector2(hpPos.x * 5, 24f);
            m_hpText = FontManager.CreateSpriteText<XOPSSpriteText>(
                hp, SetHPText(m_player.HP), Vector2.zero, Vector2.zero, hpPos, hpSize, stateFontSize, stateColor, TextAnchor.UpperLeft, 0f);

            Vector2 ammoFontSize = new Vector2(23f, 24f);
            string currentAmmo = m_player.CurrentWeapon.CurrentMagazine.ToString();
            string reserveAmmo = m_player.CurrentWeapon.ReserveAmmo > 999 ? "999+" : m_player.CurrentWeapon.ReserveAmmo.ToString();
            string finalAmmo = $"\u00BB{currentAmmo} \u00BA{reserveAmmo}";
            m_ammoText = FontManager.CreateSpriteText<XOPSSpriteText>(
                ammo, finalAmmo, Vector2.zero, Vector2.zero, new Vector2(25, 96), new Vector2(25 * finalAmmo.Length, 24), ammoFontSize, Color.white, TextAnchor.UpperLeft, 0f);

            string weaponNameString = m_player.CurrentWeapon.WeaponData.name;
            Vector2 weaponNameFontSize = SetWeaponNameFontSize(weaponNameString, 14, new Vector2(16f, 20f));
            m_weaponNameText = FontManager.CreateSpriteText<XOPSSpriteText>(
                weaponName, weaponNameString, oneZero, oneZero, new Vector2(-250, 85), new Vector2(25 * weaponNameString.Length, 98), weaponNameFontSize, Color.white, TextAnchor.MiddleLeft, 0f);

            string simpleWeaponNameString = m_player.CurrentWeapon.WeaponData.name;
            Vector2 simpleWeaponNameFontSize = SetWeaponNameFontSize(simpleWeaponNameString, 10, new Vector2(16f, 20f));
            m_simpleWeaponNameText = FontManager.CreateSpriteText<XOPSSpriteText>(
                simpleWeaponName, simpleWeaponNameString, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(25 * simpleWeaponNameString.Length, 98), simpleWeaponNameFontSize, Color.white, TextAnchor.MiddleLeft, 0f);

            Vector2 reloadChargeFontSize = new Vector2(32f, 34f);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                reload, "RELOADING", center, center, new Vector2(3, -63), new Vector2(0, 0), reloadChargeFontSize, new Color(0.2f, 0.2f, 0.2f, 1.0f), TextAnchor.MiddleCenter, 0f);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                reload, "RELOADING", center, center, new Vector2(0, -60), new Vector2(0, 0), reloadChargeFontSize, Color.white, TextAnchor.MiddleCenter, 0f);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                change, "CHANGING", center, center, new Vector2(3, -63), new Vector2(0, 0), reloadChargeFontSize, new Color(0.2f, 0.2f, 0.2f, 1.0f), TextAnchor.MiddleCenter, 0f);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                change, "CHANGING", center, center, new Vector2(0, -60), new Vector2(0, 0), reloadChargeFontSize, Color.white, TextAnchor.MiddleCenter, 0f);

            m_lastHP          = m_player.HP;
            m_lastMagazine    = m_player.CurrentWeapon.CurrentMagazine;
            m_lastReserveAmmo = m_player.CurrentWeapon.ReserveAmmo;
            m_lastWeaponName  = m_player.CurrentWeapon.WeaponData.name;

            simpleUI.SetActive(false);
        }

        /// <summary>
        /// Normal UI ↔ Simple UI 토글. 토글 직후 활성 UI가 최신값을 즉시 반영하도록 캐시를 무효화한다.
        /// </summary>
        public void ToggleUIMode()
        {
            bool toSimple = normalUI.activeSelf;
            normalUI.SetActive(!toSimple);
            simpleUI.SetActive(toSimple);

            m_lastHP          = float.NaN;
            m_lastMagazine    = -1;
            m_lastReserveAmmo = -1;
            m_lastWeaponName  = null;
        }

        private void Update()
        {
            // 스폰/리스폰(F12 인-씬 재시작)으로 Player 가 교체·소멸될 수 있어 매 프레임 재취득.
            // 안 하면 언로드로 파괴된 옛 Player 접근 → MissingReferenceException 으로 Update 가 매 프레임 중단돼 UI 전체 먹통.
            Human player = MapLoader.Player;
            if (player == null) return;
            if (player != m_player)
            {
                m_player = player;
                // 교체 시 캐시 무효화 → 새 Player 의 HP/탄/무기명을 즉시 다시 칠함 (텍스트 오브젝트는 Start 생성분 그대로 사용).
                m_lastHP          = float.NaN;
                m_lastMagazine    = -1;
                m_lastReserveAmmo = -1;
                m_lastWeaponName  = null;
            }

            if (normalUI.activeSelf)
            {
                float hp = m_player.HP;
                if (hp != m_lastHP)
                {
                    Color32 stateColor    = SetStateColor(hp);
                    m_stateText.FontColor = stateColor;
                    m_hpText.FontColor    = stateColor;
                    m_hpText.Text         = SetHPText(hp);

                    Vector2 hpPos = SetHPPosition(hp);
                    m_hpText.rectTransform.anchoredPosition = hpPos;
                    m_hpText.rectTransform.sizeDelta        = new Vector2(hpPos.x * 5, hpPos.y);
                    m_lastHP = hp;
                }

                Weapon w   = m_player.CurrentWeapon;
                int    mag = w.CurrentMagazine;
                int    res = w.ReserveAmmo;
                if (mag != m_lastMagazine || res != m_lastReserveAmmo)
                {
                    string reserveStr = res > 999 ? "999+" : res.ToString();
                    m_ammoText.Text   = $"\u00BB{mag} \u00BA{reserveStr}";
                    m_lastMagazine    = mag;
                    m_lastReserveAmmo = res;
                }

                string name = w.WeaponData.name;
                if (name != m_lastWeaponName)
                {
                    m_weaponNameText.Text                    = name;
                    m_weaponNameText.rectTransform.sizeDelta = new Vector2(25 * name.Length, 98);
                    m_lastWeaponName = name;
                }
            }

            if (simpleUI.activeSelf)
            {
                float hp = m_player.HP;
                if (hp != m_lastHP)
                {
                    Color32 stateColor = SetStateColor(hp);
                    simpleHPLeft.color  = stateColor;
                    simpleHPRight.color = stateColor;
                    simpleHPUp.color    = stateColor;
                    simpleHPDown.color  = stateColor;

                    m_lastHP = hp;
                }

                Weapon w = m_player.CurrentWeapon;
                string name = w.WeaponData.name;
                if (name != m_lastWeaponName)
                {
                    m_simpleWeaponNameText.Text                    = name;
                    m_simpleWeaponNameText.rectTransform.sizeDelta = new Vector2(25 * name.Length, 98);
                    m_lastWeaponName = name;
                }
            }

            reload.gameObject.SetActive(m_player.IsReloading);
            change.gameObject.SetActive(m_player.IsSwitchingWeapon);
        }

        private Color32 SetStateColor(float hp)
        {
            return hp switch
            {
                >= 100f => (Color32)new Color(0.0f, 1.0f, 0.0f, 1.0f),
                >= 50f => (Color32)new Color(1.0f / 50 * (100 - hp), 1.0f, 0.0f, 1.0f),
                > 0f => (Color32)new Color(1.0f, 1.0f / 50 * hp, 0.0f, 1.0f),
                _ => (Color32)new Color(1.0f, 0.0f, 0.0f, 1.0f)
            };
        }

        private string SetHPText(float hp)
        {
            return hp switch
            {
                >= 80 => "FINE",
                >= 40 => "CAUTION",
                > 0 => "DANGER",
                _ => "DEAD"
            };
        }

        private Vector2 SetHPPosition(float hp)
        {
            return hp switch
            {
                >= 80 => new Vector2(155, 45),
                >= 40 => new Vector2(135, 45),
                > 0 => new Vector2(140, 45),
                _ => new Vector2(155, 45)
            };
        }

        private Vector2 SetWeaponNameFontSize(string name, int limit, Vector2 baseFontSize)
        {
            if (name.Length <= limit)
            {
                return baseFontSize;
            }

            float ratio = name.Length / (float)limit;
            return new Vector2(baseFontSize.x / ratio, baseFontSize.y);
        }
    }
}
