using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using TMPro;

namespace UnityXOPS
{
    /// <summary>
    /// A static class that provides functionality to load and manage opening data for the application.
    /// </summary>
    public static class OpeningData
    {
        public static FadeData FadeValue;
        public static float OpeningEnd;
        public static readonly List<MessageData> MessageData = new List<MessageData>();
        public static readonly List<OpeningOptions> OpeningOptions = new List<OpeningOptions>();

        /// <summary>
        /// Loads the opening data from a predefined JSON file located in the application's streaming assets directory.
        /// Parses and initializes related path strings, force state transition time, as well as fade, position,
        /// and rotation data required for the application.
        /// </summary>
        /// <remarks>
        /// The method expects the file "opening.json" to exist under the "opening" folder in the streaming assets path
        /// and processes its contents line-by-line. If the file is not found or an error occurs during processing,
        /// appropriate error messages are logged in the Unity Editor.
        /// </remarks>
        /// <exception cref="System.Exception">
        /// Triggers logging of an error message in case of a failure to parse or retrieve the opening data.
        /// </exception>
        public static void LoadOpeningData()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "common", "OpeningData.txt");
            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError("[OpeningData] File is not exist.");
#endif
                DefaultData();
                return;
            }

            try
            {
                var lines = File.ReadAllLines(path);

                var fadeSplit = lines[0].Split(',');
                FadeValue = new FadeData()
                {
                    fadeInStart = float.TryParse(fadeSplit[0], out var fis) ? fis * 1.1f : 0f,
                    fadeInEnd = float.TryParse(fadeSplit[1], out var fie) ? fie * 1.1f : 0f,
                    fadeOutStart = float.TryParse(fadeSplit[2], out var fos) ? fos * 1.1f : 0f,
                    fadeOutEnd = float.TryParse(fadeSplit[3], out var foe) ? foe * 1.1f : 0f,
                };

                OpeningEnd = float.TryParse(fadeSplit[4], out var oe) ? oe * 1.1f : 0.5f;
                
                var textCount = int.Parse(lines[1]);
                for (var i = 0; i < textCount; i++)
                {
                    var index = 2 + 8 * i;
                    
                    var message = new MessageData();
                    
                    var split = lines[index].Split(',');
                    message.fade = new FadeData
                    {
                        fadeInStart = float.TryParse(split[0], out var tfis) ? tfis * 1.1f : 0f,
                        fadeInEnd = float.TryParse(split[1], out var tfie) ? tfie * 1.1f : 0f,
                        fadeOutStart = float.TryParse(split[2], out var tfos) ? tfos * 1.1f : 0f,
                        fadeOutEnd = float.TryParse(split[3], out var tfoe) ? tfoe * 1.1f : 0f,
                    };
                    
                    split = lines[index + 1].Split(',');
                    message.pivot = new Vector2
                    {
                        x = float.TryParse(split[0], out var px) ? px : 0f,
                        y = float.TryParse(split[1], out var py) ? py : 0f
                    };

                    split = lines[index + 2].Split(',');
                    message.anchorMin = new Vector2
                    {
                        x = float.TryParse(split[0], out var aix) ? aix : 0f,
                        y = float.TryParse(split[1], out var aiy) ? aiy : 0f
                    };
                    
                    split = lines[index + 3].Split(',');
                    message.anchorMax = new Vector2
                    {
                        x = float.TryParse(split[0], out var aax) ? aax : 0f,
                        y = float.TryParse(split[1], out var aay) ? aay : 0f
                    };
                    
                    split = lines[index + 4].Split(',');
                    message.anchoredPosition = new Vector2
                    {
                        x = float.TryParse(split[0], out var apx) ? apx : 0f,
                        y = float.TryParse(split[1], out var apy) ? apy : 0f
                    };
                    
                    split = lines[index + 5].Split(',');
                    message.sizeDelta = new Vector2
                    {
                        x = float.TryParse(split[0], out var sdx) ? sdx : 0f,
                        y = float.TryParse(split[1], out var sdy) ? sdy : 0f
                    };

                    message.alignment = Enum.TryParse<TextAlignmentOptions>(lines[index + 6].TrimEnd(), out var ali) ? ali : TextAlignmentOptions.TopLeft;
                    
                    split = lines[index + 7].Split(',');
                    if (split.Length == 1)
                    {
                        message.text = FontManager.NormalTextToGameText(split[0].TrimEnd());
                    }
                    else
                    {
                        var sb = new StringBuilder();
                        if (split.Length % 2 == 0)
                        {
                            for (var j = 0; j < split.Length; j += 2)
                            {
                                sb.Append(FontManager.NormalTextToGameText(split[j].TrimEnd(), split[j + 1].Trim()));
                            }
                        }
                        else
                        {
                            for (var j = 0; j < split.Length - 1; j += 2)
                            {
                                sb.Append(FontManager.NormalTextToGameText(split[j].TrimEnd(), split[j + 1].Trim()));
                            }
                            sb.Append(FontManager.NormalTextToGameText(split[^1].TrimEnd()));
                        }
                        message.text = sb.ToString();
                        sb.Clear();
                    }
                    
                    MessageData.Add(message);
                }

                var currentIndex = 2 + 8 * textCount;
                if (lines.Length <= currentIndex)
                {
                    throw new Exception("Opening data is not enough.");
                }
                
                var opVarCount = int.TryParse(lines[currentIndex], out var ovc) ? ovc : 0;
                currentIndex++;

                for (var i = 0; i < opVarCount; i++)
                {
                    var option = new OpeningOptions
                    {
                        positionData = new List<CameraData>(),
                        rotationData = new List<CameraData>()
                    };

                    option.bd1Path = lines[currentIndex++].TrimEnd();
                    option.pd1Path = lines[currentIndex++].TrimEnd();

                    var split = lines[currentIndex++].Split(',');
                    option.startPosition = new Vector3(
                        split.Length > 0 && float.TryParse(split[0], out var spx) ? spx : 0f,
                        split.Length > 1 && float.TryParse(split[1], out var spy) ? spy : 0f,
                        split.Length > 2 && float.TryParse(split[2], out var spz) ? spz : 0f
                    );

                    split = lines[currentIndex++].Split(',');
                    option.startRotation = new Vector3(
                        split.Length > 0 && float.TryParse(split[0], out var srx) ? srx : 0f,
                        split.Length > 1 && float.TryParse(split[1], out var sry) ? sry : 0f,
                        split.Length > 2 && float.TryParse(split[2], out var srz) ? srz : 0f
                    );

                    var posMoveCount = int.TryParse(lines[currentIndex++], out var pmc) ? pmc : 0;
                    for (var j = 0; j < posMoveCount; j++)
                    {
                        split = lines[currentIndex++].Split(',');
                        var time = new Vector2(
                            split.Length > 0 && float.TryParse(split[0], out var ptx) ? ptx * 1.1f : 0f,
                            split.Length > 1 && float.TryParse(split[1], out var pty) ? pty * 1.1f : 0f
                        );

                        split = lines[currentIndex++].Split(',');
                        var pos = new Vector3(
                            split.Length > 0 && float.TryParse(split[0], out var ppx) ? ppx : 0f,
                            split.Length > 1 && float.TryParse(split[1], out var ppy) ? ppy : 0f,
                            split.Length > 2 && float.TryParse(split[2], out var ppz) ? ppz : 0f
                        );

                        option.positionData.Add(new CameraData { time = time, position = pos });
                    }

                    var rotMoveCount = int.TryParse(lines[currentIndex++], out var rmc) ? rmc : 0;
                    for (var j = 0; j < rotMoveCount; j++)
                    {
                        split = lines[currentIndex++].Split(',');
                        var time = new Vector2(
                            split.Length > 0 && float.TryParse(split[0], out var rtx) ? rtx * 1.1f : 0f,
                            split.Length > 1 && float.TryParse(split[1], out var rty) ? rty * 1.1f : 0f
                        );

                        split = lines[currentIndex++].Split(',');
                        var rot = new Vector3(
                            split.Length > 0 && float.TryParse(split[0], out var rrx) ? rrx : 0f,
                            split.Length > 1 && float.TryParse(split[1], out var rry) ? rry : 0f,
                            split.Length > 2 && float.TryParse(split[2], out var rrz) ? rrz : 0f
                        );

                        option.rotationData.Add(new CameraData { time = time, position = rot });
                    }

                    OpeningOptions.Add(option);
                }
                
#if UNITY_EDITOR
                Debug.Log("[OpeningData] Opening data successfully loaded.");
#endif
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"[OpeningData] Failed to load opening data. Error: {e.Message}");
#endif
                DefaultData();
            }
        }

        private static void DefaultData()
        {
            FadeValue = new FadeData(0f * 1.1f, 2f * 1.1f, 11f * 1.1f, 13f * 1.1f);
            OpeningEnd = 17f * 1.1f;
            MessageData.Clear();
            MessageData.Add(new MessageData
            {
                fade = new FadeData(0.5f * 1.1f, 1.5f * 1.1f, 3f * 1.1f, 4f * 1.1f),
                pivot = new Vector2(0.5f, 0.5f),
                anchorMin = new Vector2(0.5f, 0.5f),
                anchorMax = new Vector2(0.5f, 0.5f),
                anchoredPosition = new Vector2(0, -100),
                sizeDelta = new Vector2(1000, 30),
                alignment = TextAlignmentOptions.Center,
                text = FontManager.NormalTextToGameText("UnityXOPS Project")
            });
            MessageData.Add(new MessageData
            {
                fade = new FadeData(4.5f * 1.1f, 5.5f * 1.1f, 7.5f * 1.1f, 8.5f * 1.1f),
                pivot = new Vector2(0f, 1f),
                anchorMin = new Vector2(0f, 0.5f),
                anchorMax = new Vector2(0f, 0.5f),
                anchoredPosition = new Vector2(25, 180),
                sizeDelta = new Vector2(1000, 30),
                alignment = TextAlignmentOptions.MidlineLeft,
                text = FontManager.NormalTextToGameText("ORIGINAL")
            });
            MessageData.Add(new MessageData
            {
                fade = new FadeData(5f * 1.1f, 6f * 1.1f, 8f * 1.1f, 9f * 1.1f),
                pivot = new Vector2(0f, 1f),
                anchorMin = new Vector2(0f, 0.5f),
                anchorMax = new Vector2(0f, 0.5f),
                anchoredPosition = new Vector2(50, 155),
                sizeDelta = new Vector2(1000, 30),
                alignment = TextAlignmentOptions.MidlineLeft,
                text = FontManager.NormalTextToGameText("TEAM-MITEI")
            });
            MessageData.Add(new MessageData
            {
                fade = new FadeData(6f * 1.1f, 7f * 1.1f, 9f * 1.1f, 10f * 1.1f),
                pivot = new Vector2(0f, 1f),
                anchorMin = new Vector2(0f, 0.5f),
                anchorMax = new Vector2(0f, 0.5f),
                anchoredPosition = new Vector2(25, -45),
                sizeDelta = new Vector2(1000, 30),
                alignment = TextAlignmentOptions.MidlineLeft,
                text = FontManager.NormalTextToGameText("THANKS TO")
            });
            MessageData.Add(new MessageData
            {
                fade = new FadeData(6.5f * 1.1f, 7.5f * 1.1f, 9.5f * 1.1f, 10.5f * 1.1f),
                pivot = new Vector2(0f, 1f),
                anchorMin = new Vector2(0f, 0.5f),
                anchorMax = new Vector2(0f, 0.5f),
                anchoredPosition = new Vector2(50, -70),
                sizeDelta = new Vector2(1000, 30),
                alignment = TextAlignmentOptions.MidlineLeft,
                text = FontManager.NormalTextToGameText("[-_-;](mikan)")
            });
            MessageData.Add(new MessageData
            {
                fade = new FadeData(7.5f * 1.1f, 8.5f * 1.1f, 10.5f * 1.1f, 11.5f * 1.1f),
                pivot = new Vector2(1f, 1f),
                anchorMin = new Vector2(1f, 0.5f),
                anchorMax = new Vector2(1f, 0.5f),
                anchoredPosition = new Vector2(-25, 65),
                sizeDelta = new Vector2(1000, 30),
                alignment = TextAlignmentOptions.MidlineRight,
                text = FontManager.NormalTextToGameText("UnityXOPS")
            });
            MessageData.Add(new MessageData
            {
                fade = new FadeData(8f * 1.1f, 9f * 1.1f, 11f * 1.1f, 12f * 1.1f),
                pivot = new Vector2(1f, 1f),
                anchorMin = new Vector2(1f, 0.5f),
                anchorMax = new Vector2(1f, 0.5f),
                anchoredPosition = new Vector2(-50, 40),
                sizeDelta = new Vector2(1000, 30),
                alignment = TextAlignmentOptions.MidlineRight,
                text = FontManager.NormalTextToGameText("dlwowlsgod")
            });
            MessageData.Add(new MessageData
            {
                fade = new FadeData(12f * 1.1f, 13f * 1.1f, 15f * 1.1f, 16f * 1.1f),
                pivot = new Vector2(0.5f, 0.5f),
                anchorMin = new Vector2(0.5f, 0.5f),
                anchorMax = new Vector2(0.5f, 0.5f),
                anchoredPosition = new Vector2(0, 0),
                sizeDelta = new Vector2(1000, 30),
                alignment = TextAlignmentOptions.Center,
                text = FontManager.NormalTextToGameText("X OPERATION", "FF0000")
            });
            
            OpeningOptions.Clear();
            OpeningOptions.Add(new OpeningOptions
            {
                bd1Path = @"data\map10\temp.bd1",
                pd1Path = @"data\map10\op.pd1",
                startPosition = new Vector3(0f, 6.85f, 2.3f),
                startRotation = new Vector3(12f, 255f, 0f),
                positionData = new List<CameraData>
                {
                    new CameraData { time = new Vector2(0f * 1.1f, 2.6f * 1.1f), position = new Vector3(0f, 6.85f, 2.3f) },
                    new CameraData { time = new Vector2(2.6f * 1.1f, 5f * 1.1f), position = new Vector3(0f, 6.35f, 6.39f) },
                    new CameraData { time = new Vector2(5f * 1.1f, 17f * 1.1f), position = new Vector3(0f, 4f, 11f) }
                },
                rotationData = new List<CameraData>
                {
                    new CameraData { time = new Vector2(0f * 1.1f, 2.6f * 1.1f), position = new Vector3(12f, 255f, 0f) },
                    new CameraData { time = new Vector2(2.6f * 1.1f, 5f * 1.1f), position = new Vector3(30f, 180f, 0f) },
                    new CameraData { time = new Vector2(5f * 1.1f, 17f * 1.1f), position = new Vector3(25f, 180f, 0f) }
                }
            });

#if UNITY_EDITOR
            Debug.Log("[OpeningData] Default opening data loaded.");
#endif
        }
    }

    [Serializable]
    public class FadeData
    {
        public float fadeInStart;
        public float fadeInEnd;
        public float fadeOutStart;
        public float fadeOutEnd;

        public FadeData(float fadeInStart, float fadeInEnd, float fadeOutStart, float fadeOutEnd)
        {
            this.fadeInStart = fadeInStart;
            this.fadeInEnd = fadeInEnd;
            this.fadeOutStart = fadeOutStart;
            this.fadeOutEnd = fadeOutEnd;
        }

        public FadeData()
        {
            
        }
    }

    [Serializable]
    public class MessageData
    {
        public FadeData fade;
        public Vector2 pivot;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public TextAlignmentOptions alignment;
        public string text;
    }
    
    [Serializable]
    public class OpeningOptions
    {
        public string bd1Path;
        public string pd1Path;
        public Vector3 startPosition;
        public Vector3 startRotation;
        public List<CameraData> positionData;
        public List<CameraData> rotationData;
    }

    [Serializable]
    public class CameraData
    {
        public Vector2 time;
        public Vector3 position;
    }
}