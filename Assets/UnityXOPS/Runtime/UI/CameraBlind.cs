using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// OpenXOPS gamemain.cpp:2973-3003 포팅.
    /// 카메라 near-clip plane 4개 모서리 중점을 사분면별로 1:1 샘플링해,
    /// 해당 중점이 블록 내부에 있을 때만 그 방향의 RawImage를 활성화한다.
    /// </summary>
    public class CameraBlind : MonoBehaviour
    {
        // near-clip 모서리 중점을 바깥쪽으로 살짝 확장해 경계에 아슬하게 닿는 케이스를 잡는다.
        private const float k_edgeInflate = 0f;

        [SerializeField] private RawImage topImage;
        [SerializeField] private RawImage bottomImage;
        [SerializeField] private RawImage leftImage;
        [SerializeField] private RawImage rightImage;

        [SerializeField] private Camera playerCamera;
        [SerializeField] private bool   enableBlind = true;

        private void Update()
        {
            Human player = MapLoader.Player;
            if (!enableBlind || playerCamera == null || player == null || !player.Alive)
            {
                DisableAll();
                return;
            }

            var colliders = MapLoader.BlockColliders;
            if (colliders == null)
            {
                DisableAll();
                return;
            }

            Transform t         = playerCamera.transform;
            Vector3   cameraPos = t.position;
            Vector3   forward   = t.forward;
            Vector3   right     = t.right;
            Vector3   up        = t.up;

            float near  = playerCamera.nearClipPlane;
            float halfH = near * Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) + k_edgeInflate;
            float halfW = halfH * playerCamera.aspect;

            Vector3 nearCenter = cameraPos + forward * near;
            Vector3 topMid     = nearCenter + up    *  halfH;
            Vector3 bottomMid  = nearCenter + up    * -halfH;
            Vector3 leftMid    = nearCenter + right * -halfW;
            Vector3 rightMid   = nearCenter + right *  halfW;

            SetActive(topImage,    IsInsideAnyBlock(topMid,    colliders));
            SetActive(bottomImage, IsInsideAnyBlock(bottomMid, colliders));
            SetActive(leftImage,   IsInsideAnyBlock(leftMid,   colliders));
            SetActive(rightImage,  IsInsideAnyBlock(rightMid,  colliders));
        }

        private void DisableAll()
        {
            SetActive(topImage,    false);
            SetActive(bottomImage, false);
            SetActive(leftImage,   false);
            SetActive(rightImage,  false);
        }

        private static bool IsInsideAnyBlock(Vector3 point, IReadOnlyList<Block> colliders)
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                if (colliders[i].Contains(point)) return true;
            }
            return false;
        }

        private static void SetActive(RawImage image, bool active)
        {
            if (image == null) return;
            if (image.gameObject.activeSelf != active)
                image.gameObject.SetActive(active);
        }
    }
}
