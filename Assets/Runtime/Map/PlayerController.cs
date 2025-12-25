using UnityEngine;

namespace UnityXOPS
{
    public class PlayerController : Singleton<PlayerController>
    {
        private void Update()
        {
            if (MapManager.Instance.Player == null)
            {
                return;
            }
            
            
        }
    }
}
