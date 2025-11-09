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
    public static class InitializeOnLoad
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void InitializeBeforeSplashScreen()
        {
            ProfileLoader.Initialize();
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeBeforeSceneLoad()
        {
            ModelLoader.Initialize();
            ImageLoader.Initialize();
            SoundLoader.Initialize();
            FontLoader.Initialize();
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeAfterSceneLoad()
        {
            ParameterManager.Initialize();
            
            SkyLoader.Initialize();
            BD1Loader.Initialize();
            PD1Loader.Initialize();
        }
    }
}
