using System;
using TMPro;
using UnityEngine;

namespace UnityXOPS
{
    [Serializable]
    public class TextSequence : Sequence
    {
        public string text;
        public bool gameFont;
        public Color start;
        public Color end;
        public Vector2 pivot;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public TextAlignmentOptions alignment;
        public float ratio;
    }
}