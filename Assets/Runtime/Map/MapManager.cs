using UnityEngine;
using System.Collections.Generic;

namespace UnityXOPS
{
    public class MapManager : Singleton<MapManager>
    {
        public string mapName;
        public string mapFullName;
        public Texture2D briefingImage0;
        public Texture2D briefingImage1;
        public string briefingText;
        
        public string bd1Name;
        [SerializeField]
        private Material[] textures;
        public int blockCount;

        public string pd1Name;
        public int pointCount;
        public int activeHumanCount;
        public int deadHumanCount;
        public int equippedWeaponCount;
        public int droppedWeaponCount;
        public int activeObjectCount;
        public int destroyedObjectCount;
        public string[] messages;

        [SerializeField]
        private Human player;
        public Human Player => player;
        public int time;
        public int fired;
        public int hit;
        public int killed;
        public int headshot;
        
        private Transform _blockRoot;
        private Transform _pointRoot;
        private Transform _humanRoot;
        private Transform _weaponRoot;
        private Transform _objectRoot;

        private List<Human> _humanList;
        private int _playerHumanIndex;

        public Dictionary<int, int> HumanParameter { get; } = new();

        private float _timer;

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= 1)
            {
                _timer -= 1;
                time++;
            }
        }

        public void LoadMap(DemoData demo)
        {
            var bd1Path = SafeIO.Combine(Application.streamingAssetsPath, demo.bd1Path);
            var pd1Path = SafeIO.Combine(Application.streamingAssetsPath, demo.pd1Path);
            LoadBlock(bd1Path);
            LoadPoint(pd1Path);
            LoadSky(demo.skyIndex);
        }

        public void LoadMap(OfficialMissionParameterSO officialMission)
        {
            mapName = officialMission.name;
            mapFullName = officialMission.fullName;
            
            var txtPath = SafeIO.Combine(Application.streamingAssetsPath, officialMission.txtPath);
            var bd1Path = SafeIO.Combine(Application.streamingAssetsPath, officialMission.bd1Path);
            var pd1Path = SafeIO.Combine(Application.streamingAssetsPath, officialMission.pd1Path);
            
            LoadBriefing(txtPath, out var skyIndex);
            LoadBlock(bd1Path);
            LoadPoint(pd1Path);
            LoadSky(skyIndex);

            ForceSetPlayer();
        }

        public void LoadMap(AddonMissionParameterSO addonMission)
        {
            mapName = addonMission.name;
            mapFullName = addonMission.fullName;
            briefingText = addonMission.description;

            var image0Path = SafeIO.Combine(Application.streamingAssetsPath, addonMission.briefing0Path);
            var image1Path = SafeIO.Combine(Application.streamingAssetsPath, addonMission.briefing1Path);
            
            briefingImage0 = ImageLoader.LoadImage(image0Path);
            briefingImage1 = ImageLoader.LoadImage(image1Path);
            
            var bd1Path = SafeIO.Combine(Application.streamingAssetsPath, addonMission.bd1Path);
            var pd1Path = SafeIO.Combine(Application.streamingAssetsPath, addonMission.pd1Path);
            
            LoadBlock(bd1Path);
            LoadPoint(pd1Path);
            LoadSky(addonMission.skyIndex);
            
            //todo: load addon objects
            
            ForceSetPlayer();
        }

        public void ClearMap()
        {
            mapName = null;
            mapFullName = null;
            briefingText = null;
            briefingImage0 = null;
            briefingImage1 = null;
            bd1Name = null;
            textures = null;
            blockCount = 0;
            pd1Name = null;
            pointCount = 0;
            activeHumanCount = 0;
            deadHumanCount = 0;
            equippedWeaponCount = 0;
            droppedWeaponCount = 0;
            activeObjectCount = 0;
            destroyedObjectCount = 0;
            messages = null;
            player = null;
            time = 0;
            fired = 0;
            hit = 0;
            killed = 0;
            headshot = 0;
            
            HumanParameter.Clear();
        }

        public Human PreviousHuman()
        {
            _playerHumanIndex--;
            if (_playerHumanIndex < 0)
            {
                _playerHumanIndex = _humanRoot.childCount - 1;
            }
            
            //player = _humanRoot.GetChild(_playerHumanIndex).GetComponent<Human>();
            player = _humanList[_playerHumanIndex];
            return player;
        }

        public Human NextHuman()
        {
            _playerHumanIndex++;
            if (_playerHumanIndex >= _humanRoot.childCount)
            {
                _playerHumanIndex = 0;
            }
            
            //player = _humanRoot.GetChild(_playerHumanIndex).GetComponent<Human>();
            player = _humanList[_playerHumanIndex];
            return player;
        }
        
        private void LoadBriefing(string txtPath, out int skyIndex)
        {
            if (txtPath != null)
            {
                var fullPath = SafeIO.Combine(Application.streamingAssetsPath, txtPath);
                var txt = EncodingHelper.ReadAllLinesWithEncoding(fullPath);
                
                var usePath = ProfileLoader.GetProfileValue("Stream", "UseBriefingFolderForMissionTxt", "false") == "true";
                if (usePath)
                {
                    var image0Path = SafeIO.Combine(Application.streamingAssetsPath, txt[0]);
                    var image1Path = SafeIO.Combine(Application.streamingAssetsPath, txt[1]);
                    
                    briefingImage0 = ImageLoader.LoadImage(image0Path);
                    briefingImage1 = ImageLoader.LoadImage(image1Path);
                    briefingText = string.Join("\n", txt[3..]);
                    skyIndex = int.TryParse(txt[2], out var index) ? index : 0;
                }
                else
                {
                    var image0Path =
                        SafeIO.Combine(Application.streamingAssetsPath, "data/briefing/" + txt[0] + ".bmp");
                    var image1Path =
                        SafeIO.Combine(Application.streamingAssetsPath, "data/briefing/" + txt[1] + ".bmp");
                    
                    briefingImage0 = ImageLoader.LoadImage(image0Path);
                    briefingImage1 = ImageLoader.LoadImage(image1Path);
                    briefingText = string.Join("\n", txt[3..]);
                    skyIndex = int.TryParse(txt[2], out var index) ? index : 0;
                }

                return;
            }

            skyIndex = 0;
        }

        private void LoadBlock(string bd1Path)
        {
            bd1Name = System.IO.Path.GetFileName(bd1Path);
            
            if (_blockRoot == null)
            {
                _blockRoot = new GameObject("BlockRoot").transform;
                _blockRoot.parent = transform;
            }
            
            var bd1 = BD1Loader.LoadBD1(bd1Path);
            textures = bd1.textures;
            blockCount = bd1.blocks.Length;

            for (int i = 0; i < bd1.blocks.Length; i++)
            {
                var raw = bd1.rawBlockData[i];
                var blockObj = new GameObject($"block_{i}");
                blockObj.layer = LayerMask.NameToLayer("Block");
                blockObj.transform.parent = _blockRoot;
                blockObj.transform.localPosition = raw.position;
                blockObj.transform.localRotation = Quaternion.identity;
                blockObj.transform.localScale = Vector3.one;
                
                var meshFilter = blockObj.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = bd1.blocks[i];
                
                var meshRenderer = blockObj.AddComponent<MeshRenderer>();
                var subMeshTextures = new Material[raw.subMeshTextureIndices.Length];
                for (int j = 0; j < raw.subMeshTextureIndices.Length; j++)
                {
                    subMeshTextures[j] = textures[raw.subMeshTextureIndices[j]];
                }
                meshRenderer.sharedMaterials = subMeshTextures;
                
                if (raw.collider)
                {
                    var meshCollider = blockObj.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = bd1.blocks[i];
                }
            }
        }
        
        private void LoadPoint(string pd1Path)
        {
            if (_pointRoot == null)
            {
                _pointRoot = new GameObject("PointRoot").transform;
                _pointRoot.parent = transform;
            }

            if (_humanRoot == null)
            {
                _humanRoot = new GameObject("HumanRoot").transform;
                _humanRoot.parent = transform;
            }

            if (_weaponRoot == null)
            {
                _weaponRoot = new GameObject("WeaponRoot").transform;
                _weaponRoot.parent = transform;
            }

            if (_objectRoot == null)
            {
                _objectRoot = new GameObject("ObjectRoot").transform;
                _objectRoot.parent = transform;
            }
            
            pd1Name = System.IO.Path.GetFileName(pd1Path);
            var pd1 = PD1Loader.LoadPD1(pd1Path);
            pointCount = pd1.rawPointData.Length;

            //collect parameter info
            for (int i = 0; i < pd1.rawPointData.Length; i++)
            {
                var raw = pd1.rawPointData[i];
                if (raw.type == PointType.Parameter)
                {
                    HumanParameter[raw.param2] = raw.param0;
                }
            }
            
            //create human
            var humanSO = ParameterManager.Instance.HumanParameterSO;
            var dataSOs = humanSO.humanDataParameterSOs;
            var humanPrefab = Resources.Load<GameObject>("Prefab/Human");
            _humanList = new List<Human>();
            for (int i = 0; i < pd1.rawPointData.Length; i++)
            {
                var raw = pd1.rawPointData[i];
                if (raw.type is PointType.Human or PointType.HumanNoPrimaryWeapon)
                {
                    var humanObj = Instantiate(humanPrefab, raw.position, raw.rotation, _humanRoot);
                    var human = humanObj.GetComponent<Human>();
                    human.InitializeHuman(raw.param0, raw.param1, raw.param2);
                    _humanList.Add(human);
                    activeHumanCount++;
                    
                    if (raw.param2 == 0)
                    {
                        player = human;
                    }
                }
            }
        }

        private void LoadSky(int skyIndex)
        {
            var skyPaths = ParameterManager.Instance.SkyParameterSO.skyTextures;
            if (skyIndex < 0 || skyIndex >= skyPaths.Length)
            {
                return;
            }
            
            var skybox = SkyLoader.LoadSky(skyIndex);
            RenderSettings.skybox = skybox;
        }

        private void ForceSetPlayer()
        {
            if (player == null)
            {
                player = _humanRoot.GetChild(_humanRoot.childCount - 1).GetComponent<Human>();
            }

            var i = 0;
            foreach (Transform child in _humanRoot)
            {
                if (child.GetComponent<Human>() == player)
                {
                    _playerHumanIndex = i;
                    return;
                }
                i++;
            }
        }
    }
}
