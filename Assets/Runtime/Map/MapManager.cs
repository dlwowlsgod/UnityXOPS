using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnityXOPS
{
    public class MapManager : Singleton<MapManager>
    {
        [SerializeField]
        private BriefingData briefing;
        
        [SerializeField]
        private Material[] blockTextures;

        [SerializeField]
        private string[] messages;

        [SerializeField]
        private Human player;
        
        [SerializeField]
        private Transform blockRoot;

        [SerializeField]
        private Transform humanRoot;
        
        private readonly Dictionary<int, int> _humanParameters = new();
        private readonly Dictionary<int, int> _paths = new();
        private readonly Dictionary<int, Branch> _randomPaths = new();

        private void Start()
        {
            var missionSO = ParameterManager.Instance.MissionParameterSO.officialMissionParameterSOs[0];
            CreateMap(missionSO);
        }

        public void CreateMap(OfficialMissionParameterSO missionSO)
        {
            var bd1Path = SafeIO.Combine(Application.streamingAssetsPath, missionSO.bd1Path);
            var bd1 = BD1Loader.LoadBD1(bd1Path);
            var pd1Path = SafeIO.Combine(Application.streamingAssetsPath, missionSO.pd1Path);
            var pd1 = PD1Loader.LoadPD1(pd1Path);

            briefing = new BriefingData();
            var txt = SafeIO.Combine(Application.streamingAssetsPath, missionSO.txtPath);
            var briefingPath = Path.Combine(Application.streamingAssetsPath, "data/briefing");
            var txtLine = EncodingHelper.ReadAllLinesWithEncoding(txt);
            if (!string.IsNullOrWhiteSpace(txtLine[0]) && txtLine[0] != "!")
            {
                var image0Path = SafeIO.Combine(briefingPath, txtLine[0] + ".bmp");
                briefing.image0 = ImageLoader.LoadImage(image0Path);
            }

            if (!string.IsNullOrWhiteSpace(txtLine[1]) && txtLine[1] != "!")
            {
                var image1Path = SafeIO.Combine(briefingPath, txtLine[1] + ".bmp");
                briefing.image1 = ImageLoader.LoadImage(image1Path);
            }

            briefing.title = missionSO.fullName;
            briefing.text = string.Join("\n", txtLine[3..]);

            var skyIndex = int.TryParse(txtLine[2], out var parse) ? parse : 0;
            
            CreateBlock(bd1);
            CreatePoint(pd1);
            CreateSky(skyIndex);
        }

        public void CreateMap(AddonMissionParameterSO missionSO)
        {
            
        }

        public void CreateMap(DemoData demo)
        {
            var bd1Path = SafeIO.Combine(Application.streamingAssetsPath, demo.bd1Path);
            var bd1 = BD1Loader.LoadBD1(bd1Path);
            var pd1Path = SafeIO.Combine(Application.streamingAssetsPath, demo.pd1Path);
            var pd1 = PD1Loader.LoadPD1(pd1Path);
            
            CreateBlock(bd1);
            CreatePoint(pd1);
            CreateSky(demo.skyIndex);
        }
        
        private void CreateBlock(BlockData data)
        {
            blockTextures = data.textures;
            
            for (int i = 0; i < data.blocks.Length; i++)
            {
                var block = new GameObject($"block_{i}");
                block.transform.parent = blockRoot;
                block.transform.localPosition = data.rawBlockData[i].position;
                block.transform.localRotation = Quaternion.identity;
                block.transform.localScale = Vector3.one;
                
                block.AddComponent<MeshFilter>().sharedMesh = data.blocks[i];
                var materials = new Material[data.rawBlockData[i].subMeshTextureIndices.Length];
                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j] = blockTextures[data.rawBlockData[i].subMeshTextureIndices[j]];
                }
                block.AddComponent<MeshRenderer>().sharedMaterials = materials;
                //block.AddComponent<MeshRenderer>().sharedMaterials = data.rawBlockData[i].subMeshMaterials;
                if (data.rawBlockData[i].collider)
                {
                    block.AddComponent<MeshCollider>().sharedMesh = data.blocks[i];
                }
            }
        }

        private void CreatePoint(PointData data)
        {
            messages = data.msgData;

            foreach (var raw in data.rawPointData)
            {
                var type = raw.type;
                switch (type)
                {
                    case PointType.Path:
                        _paths[raw.param2] = raw.param1;
                        break;
                    case PointType.RandomPath:
                        _randomPaths[raw.param2] = new Branch(raw.param0, raw.param1);
                        break;
                    case PointType.Parameter:
                        _humanParameters[raw.param2] = raw.param0;
                        break;
                    case PointType.EventSuccess:
                    case PointType.EventFailure:
                    case PointType.EventIfHumanKilled:
                    case PointType.EventIfHumanArrived:
                    case PointType.EventInvokePathWait:
                    case PointType.EventIfObjectDestroyed:
                    case PointType.EventIfHumanArrivedWithCase:
                    case PointType.EventTimer:
                    case PointType.EventMessage:
                    case PointType.EventChangeTeamNumberTo0:
                        break;
                }
            }
            
            //human
            var humans = data.rawPointData
                .Where(p => p.type is PointType.Human or PointType.HumanNoPrimaryWeapon).ToArray();
            for (int i = 0; i < humans.Length; i++)
            {
                //basic
                var humanObj = Instantiate(Resources.Load<GameObject>("Prefab/Human"), humanRoot);
                humanObj.name = $"Human_{i}";
                humanObj.transform.localPosition = humans[i].position;
                humanObj.transform.localRotation = humans[i].rotation;
                humanObj.transform.localScale = Vector3.one;
                
                //create human
                var human = humanObj.GetComponent<Human>();
                var paramLength = ParameterManager.Instance.HumanParameterSO.humanDataParameterSOs.Length;
                var paramIndex = _humanParameters.GetValueOrDefault(humans[i].param0, 0);
                if (paramIndex < 0 || paramIndex >= paramLength)
                {
                    paramIndex = 0;
                }
                var humanData = ParameterManager.Instance.HumanParameterSO.humanDataParameterSOs[paramIndex];
                human.CreateHuman(humanData);
                
                
                if (humans[i].param2 == 0)
                {
                    player = human;
                }
            }
            if (player == null)
            {
                // set last created human as player
                var humanObj = humanRoot.GetChild(humanRoot.childCount - 1).gameObject;
                player = humanObj.GetComponent<Human>();
            }
        }

        private void CreateSky(int skyIndex)
        {
            var skybox = SkyLoader.LoadSky(skyIndex);
            RenderSettings.skybox = skybox;
        }
    }
}
