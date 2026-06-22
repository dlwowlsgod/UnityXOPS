using JJLUtility;
using JJLUtility.IO;
using UnityEngine;

namespace UnityXOPS
{
    public partial class MapLoader
    {
        [SerializeField]
        private Transform skyRoot;

        /// <summary>
        /// 스카이 메시와 텍스처를 로드해 메인 카메라 하위에 스카이박스 오브젝트를 생성한다.
        /// </summary>
        /// <param name="textureIndex">SkyData 텍스처 경로 목록의 인덱스.</param>
        public static void LoadSkyData(int textureIndex)
        {
            var skyData = DataManager.Instance.SkyData;

            if (skyData == null)
            {
                Debugger.LogError("SkyData is null.", Instance, nameof(MapLoader));
                return;
            }

            string streamingPath = Application.streamingAssetsPath;
            string fullMeshPath = SafePath.Combine(streamingPath, skyData.skyMeshPath);

            Mesh skyMesh = ModelLoader.LoadMesh(fullMeshPath);
            if (skyMesh == null)
            {
                Debugger.LogError($"Failed to load sky mesh: {fullMeshPath}", Instance, nameof(MapLoader));
                return;
            }

            // textureIndex가 유효하고 경로가 비어있지 않으면 텍스처 적용, 아니면 검은색
            Material skyMaterial = new Material(MaterialManager.Instance.SkyMaterial);
            skyMaterial.name = "SkyMaterial";

            if (textureIndex > 0 && textureIndex < skyData.skyTexturePath.Count)
            {
                string texPath = skyData.skyTexturePath[textureIndex];
                if (!string.IsNullOrEmpty(texPath))
                {
                    string fullTexPath = SafePath.Combine(streamingPath, texPath);
                    Texture2D tex = ImageLoader.LoadTexture(fullTexPath);
                    if (tex != null)
                        skyMaterial.mainTexture = tex;
                }
            }

            // Skybox를 MapLoader 하위(skyRoot) 아래에 붙여 씬 전환 시에도 유지.
            // 쉐이더는 _WorldSpaceCameraPos 기준 렌더라 렌더링은 트랜스폼 무관하지만,
            // Unity 프러스텀 컬링은 transform 위치를 사용 → skyRoot는 별도 스크립트로 카메라 추적.
            GameObject skyObject = new GameObject("Skybox");
            skyObject.transform.SetParent(Instance.skyRoot, false);
            skyObject.AddComponent<MeshFilter>().sharedMesh = skyMesh;
            skyObject.AddComponent<MeshRenderer>().sharedMaterial = skyMaterial;

            // fog(RenderSettings)는 씬별 자산이라 여기서 걸면 씬 전환에 씻겨나간다.
            // fog 가 보여야 할 씬의 카메라(CameraClippingApplier)가 Awake 에서 ApplySkyFog 를 직접 호출한다.
        }

        /// <summary>
        /// 메인 카메라 하위의 스카이박스 오브젝트를 모두 제거한다.
        /// </summary>
        public static void UnloadSkyData()
        {
            if (Instance.skyRoot == null) return;
            foreach (Transform child in Instance.skyRoot)
            {
                var renderer = child.GetComponent<MeshRenderer>();
                if (renderer != null) DestroyIfRuntimeMaterial(renderer.sharedMaterial);
                Destroy(child.gameObject);
            }

            ClearFog();
        }

        /// <summary>
        /// 하늘 번호별 fog 색 — SkyData.skyColor (sky_data.json) 에서 skyIndex 로 조회. 원본 SetFog skycolor switch 의 데이터화.
        /// AlphaClipBlend(블록/소물/사람/무기)만 multi_compile_fog 를 가져 fog 대상이 되고, SkyMesh·EffectBlend 는 제외된다.
        /// 데이터가 없거나 범위를 벗어나면 검정(원본 default) 반환.
        /// </summary>
        private static Color SkyFogColor(int skyIndex)
        {
            var colors = DataManager.Instance.SkyData?.skyColor;
            if (colors == null || skyIndex < 0 || skyIndex >= colors.Count)
                return new Color32(0, 0, 0, 255);
            return colors[skyIndex];
        }

        /// <summary>
        /// 원본 SetFog(true, skynumber) 대응. Linear fog 를 SkyData(fogClippingPlane ~ farClippingPlane) 범위, skyIndex 색으로 적용한다.
        /// SkyData.fog 가 false 거나 데이터가 없으면 fog 를 끈다. 맵/스카이 로드 후 호출 (skyIndex 가 세팅된 뒤).
        /// </summary>
        public static void ApplySkyFog(int skyIndex)
        {
            var skyData = DataManager.Instance.SkyData;
            if (skyData == null || !skyData.fog)
            {
                ClearFog();
                return;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = skyData.fogStart;
            RenderSettings.fogEndDistance = skyData.fogEnd;
            RenderSettings.fogColor = SkyFogColor(skyIndex);
        }

        /// <summary>원본 SetFog(false, …) 대응. 맵 언로드 시 fog 해제.</summary>
        public static void ClearFog()
        {
            RenderSettings.fog = false;
        }
    }
}
