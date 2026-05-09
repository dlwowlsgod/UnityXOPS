using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityXOPS
{
    public class MaingameUIDynamicLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform state, hp, ammo, weaponName;
        [SerializeField] private GameObject normalUI, simpleUI;

        private Human m_player;
        private XOPSSpriteText m_stateText, m_hpText, m_ammoText, m_weaponNameText;
        private RectTransform m_hpTextRectTransform, m_weaponNameTextRectTransform;

        private Color32 m_stateColor;
        private void Start()
        {
            m_player = MapLoader.Player;
            Vector2 stateFontSize = new Vector2(18f, 24f);
            Vector2 oneZero = new Vector2(1f, 0f);

            m_stateColor = SetStateColor(m_player.HP);
            m_stateText = FontManager.CreateSpriteText<XOPSSpriteText>(
                state, "STATE", Vector2.zero, Vector2.zero, new Vector2(23, 45), new Vector2(23 * 5, 45), stateFontSize, m_stateColor, TextAnchor.UpperLeft, 0f);
            
            var hpPos = SetHPPosition(m_player.HP);
            var hpSize = new Vector2(hpPos.x * 5, hpPos.y);
            m_hpText = FontManager.CreateSpriteText<XOPSSpriteText>(
                hp, SetHPText(m_player.HP), Vector2.zero, Vector2.zero, hpPos, hpSize, stateFontSize, m_stateColor, TextAnchor.UpperLeft, 0f);
            m_hpTextRectTransform = m_hpText.GetComponent<RectTransform>();

            Vector2 ammoFontSize = new Vector2(23f, 24f);
            string currentAmmo = m_player.CurrentWeapon.CurrentMagazine.ToString();
            string reserveAmmo = m_player.CurrentWeapon.ReserveAmmo > 999 ? "999+" : m_player.CurrentWeapon.ReserveAmmo.ToString();
            string finalAmmo = $"»{currentAmmo} º{reserveAmmo}";
            m_ammoText = FontManager.CreateSpriteText<XOPSSpriteText>(
                ammo, finalAmmo, Vector2.zero, Vector2.zero, new Vector2(25, 96), new Vector2(25 * finalAmmo.Length, 45), ammoFontSize, Color.white, TextAnchor.UpperLeft, 0f);

            string weaponNameString = m_player.CurrentWeapon.WeaponData.name;
            Vector2 weaponNameFontSize = SetWeaponNameFontSize(weaponNameString, 14, new Vector2(16f, 20f));
            float scaledHeightPosition = 95 - (20 - weaponNameFontSize.y) / 2; // Adjust Y position based on font size
            m_weaponNameText = FontManager.CreateSpriteText<XOPSSpriteText>(
                weaponName, weaponNameString, oneZero, oneZero, new Vector2(-250, scaledHeightPosition), new Vector2(25 * weaponNameString.Length, 98), weaponNameFontSize, Color.white, TextAnchor.UpperLeft, 0f);
            m_weaponNameTextRectTransform = m_weaponNameText.GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (normalUI.activeSelf)
            {
                m_stateColor = SetStateColor(m_player.HP);
                m_stateText.FontColor = m_stateColor;
                
                m_hpText.FontColor = m_stateColor;
                m_hpText.Text = SetHPText(m_player.HP);
                
                var hpPos = SetHPPosition(m_player.HP);
                var hpSize = new Vector2(hpPos.x * 5, hpPos.y);
                m_hpTextRectTransform.anchoredPosition = hpPos;
                m_hpTextRectTransform.sizeDelta = hpSize;

                string currentAmmo = m_player.CurrentWeapon.CurrentMagazine.ToString();
                string reserveAmmo = m_player.CurrentWeapon.ReserveAmmo > 999 ? "999+" : m_player.CurrentWeapon.ReserveAmmo.ToString();
                m_ammoText.Text = $"»{currentAmmo} º{reserveAmmo}";
                
                string weaponNameString = m_player.CurrentWeapon.WeaponData.name;
                Vector2 weaponNameFontSize = SetWeaponNameFontSize(weaponNameString, 14, new Vector2(16f, 20f));
                float scaledHeightPosition = 95 - (20 - weaponNameFontSize.y) / 2; // Adjust Y position based on font size
                m_weaponNameText.Text = weaponNameString;
                m_weaponNameTextRectTransform.anchoredPosition = new Vector2(-250, scaledHeightPosition);
                m_weaponNameTextRectTransform.sizeDelta = new Vector2(25 * weaponNameString.Length, 98);
            }
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
            return baseFontSize / ratio;
        }
    }
}
