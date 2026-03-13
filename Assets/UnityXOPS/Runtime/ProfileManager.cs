using UnityEngine;
using JJLUtility;

namespace UnityXOPS
{
    public class ProfileManager : SingletonBehavior<ProfileManager>
    {
        [SerializeField]
        private Profile currentProfile;
        public static Profile CurrentProfile => Instance.currentProfile;
    }
}