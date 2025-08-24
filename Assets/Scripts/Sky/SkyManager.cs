using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityXOPS
{
    /// <summary>
    /// SkyLoader 클래스는 <see cref="SkyParameter">SkyParameter</see>의 데이터를 게임 스카이박스로 변환하는데 사용합니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Singleton{T}">Singleton</see> 클래스입니다.
    /// </remarks>
    public class SkyManager : Singleton<SkyManager>
    {
        public bool Load { get; private set; }
        
        private Camera _mainCamera;
        
        private static readonly int Mode = Shader.PropertyToID("_Mode");
        private static readonly int Glossiness = Shader.PropertyToID("_Glossiness");
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");

        private void Update()
        {
            if (Load && _mainCamera)
            {
                // 스카이박스는 카메라의 원점에 있어야 함
                // 안 그러면 하늘이 움직임
                transform.position = _mainCamera.transform.position;
            }
        }

        /// <summary>
        /// 인덱스에 맞는 Sky를 로드합니다.
        /// </summary>
        /// <param name="index">SkyParameter를 지정하는 Index</param>
        public void LoadSky(int index)
        {
            if (Load)
            {
                return;
            }
            
            Load = true;
            _mainCamera = Camera.main;

            var sky = ParameterManager.Instance.skyParameters[index];
            
            var skyModelPath = Path.Combine(Application.streamingAssetsPath, "data", "sky", "sky.x");
            var skyTexturePath = Path.Combine(Application.streamingAssetsPath, sky.skyTexturePath);
            var billboardTexturePath = Path.Combine(Application.streamingAssetsPath, sky.billboardTexturePath);
            var cloudTexturePath = Path.Combine(Application.streamingAssetsPath, sky.cloudTexturePath);
            var lightTexturePath = Path.Combine(Application.streamingAssetsPath, sky.lightTexturePath);

            var skyMesh = ModelManager.Instance.LoadModel(skyModelPath);
            var skyTexture = ImageManager.Instance.LoadImage(skyTexturePath);
            var billboardTexture = ImageManager.Instance.LoadImage(billboardTexturePath);
            var cloudTexture = ImageManager.Instance.LoadImage(cloudTexturePath);
            var lightTexture = ImageManager.Instance.LoadImage(lightTexturePath);

            // sky
            if (skyTexture)
            {
                var skyObj = new GameObject("Sky");
                skyObj.transform.SetParent(transform);
                skyObj.transform.localPosition = Vector3.zero;
                skyObj.transform.localRotation = Quaternion.identity;
                skyObj.transform.localScale = new Vector3(600f, 600f, 600f);
                skyObj.AddComponent<MeshFilter>().mesh = skyMesh;
                var skyRenderer = skyObj.AddComponent<MeshRenderer>();
                var skyMaterial = new Material(Shader.Find("Standard"));
                skyMaterial.name = skyTexture.name;
                skyMaterial.mainTexture = skyTexture;
                skyMaterial.SetFloat(Mode, 0f);
                skyMaterial.SetOverrideTag("RenderType", "Opaque");
                skyMaterial.SetInt(ZWrite, 1);
                skyMaterial.SetFloat(Glossiness, 0f);
                skyMaterial.renderQueue = (int)RenderQueue.Background;
                skyRenderer.material = skyMaterial;
#if UNITY_EDITOR
                Debug.Log($"[SkyLoader] sky {index}: sky set");
#endif
            }
            
            // billboard
            if (billboardTexture)
            {
                var billboardObj = new GameObject("Billboard");
                billboardObj.transform.SetParent(transform);
                billboardObj.transform.localPosition = Vector3.zero;
                billboardObj.transform.localRotation = Quaternion.identity;
                billboardObj.transform.localScale = new Vector3(590f, 590f, 590f);
                billboardObj.AddComponent<MeshFilter>().mesh = skyMesh;
                var billboardRenderer = billboardObj.AddComponent<MeshRenderer>();
                var billboardMaterial = new Material(Shader.Find("Standard"));
                billboardMaterial.name = billboardTexture.name;
                billboardMaterial.mainTexture = billboardTexture;
                billboardMaterial.SetFloat(Mode, 3f);
                billboardMaterial.SetOverrideTag("RenderType", "Transparent");
                billboardMaterial.SetInt(SrcBlend, (int)BlendMode.SrcAlpha);
                billboardMaterial.SetInt(DstBlend, (int)BlendMode.OneMinusSrcAlpha);
                billboardMaterial.SetInt(ZWrite, 0);
                billboardMaterial.EnableKeyword("_ALPHABLEND_ON");
                billboardMaterial.DisableKeyword("_ALPHATEST_ON");
                billboardMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                billboardMaterial.SetFloat(Glossiness, 0f);
                billboardMaterial.renderQueue = (int)RenderQueue.Transparent - 1;
                billboardRenderer.material = billboardMaterial;
#if UNITY_EDITOR
                Debug.Log($"[SkyLoader] sky {index}: billboard set");
#endif
            }

            // cloud
            if (cloudTexture)
            {
                var cloudObj = new GameObject("Cloud");
                cloudObj.transform.SetParent(transform);
                cloudObj.transform.localPosition = Vector3.zero;
                cloudObj.transform.localRotation = Quaternion.identity;
                cloudObj.transform.localScale = new Vector3(580f, 580f, 580f); // Smaller than billboard
                cloudObj.AddComponent<MeshFilter>().mesh = skyMesh;
                var cloudRenderer = cloudObj.AddComponent<MeshRenderer>();
                var cloudMaterial = new Material(Shader.Find("Standard"));
                cloudMaterial.name = cloudTexture.name;
                cloudMaterial.mainTexture = cloudTexture;
                cloudMaterial.SetFloat(Mode, 3f);
                cloudMaterial.SetOverrideTag("RenderType", "Transparent");
                cloudMaterial.SetInt(SrcBlend, (int)BlendMode.SrcAlpha);
                cloudMaterial.SetInt(DstBlend, (int)BlendMode.OneMinusSrcAlpha);
                cloudMaterial.SetInt(ZWrite, 0);
                cloudMaterial.EnableKeyword("_ALPHABLEND_ON");
                cloudMaterial.DisableKeyword("_ALPHATEST_ON");
                cloudMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                cloudMaterial.SetFloat(Glossiness, 0f);
                cloudMaterial.renderQueue = (int)RenderQueue.Transparent;
                cloudRenderer.material = cloudMaterial;
#if UNITY_EDITOR
                Debug.Log($"[SkyLoader] sky {index}: cloud set");
#endif
            }
            // light
            if (sky.light)
            {
                var lightSourceObj = new GameObject("DirectionalLight");
                lightSourceObj.transform.SetParent(transform, false);
                lightSourceObj.transform.localRotation = Quaternion.Euler(sky.lightDirection.x, sky.lightDirection.y, 0);

                var directionalLight = lightSourceObj.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
                directionalLight.intensity = sky.lightStrength;
                directionalLight.color = sky.lightColor;
                if (lightTexture)
                {
                    var lightVisualObj = new GameObject("LightVisual");
                    lightVisualObj.transform.SetParent(transform, false);
                    lightVisualObj.transform.localPosition = -lightSourceObj.transform.forward * 598f;
                    lightVisualObj.transform.LookAt(transform);

                    lightVisualObj.transform.localScale = new Vector3(50, 50, 50);
                    lightVisualObj.AddComponent<MeshFilter>().mesh = CreatePlaneMesh();
                    var lightRenderer = lightVisualObj.AddComponent<MeshRenderer>();
                    var lightMaterial = new Material(Shader.Find("Standard"));
                    lightMaterial.name = lightTexture.name;
                    lightMaterial.mainTexture = lightTexture;
                    lightMaterial.EnableKeyword("_EMISSION");
                    lightMaterial.SetColor(EmissionColor, Color.white);
                    lightMaterial.SetTexture(EmissionMap, lightTexture);
                    lightMaterial.SetFloat(Mode, 3f);
                    lightMaterial.SetOverrideTag("RenderType", "Transparent");
                    lightMaterial.SetInt(SrcBlend, (int)BlendMode.One);
                    lightMaterial.SetInt(DstBlend, (int)BlendMode.One);
                    lightMaterial.SetInt(ZWrite, 0);
                    lightMaterial.EnableKeyword("_ALPHABLEND_ON");
                    lightMaterial.DisableKeyword("_ALPHATEST_ON");
                    lightMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    lightMaterial.SetFloat(Glossiness, 0f);
                    lightMaterial.renderQueue = (int)RenderQueue.Transparent + 1;
                    lightRenderer.material = lightMaterial;
                }
#if UNITY_EDITOR
                Debug.Log($"[SkyLoader] sky {index}: light set");
#endif
            }
#if UNITY_EDITOR
            Debug.Log($"[SkyLoader] sky {index} completely loaded");
#endif
        }

        /// <summary>
        /// Sky를 파괴합니다.
        /// </summary>
        public void DestroySky()
        {
            transform.position = Vector3.zero;
            Load = false;
            _mainCamera = null;
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
#if UNITY_EDITOR
            Debug.Log($"[SkyLoader] sky destroyed");
#endif
        }
        
        /// <summary>
        /// 광원체가 될 평면 텍스쳐를 생성합니다.
        /// </summary>
        /// <returns>평면으로 된 객체</returns>
        private Mesh CreatePlaneMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.uv = new[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            mesh.RecalculateNormals();
            return mesh;
        }

    }
}
