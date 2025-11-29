using UnityEngine;
using System.IO;
using System.Collections.Generic;
namespace UnityXOPS
{
    /// <summary>
    /// A static class responsible for loading and managing profile data from a configuration file.
    /// </summary>
    public static class ProfileLoader
    {
        private const string ProfilePath = "UnityXOPSProfile.ini";

        private static readonly List<string> ProfileData = new()
        {
            "[Common]",
            "; Select game language.",
            "; Values : en, kr, jp, cn_s, cn_t",
            "; If not contained value, game use \"en\" font.",
            "Language = kr",
            "",
            "[Stream]",
            "; Use 2byte header for texture counts in block data.",
            "UseBlockDataTextureCountHeader = false",
            "",
            "; Use 4byte for point parameters.",
            "UseExtendedPointParameter = false",
            "",
            "; Use another feature of mif addon object txt file.",
            "UseExtendedMIFAddonTXT = false",
            "",
            "; Assimp can't load properly if some of x file tokens are broken.",
            "; if true, add some spaces between tokens then can assimp read correctly.",
            "; choose false if you predict your x models can read perfectly.",
            "FixXFileToken = true",
            "",
            "; Developer implemented failed (sorry ^^) perfect dds loader using FreeImage library",
            "; So char.dds can't load properly",
            "; If true, will load char.dds integrated in UnityXOPS.exe",
            "; use false if you use another char.dds or converted modern dds format",
            "UseInternalCharDDS = true"
        };

        private static readonly Dictionary<string, Dictionary<string, string>> Profile = new();
        
        public static void Initialize()
        {
            var profilePath = Path.Combine(Application.streamingAssetsPath, ProfilePath);

            if (!File.Exists(profilePath))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Profile file {profilePath} not found.");
#endif
                File.WriteAllLines(profilePath, ProfileData);
                return;
            }
            
            var profileFile = File.ReadAllLines(profilePath);

            string currentSection = null;
            foreach (var rawLine in profileFile)
            {
                var line = rawLine.Trim();

                // Check profile comments
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("/") || line.StartsWith(";"))
                {
                    continue;
                }

                // Parse section
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    if (!Profile.ContainsKey(currentSection))
                    {
                        Profile[currentSection] = new Dictionary<string, string>();
                    }
                }
                // Parse key-value pairs
                else if (currentSection != null)
                {
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        Profile[currentSection][key] = value;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the value associated with the specified section and key in the profile.
        /// If no value is found, the provided default value is returned.
        /// </summary>
        /// <param name="section">The section in the profile to search for the key.</param>
        /// <param name="key">The key whose associated value is to be retrieved.</param>
        /// <param name="defaultValue">The default value to return if the key is not found in the profile.</param>
        /// <returns>The value associated with the specified key in the specified section, or the default value if the key is not found.</returns>
        public static string GetProfileValue(string section, string key, string defaultValue)
        {
            if (Profile.TryGetValue(section, out var sectionValues))
            {
                return sectionValues.GetValueOrDefault(key, defaultValue);
            }
            return defaultValue;
        }
    }
}
