using UnityEngine;
using System.IO;

namespace UnityXOPS
{
    public static class MIFLoader
    {
        private static bool _mifExtension;

        public static void Initialize()
        {
            _mifExtension = ProfileLoader.GetProfileValue("Stream", "UseExtendedMIFAddonTXT", "false") == "true";
        }
        
        public static AddonMissionParameterSO LoadMIF(string path)
        {
            var mif = EncodingHelper.ReadAllLinesWithEncoding(path);
            
            var so = ScriptableObject.CreateInstance<AddonMissionParameterSO>();

            so.name = mif[0];
            so.fullName = mif[1];
            so.bd1Path = mif[2].Replace(".\\", "").Replace('\\', '/');
            so.pd1Path = mif[3].Replace(".\\", "").Replace('\\', '/');
            so.skyIndex = int.Parse(mif[4]);
            switch (int.Parse(mif[5]))
            {
                case 0:
                    so.adjustCollision = false;
                    so.darkScreen = false;
                    break;
                case 1:
                    so.adjustCollision = true;
                    so.darkScreen = false;
                    break;
                case 2:
                    so.adjustCollision = false;
                    so.darkScreen = true;
                    break;
                case 3:
                    so.adjustCollision = true;
                    so.darkScreen = true;
                    break;
            }

            if (_mifExtension)
            {
                var txtPath = SafeIO.Combine(Application.streamingAssetsPath, mif[6]);
                var txt = EncodingHelper.ReadAllLinesWithEncoding(txtPath);
                
                so.addonHumanPath = txt[0];
                so.addonWeaponPath = txt[1];
                so.addonObjectPath = txt[2];
            }
            else
            {
                so.addonObjectPath = mif[6].Replace(".\\", "").Replace('\\', '/').Replace("!", "");
            }
            
            so.briefing0Path = mif[7].Replace(".\\", "").Replace('\\', '/').Replace("!", "");
            so.briefing1Path = mif[8].Replace(".\\", "").Replace('\\', '/').Replace("!", "");
            so.description = string.Join("\n", mif[9..]);
            
            return so;
        }
    }
}