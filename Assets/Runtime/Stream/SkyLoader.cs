using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace UnityXOPS
{
    public static class SkyLoader
    {
        private static Material _defaultSky;
        private static readonly int FrontTex = Shader.PropertyToID("_FrontTex");
        private static readonly int BackTex = Shader.PropertyToID("_BackTex");
        private static readonly int LeftTex = Shader.PropertyToID("_LeftTex");
        private static readonly int RightTex = Shader.PropertyToID("_RightTex");
        private static readonly int UpTex = Shader.PropertyToID("_UpTex");
        private static readonly int DownTex = Shader.PropertyToID("_DownTex");
        private static readonly int Tint = Shader.PropertyToID("_Tint");
        
        public static void Initialize()
        {
            _defaultSky = Resources.Load<Material>("DefaultSky");
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
            
            skybox.SetColor(Tint, new Color(0.5010f, 0.5010f, 0.5010f, 0.5010f));
            
            //top
            skybox.SetTexture(UpTex, image);
            //bottom
            skybox.SetTexture(DownTex, image);
            //front
            skybox.SetTexture(FrontTex, image);
            //back
            skybox.SetTexture(BackTex, image);
            //left
            skybox.SetTexture(LeftTex, image);
            //right
            skybox.SetTexture(RightTex, image);

            
            return skybox;
        }
    }
}
