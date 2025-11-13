using UnityEngine;

namespace UnityXOPS
{
    public static class SkyLoader
    {
        private static Material _defaultSky;
        private static readonly int[] SkyboxPropertyID = {
            Shader.PropertyToID("_FrontTex"),
            Shader.PropertyToID("_BackTex"), 
            Shader.PropertyToID("_LeftTex"),
            Shader.PropertyToID("_RightTex"),
            Shader.PropertyToID("_UpTex"),
            Shader.PropertyToID("_DownTex")
        };
        private static readonly int Tint = Shader.PropertyToID("_Tint");
        
        public static void Initialize()
        {
            _defaultSky = Resources.Load<Material>("DefaultSky");
            _defaultSky = Object.Instantiate(_defaultSky);

            var skyboxSO = ParameterManager.Instance.SkyParameterSO;

            var scaleAndOffset = new[]
            {
                skyboxSO.frontScaleAndOffset,
                skyboxSO.backScaleAndOffset,
                skyboxSO.leftScaleAndOffset,
                skyboxSO.rightScaleAndOffset,
                skyboxSO.upScaleAndOffset,
                skyboxSO.downScaleAndOffset
            };
            
            for (int i = 0; i < 6; i++)
            {
                _defaultSky.SetTextureScale(SkyboxPropertyID[i], scaleAndOffset[i].scale);
                _defaultSky.SetTextureOffset(SkyboxPropertyID[i], scaleAndOffset[i].offset);
            }
            
            RenderSettings.skybox = _defaultSky;
        }

        public static Material LoadSky(string filePath)
        {
            var image = ImageLoader.LoadImage(filePath);
            if (image == null)
            {
                return _defaultSky;
            }

            var name = image.name;
            var skybox = Object.Instantiate(_defaultSky);
            
            skybox.SetColor(Tint, new Color(0.5f, 0.5f, 0.5f, 0.5f));

            for (int i = 0; i < 6; i++)
            {
                skybox.SetTexture(SkyboxPropertyID[i], image);
            }
            
            return skybox;
        }
    }
}