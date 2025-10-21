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

        private static Dictionary<string, Dictionary<string, string>> _profile;
        
        public static void Initialize()
        {
            _profile = new Dictionary<string, Dictionary<string, string>>();
            
            var profilePath = Path.Combine(Application.streamingAssetsPath, ProfilePath);

            if (!File.Exists(profilePath))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Profile file {profilePath} not found.");
#endif
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
                    if (!_profile.ContainsKey(currentSection))
                    {
                        _profile[currentSection] = new Dictionary<string, string>();
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
                        _profile[currentSection][key] = value;
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
            if (_profile.TryGetValue(section, out var sectionValues))
            {
                return sectionValues.GetValueOrDefault(key, defaultValue);
            }
            return defaultValue;
        }
    }
}
