using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// A class that initializes necessary components or dependencies automatically when the application loads.
    /// </summary>
    /// <remarks>
    /// The <see cref="InitializeOnLoad"/> class utilizes the <c>RuntimeInitializeOnLoadMethod</c> attribute
    /// to execute initialization logic before the Unity splash screen is shown. It ensures that specific
    /// systems are prepared and ready for use as soon as the application starts.
    /// </remarks>
    public class InitializeOnLoad
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void Initialize()
        {
            ProfileLoader.Initialize();
            ModelLoader.Initialize();
            ImageLoader.Initialize();
        }
    }
}
