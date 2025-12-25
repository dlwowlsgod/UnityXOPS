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
        private static bool _initializedBeforeSplashScreen;
        private static bool _initializedBeforeSceneLoad;
        private static bool _initializedAfterSceneLoad;
        
        public static bool Initialized => _initializedBeforeSplashScreen && _initializedBeforeSceneLoad && _initializedAfterSceneLoad;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void InitializeBeforeSplashScreen()
        {
            ProfileLoader.Initialize();
            Timestep.Initialize();

            _initializedBeforeSplashScreen = true;
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeBeforeSceneLoad()
        {
            ModelLoader.Initialize();
            ImageLoader.Initialize();
            SoundLoader.Initialize();
            FontLoader.Initialize();
            
            _initializedBeforeSceneLoad = true;
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeAfterSceneLoad()
        {
            ParameterManager.Initialize();
            
            MIFLoader.Initialize();
            SkyLoader.Initialize();
            BD1Loader.Initialize();
            PD1Loader.Initialize();
            
            _initializedAfterSceneLoad = true;
        }
    }
}
