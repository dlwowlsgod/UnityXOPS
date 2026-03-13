using JJLUtility;
using JJLUtility.IO;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityXOPS
{
    public partial class MapLoader : SingletonBehavior<MapLoader>
    {
        [SerializeField]
        private Transform blockRoot;
        [SerializeField]
        private Transform skyRoot;
        [SerializeField]
        private Material BlockOpaqueMaterial;
        [SerializeField]
        private Material BlockTransparentMaterial;

        [SerializeField]
        private int blockCount;
        [SerializeField]
        private List<Material> blockMaterials;

        [SerializeField]
        private string missionName;
        [SerializeField]
        private string missionFullname;
        [SerializeField]
        private string missionBD1Path;
        [SerializeField]
        private string missionPD1Path;
        [SerializeField]
        private string missionAddonObjectPath;
        [SerializeField]
        private string missionImage0;
        [SerializeField]
        private string missionImage1;
        [SerializeField]
        private int skyIndex;
        [SerializeField]
        private string missionBriefing;
        [SerializeField]
        private bool adjustCollision;
        [SerializeField]
        private bool darkScreen;

        public static void LoadBlockData(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                Debugger.LogError("BD1 path is empty.", Instance, nameof(MapLoader));
                return;
            }

            if (!File.Exists(filepath))
            {
                Debugger.LogError($"BD1 file not exists: {filepath}", Instance, nameof(MapLoader));
                return;
            }

            BlockData blockData = LoadBD1File(filepath);
            if (blockData == null)
            {
                return;
            }

            Instance.blockCount = blockData.rawBlockData.Length;
            blockData.blocks = BuildBlocks(blockData.rawBlockData);

            Instance.blockMaterials = new List<Material>();
            string bd1Dir = Path.GetDirectoryName(filepath);
            for (int i = 0; i < blockData.texturePaths.Length; i++)
            {
                string texturePath = blockData.texturePaths[i];

                if (string.IsNullOrEmpty(texturePath))
                {
                    Instance.blockMaterials.Add(Instance.BlockOpaqueMaterial);
                    continue;
                }

                string extension = Path.GetExtension(texturePath).ToLower();
                Material baseMaterial = extension is ".png" or ".dds"
                    ? Instance.BlockTransparentMaterial
                    : Instance.BlockOpaqueMaterial;

                string fullTexturePath = SafePath.Combine(bd1Dir, texturePath);
                Texture2D blockTexture = ImageLoader.LoadTexture(fullTexturePath);

                if (blockTexture == null)
                {
                    Instance.blockMaterials.Add(baseMaterial);
                    continue;
                }

                Material blockMaterial = new Material(baseMaterial);
                blockMaterial.name = Path.GetFileName(texturePath);
                blockMaterial.mainTexture = blockTexture;
                Instance.blockMaterials.Add(blockMaterial);
            }

            for (int i = 0; i < blockData.blocks.Length; i++)
            {
                Block block = blockData.blocks[i];
                GameObject blockObj = new GameObject($"Block_{i}");
                blockObj.transform.SetParent(Instance.blockRoot, false);

                MeshFilter meshFilter = blockObj.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = block.mesh;

                MeshRenderer meshRenderer = blockObj.AddComponent<MeshRenderer>();
                Material[] materials = new Material[block.subMeshTextureIndices.Length];
                for (int j = 0; j < block.subMeshTextureIndices.Length; j++)
                {
                    int textureIndex = block.subMeshTextureIndices[j];
                    if (textureIndex >= 0 && textureIndex < Instance.blockMaterials.Count)
                    {
                        materials[j] = Instance.blockMaterials[textureIndex];
                    }
                    else
                    {
                        materials[j] = new Material(Instance.BlockOpaqueMaterial);
                    }
                }
                meshRenderer.sharedMaterials = materials;

                blockObj.transform.localPosition = block.position;
            }
        }

        public static void UnloadBlockData()
        {
            foreach (Transform child in Instance.blockRoot)
            {
                Destroy(child.gameObject);
            }
            Instance.blockMaterials.Clear();
        }

        public static void LoadSkyData(int textureIndex)
        {
            var skyData = DataManager.Instance.SkyData;

            if (skyData == null)
            {
                Debugger.LogError("SkyData is null.", Instance, nameof(MapLoader));
                return;
            }

            UnloadSkyData();

            string streamingPath = Application.streamingAssetsPath;
            string fullMeshPath = SafePath.Combine(streamingPath, skyData.skyMeshPath);

            Mesh skyMesh = ModelLoader.LoadMesh(fullMeshPath);
            if (skyMesh == null)
            {
                Debugger.LogError($"Failed to load sky mesh: {fullMeshPath}", Instance, nameof(MapLoader));
                return;
            }

            Shader skyShader = Shader.Find("UnityXOPS/SkyMesh");
            if (skyShader == null)
            {
                Debugger.LogError($"Failed to load sky shader.", Instance, nameof(MapLoader));
                return;
            }

            // textureIndex가 유효하고 경로가 비어있지 않으면 텍스처 적용, 아니면 검은색
            Material skyMaterial = new Material(skyShader);
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

            GameObject skyObject = new GameObject("Sky");
            skyObject.transform.SetParent(Instance.skyRoot, false);
            skyObject.AddComponent<MeshFilter>().sharedMesh = skyMesh;
            skyObject.AddComponent<MeshRenderer>().sharedMaterial = skyMaterial;
        }

        public static void UnloadSkyData()
        {
            if (Instance.skyRoot.childCount > 0)
            {
                foreach (Transform child in Instance.skyRoot)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        public static void LoadMissionData(int index, bool mif)
        {
            if (mif)
            {

            }
            else
            {
                var officialMission = DataManager.Instance.MissionData.officialMissions[index];
                Instance.missionName = officialMission.name;
                Instance.missionFullname = officialMission.fullname;
                Instance.missionBD1Path = SafePath.Combine(Application.streamingAssetsPath, officialMission.bd1Path);
                Instance.missionPD1Path = SafePath.Combine(Application.streamingAssetsPath, officialMission.pd1Path);
                Instance.adjustCollision = officialMission.adjustCollision;
                Instance.darkScreen = officialMission.darkScreen;

                var txtPath = SafePath.Combine(Application.streamingAssetsPath, officialMission.txtPath);
                if (File.Exists(txtPath))
                {
                    var txt = File.ReadAllLines(txtPath, EncodingHelper.GetEncoding());
                    if (txt.Length > 2)
                    {
                        var briefingPath = Path.Combine(Application.streamingAssetsPath, "data/briefing");
                        if (!string.IsNullOrEmpty(txt[0]) && txt[0] != "!")
                        {
                            Instance.missionImage0 = SafePath.Combine(briefingPath, txt[0]) + ".bmp";
                        }
                        if (!string.IsNullOrEmpty(txt[1]) && txt[1] != "!")
                        {
                            Instance.missionImage1 = SafePath.Combine(briefingPath, txt[1]) + ".bmp";
                        }
                        if (int.TryParse(txt[2], out int skyIndex))
                        {
                            Instance.skyIndex = skyIndex;
                        }
                        Instance.missionBriefing = string.Join("\n", txt, 3, txt.Length - 3);
                    }
                }
            }
        }

        public static void UnloadMissionData()
        {
            Instance.missionName = string.Empty;
            Instance.missionFullname = string.Empty;
            Instance.missionBD1Path = string.Empty;
            Instance.missionPD1Path = string.Empty;
            Instance.missionAddonObjectPath = string.Empty;
            Instance.missionImage0 = string.Empty;
            Instance.missionImage1 = string.Empty;
            Instance.skyIndex = 0;
            Instance.missionBriefing = string.Empty;
            Instance.adjustCollision = false;
            Instance.darkScreen = false;
        }
    }
}
