using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// Represents the opening sequence of a Unity scene, managing the initial camera and fade-in effect.
    /// </summary>
    public class OpeningSequence : MonoBehaviour
    {
        [SerializeField]
        private GameObject openingCamera;
        [SerializeField]
        private GameObject fadeImage;
        [SerializeField]
        private GameObject textRoot;

        private Transform _cameraTransform;
        private Image _fadeImage;
        private Transform _textRootTransform;
        private float _timer;

        private int _optionChosen;
        
        private List<TextMeshProUGUI> _texts;
        
        private void Start()
        {
            _cameraTransform = openingCamera.transform;
            _fadeImage = fadeImage.GetComponent<Image>();
            _textRootTransform = textRoot.transform;
            _timer = 0;
            
            if (StateMachine.Instance.CurrentState == GameState.OpeningStart)
            {
                _optionChosen = Random.Range(0, OpeningData.OpeningOptions.Count);
                var bd1Path = Path.Combine(Application.streamingAssetsPath, OpeningData.OpeningOptions[_optionChosen].bd1Path);
                BlockDataReader.Instance.ReadBD1(bd1Path);
                BlockDataLoader.Instance.LoadBD1(bd1Path);

                _cameraTransform.position = OpeningData.OpeningOptions[_optionChosen].startPosition;
                _cameraTransform.rotation = Quaternion.Euler(OpeningData.OpeningOptions[_optionChosen].startRotation);

                _texts = new List<TextMeshProUGUI>();
                for (var i = 0; i < OpeningData.MessageData.Count; i++)
                {
                    var obj = new GameObject($"text_{i}");
                    obj.transform.SetParent(_textRootTransform);
                    
                    var rt = obj.AddComponent<RectTransform>();
                    rt.pivot = OpeningData.MessageData[i].pivot;
                    rt.anchorMin = OpeningData.MessageData[i].anchorMin;
                    rt.anchorMax = OpeningData.MessageData[i].anchorMax;
                    rt.anchoredPosition = OpeningData.MessageData[i].anchoredPosition;
                    rt.sizeDelta = OpeningData.MessageData[i].sizeDelta;
                    
                    var tmp = obj.AddComponent<TextMeshProUGUI>();
                    tmp.font = FontManager.Instance.OSFont;
                    tmp.spriteAsset = FontManager.Instance.GameFont;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = OpeningData.MessageData[i].sizeDelta.y * 0.748f;
                    tmp.fontSizeMax = OpeningData.MessageData[i].sizeDelta.y * 0.752f;
                    tmp.textWrappingMode = TextWrappingModes.Normal;
                    tmp.alignment = OpeningData.MessageData[i].alignment;
                    tmp.text = OpeningData.MessageData[i].text;
                    tmp.color = new Color(1, 1, 1, 0);
                    
                    tmp.ForceMeshUpdate();
                    
                    _texts.Add(tmp);
                }
                
                StateMachine.Instance.NextState();
            }
        }
        private void Update()
        {
            //fade start
            var fadeColor = _fadeImage.color;
            if (_timer >= OpeningData.FadeValue.fadeInStart && _timer <= OpeningData.FadeValue.fadeInEnd)
            {
                var lerp = Mathf.InverseLerp(OpeningData.FadeValue.fadeInStart, OpeningData.FadeValue.fadeInEnd, _timer);
                fadeColor.a = 1.0f - lerp;
            }
            else if (_timer > OpeningData.FadeValue.fadeInEnd && _timer < OpeningData.FadeValue.fadeOutStart)
            {
                fadeColor.a = 0.0f;
            }
            else if (_timer >= OpeningData.FadeValue.fadeOutStart && _timer <= OpeningData.FadeValue.fadeOutEnd)
            {
                var lerp = Mathf.InverseLerp(OpeningData.FadeValue.fadeOutStart, OpeningData.FadeValue.fadeOutEnd, _timer);
                fadeColor.a = lerp;
            }
            else if (_timer < OpeningData.FadeValue.fadeInStart || _timer > OpeningData.FadeValue.fadeOutEnd)
            {
                fadeColor.a = 1.0f;
            }
            _fadeImage.color = fadeColor;

            //fade end
            
            //text start
            /*
            사실 start, end를 지정하고 while을 잘 쓰면
            반복을 줄일수 있긴 한데, 버그가 나기 쉽기도 하고 무엇보다도 귀찮죠?
             */
            for (var i = 0; i < _texts.Count; i++)
            {
                var text = _texts[i];
                var messageFadeData = OpeningData.MessageData[i].fade;
                var color = text.color;

                if (_timer >= messageFadeData.fadeInStart && _timer <= messageFadeData.fadeInEnd)
                {
                    var lerp = Mathf.InverseLerp(messageFadeData.fadeInStart, messageFadeData.fadeInEnd, _timer);
                    color.a = lerp;
                }
                else if (_timer > messageFadeData.fadeInEnd && _timer < messageFadeData.fadeOutStart)
                {
                    color.a = 1.0f;
                }
                else if (_timer >= messageFadeData.fadeOutStart && _timer <= messageFadeData.fadeOutEnd)
                {
                    var lerp = Mathf.InverseLerp(messageFadeData.fadeOutStart, messageFadeData.fadeOutEnd, _timer);
                    color.a = 1.0f - lerp;
                }
                else if (_timer > messageFadeData.fadeOutEnd)
                {
                    color.a = 0.0f;
                }

                text.color = color;
                
                text.ForceMeshUpdate();
            }
            //text end
            
            //camera position start
            var option = OpeningData.OpeningOptions[_optionChosen];
            for (var i = 0; i < option.positionData.Count; i++)
            {
                var currentPosData = option.positionData[i];
                if (!(_timer >= currentPosData.time.x && _timer <= currentPosData.time.y))
                {
                    continue;
                }
                
                var startPos = i == 0 ? option.startPosition : option.positionData[i - 1].position;
                var endPos = currentPosData.position;
                var lerp = Mathf.InverseLerp(currentPosData.time.x, currentPosData.time.y, _timer);
                _cameraTransform.position = Vector3.Lerp(startPos, endPos, lerp);
                break;
            }

            for (var i = 0; i < option.rotationData.Count; i++)
            {
                var currentRotData = option.rotationData[i];
                if (!(_timer >= currentRotData.time.x && _timer <= currentRotData.time.y))
                {
                    continue;
                }
                
                var startRot = i == 0 ? option.startRotation : option.rotationData[i - 1].position;
                var endRot = currentRotData.position;
                var lerp = Mathf.InverseLerp(currentRotData.time.x, currentRotData.time.y, _timer);
                _cameraTransform.rotation = Quaternion.Euler(Vector3.Lerp(startRot, endRot, lerp));
                break;
            }
            //camera position end
            
            //state
            if (Input.anyKeyDown || OpeningData.OpeningEnd <= _timer)
            {
                StateMachine.Instance.AnyKeyFlag();
            }

            if (StateMachine.Instance.CurrentState == GameState.OpeningEnd)
            {
                BlockDataLoader.Instance.DestroyBD1();
                BlockDataReader.Instance.ClearBD1();
                
                StateMachine.Instance.NextState();
            }
            //state end
            
            _timer += Time.deltaTime;
        }
    }
}