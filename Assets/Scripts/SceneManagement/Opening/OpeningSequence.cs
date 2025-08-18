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

        private Vector4 _fadeTime;
        private float _openingEndTime;
        private List<TextMeshProUGUI> _texts;
        private List<Vector4> _textFades;
        private Vector3 _startPos;
        private Vector3 _startRot;
        private List<Vector2> _cameraPosTimes;
        private List<Vector3> _cameraPos;
        private List<Vector2> _cameraRotTimes;
        private List<Vector3> _cameraRot;
        
        private void Start()
        {
            _cameraTransform = openingCamera.transform;
            _fadeImage = fadeImage.GetComponent<Image>();
            _textRootTransform = textRoot.transform;
            _timer = 0;

            var vScale = Screen.height / 540f;
            
            if (StateMachine.Instance.CurrentState == GameState.OpeningStart)
            {
                var bd1Path = Path.Combine(Application.streamingAssetsPath, "data", "map10", "temp.bd1");
                BlockDataReader.Instance.ReadBD1(bd1Path);
                BlockDataLoader.Instance.LoadBD1(bd1Path);
                SkyLoader.Instance.LoadSky(1);

                _fadeTime = new Vector4(0f, 2f, 11f, 13f);
                _openingEndTime = 17f;
                _cameraTransform.position = _startPos = new Vector3(0f, 6.85f, 2.3f);
                _cameraTransform.rotation = Quaternion.Euler(_startRot = new Vector3(12f, 255f, 0f));

                _texts = new List<TextMeshProUGUI>();
                var text0 = FontManager.Instance.CreateText(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -100f),
                    new Vector2(1000f, 30f),
                    TextAlignmentOptions.Center,
                    "UnityXOPS", "FFFFFF", true, _textRootTransform);
                var text1 = FontManager.Instance.CreateText(
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(25f, 180f),
                    new Vector2(1000f, 30f),
                    TextAlignmentOptions.MidlineLeft,
                    "ORIGINAL", "FFFFFF", true, _textRootTransform);
                var text2 = FontManager.Instance.CreateText(
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(50f, 155f),
                    new Vector2(1000f, 30f),
                    TextAlignmentOptions.MidlineLeft,
                    "TEAM-MITEI", "FFFFFF", true, _textRootTransform);
                var text3 = FontManager.Instance.CreateText(
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(50f, 155f),
                    new Vector2(1000f, 30f),
                    TextAlignmentOptions.MidlineLeft,
                    "TEAM-MITEI", "FFFFFF", true, _textRootTransform);
                var text4 = FontManager.Instance.CreateText(
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(25f, -45f),
                    new Vector2(1000f, 30f),
                    TextAlignmentOptions.MidlineLeft,
                    "THANKS TO", "FFFFFF", true, _textRootTransform);
                var text5 = FontManager.Instance.CreateText(
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(50f, -70f),
                    new Vector2(1000f, 30f),
                    TextAlignmentOptions.MidlineLeft,
                    "[-_-;](mikan)", "FFFFFF", true, _textRootTransform);
                var text6 = FontManager.Instance.CreateText(
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(-25f, 65f),
                    new Vector2(1000f, 30f),
                    TextAlignmentOptions.MidlineRight,
                    "UnityXOPS", "FFFFFF", true, _textRootTransform);
                var text7 = FontManager.Instance.CreateText(
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(-50f, 45f),
                    new Vector2(1000f, 30f),
                    TextAlignmentOptions.MidlineRight,
                    "dlwowlsgod", "FFFFFF", true, _textRootTransform);
                var text8 = FontManager.Instance.CreateText(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(1000f, 30f),
                    TextAlignmentOptions.Center,
                    "X OPERATIONS", "FF0000", true, _textRootTransform);
                
                _texts.Add(text0.GetComponent<TextMeshProUGUI>());
                _texts.Add(text1.GetComponent<TextMeshProUGUI>());
                _texts.Add(text2.GetComponent<TextMeshProUGUI>());
                _texts.Add(text3.GetComponent<TextMeshProUGUI>());
                _texts.Add(text4.GetComponent<TextMeshProUGUI>());
                _texts.Add(text5.GetComponent<TextMeshProUGUI>());
                _texts.Add(text6.GetComponent<TextMeshProUGUI>());
                _texts.Add(text7.GetComponent<TextMeshProUGUI>());
                _texts.Add(text8.GetComponent<TextMeshProUGUI>());

                foreach (var text in _texts)
                {
                    text.color = new Color(text.color.r, text.color.g, text.color.b, 0f);
                }

                _textFades = new List<Vector4>
                {
                    new(0.5f, 1.5f, 3f, 4f),
                    new(4.5f, 5.5f, 7.5f, 8.5f),
                    new(5f, 6f, 8f, 9f),
                    new(5.5f, 6.5f, 8.5f, 9.5f),
                    new(6f, 7f, 9f, 10f),
                    new(6.5f, 7.5f, 9.5f, 10.5f),
                    new(7f, 8f, 10f, 11f),
                    new(7.5f, 8.5f, 10.5f, 11.5f),
                    new(11f, 12f, 16f, 17f)
                };

                _cameraPosTimes = new List<Vector2>
                {
                    new(0f, 2.6f),
                    new(2.6f, 5f),
                    new(5f, 17f)
                };
                _cameraPos = new List<Vector3>
                {
                    new(0f, 6.85f, 2.3f),
                    new(0f, 6.35f, 6.39f),
                    new(0f, 4f, 11f)
                };
                _cameraRotTimes = new List<Vector2>
                {
                    new(0f, 2.6f),
                    new(2.6f, 5f),
                    new(5f, 17f)
                };
                _cameraRot = new List<Vector3>
                {
                    new(12f, 255f, 0f),
                    new(30f, 180f, 0f),
                    new(25f, 180f, 0f)
                };
                StateMachine.Instance.NextState();
            }
        }
        private void Update()
        {
            //fade start
            var fadeColor = _fadeImage.color;
            if (_timer >= _fadeTime[0] && _timer <= _fadeTime[1])
            {
                var lerp = Mathf.InverseLerp(0f, 2f, _timer);
                fadeColor.a = 1.0f - lerp;
            }
            else if (_timer > _fadeTime[1] && _timer < _fadeTime[2])
            {
                fadeColor.a = 0.0f;
            }
            else if (_timer >= _fadeTime[2] && _timer <= _fadeTime[3])
            {
                var lerp = Mathf.InverseLerp(11f, 13f, _timer);
                fadeColor.a = lerp;
            }
            else if (_timer < _fadeTime[0] || _timer > _fadeTime[3])
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
                var fis = _textFades[i][0];
                var fie = _textFades[i][1];
                var fos = _textFades[i][2];
                var foe = _textFades[i][3];
                var color = text.color;

                if (_timer >= fis && _timer <= fie)
                {
                    var lerp = Mathf.InverseLerp(fis, fie, _timer);
                    color.a = lerp;
                }
                else if (_timer > fie && _timer < fos)
                {
                    color.a = 1.0f;
                }
                else if (_timer >= fos && _timer <= foe)
                {
                    var lerp = Mathf.InverseLerp(fos, foe, _timer);
                    color.a = 1.0f - lerp;
                }
                else if (_timer > foe)
                {
                    color.a = 0.0f;
                }

                text.color = color;
                
                text.ForceMeshUpdate();
            }
            //text end
            
            //camera position start
            for (var i = 0; i < _cameraPos.Count; i++)
            {
                var ts = _cameraPosTimes[i][0];
                var te = _cameraPosTimes[i][1];
                
                if (!(_timer >= ts && _timer <= te))
                {
                    continue;
                }
                
                var startPos = i == 0 ? _startPos : _cameraPos[i - 1];
                var endPos = _cameraPos[i];
                var lerp = Mathf.InverseLerp(ts, te, _timer);
                _cameraTransform.position = Vector3.Lerp(startPos, endPos, lerp);
                break;
            }
            //camera position end

            //camera rotation start
            for (var i = 0; i < _cameraRot.Count; i++)
            {
                var ts = _cameraRotTimes[i][0];
                var te = _cameraRotTimes[i][1];
                if (!(_timer >= ts && _timer <= te))
                {
                    continue;
                }
                
                var startRot = i == 0 ? _startRot : _cameraRot[i - 1];
                var endRot = _cameraRot[i];
                var lerp = Mathf.InverseLerp(ts, te, _timer);
                _cameraTransform.rotation = Quaternion.Euler(Vector3.Lerp(startRot, endRot, lerp));
                break;
            }
            //camera rotation end
            
            //state
            if (StateMachine.Instance.CurrentState == GameState.OpeningUpdate
            && (Input.anyKeyDown || _openingEndTime <= _timer))
            {
                StateMachine.Instance.AnyKeyFlag();
            }
            if (StateMachine.Instance.CurrentState == GameState.OpeningEnd)
            {
                BlockDataLoader.Instance.DestroyBD1();
                BlockDataReader.Instance.ClearBD1();
                SkyLoader.Instance.DestroySky();
                
                StateMachine.Instance.NextState();
            }
            //state end
            
            _timer += Time.deltaTime;
        }
    }
}