using TMPro;
using UnityEngine;

namespace UnityXOPS
{
    public class OSFont : MonoBehaviour
    {
        protected TextMeshProUGUI Text;
        
        protected virtual void Start()
        {
            Text = GetComponent<TextMeshProUGUI>();
            Text.font = FontManager.Instance.OSFont;
            Text.spriteAsset = FontManager.Instance.GameFont;
        }
    }
}