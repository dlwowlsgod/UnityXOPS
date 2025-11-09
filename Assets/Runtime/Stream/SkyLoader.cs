using UnityEngine;
using System.Linq;

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
            var skyParam = ParameterManager.Instance.SkyParameter;
            var scale = new[] {
                skyParam.frontTextureScaleAndOffset.scale,
                skyParam.backTextureScaleAndOffset.scale,
                skyParam.leftTextureScaleAndOffset.scale,
                skyParam.rightTextureScaleAndOffset.scale,
                skyParam.upTextureScaleAndOffset.scale,
                skyParam.downTextureScaleAndOffset.scale
            }.Select(scale => new Vector2(scale.x, scale.y)).ToArray();
            var offset = new[]
            {
                skyParam.frontTextureScaleAndOffset.offset,
                skyParam.backTextureScaleAndOffset.offset,
                skyParam.leftTextureScaleAndOffset.offset,
                skyParam.rightTextureScaleAndOffset.offset,
                skyParam.upTextureScaleAndOffset.offset,
                skyParam.downTextureScaleAndOffset.offset
            }.Select(offset => new Vector2(offset.x, offset.y)).ToArray();

            for (int i = 0; i < 6; i++)
            {
                _defaultSky.SetTextureScale(SkyboxPropertyID[i], scale[i]);
                _defaultSky.SetTextureOffset(SkyboxPropertyID[i], offset[i]);
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