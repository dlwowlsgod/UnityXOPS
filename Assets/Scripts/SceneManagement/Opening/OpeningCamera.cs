using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 유니티에서 오프닝 시퀀스에 사용되는 카메라를 나타냅니다.
    /// 시간에 따른 부드러운 전환으로 시네마틱 효과를 만듭니다. 
    /// </summary>
    public class OpeningCamera : MonoBehaviour
    {
        public List<OpeningCameraMove> position;
        public List<OpeningCameraMove> rotation;

        private Camera _camera;

        private void Start()
        {
            _camera = GetComponent<Camera>();

            //start time 기준으로 정렬 (혹시 몰라서)
            position = position?.OrderBy(p => p.startTime).ToList();
            rotation = rotation?.OrderBy(r => r.startTime).ToList();
        }

        private void LateUpdate()
        {
            if (StateMachine.Instance.CurrentState == GameState.OpeningUpdate)
            {
                var clock = Clock.Instance.Process;

                // 현재 시간에 맞는 위치값을 계산
                var currentPosition = GetCurrentValue(position, clock);
                if (currentPosition.HasValue)
                {
                    _camera.transform.position = currentPosition.Value;
                }

                // 현재 시간에 맞는 회전값을 계산
                var currentRotationEuler = GetCurrentValue(rotation, clock);
                if (currentRotationEuler.HasValue)
                {
                    _camera.transform.rotation = Quaternion.Euler(currentRotationEuler.Value);
                }
            }
        }

        /// <summary>
        /// 지정된 시간(clock)에 해당하는 Vector3 값을 이동 목록(moves)에서 계산하여 반환합니다.
        /// </summary>
        /// <param name="moves">startTime 순서로 정렬된 이동 데이터 리스트</param>
        /// <param name="clock">현재 진행 시간</param>
        /// <returns>계산된 Vector3 값. 유효한 값이 없으면 null을 반환</returns>
        private Vector3? GetCurrentValue(List<OpeningCameraMove> moves, float clock)
        {
            if (moves == null || moves.Count == 0)
            {
                return null;
            }

            // startTime과 endTime 사이의 Lerp 값을 계산합니다.
            foreach (var move in moves)
            {
                if (clock >= move.startTime && clock < move.endTime)
                {
                    var lerp = Mathf.InverseLerp(move.startTime, move.endTime, clock);
                    return Vector3.Lerp(move.from, move.to, lerp);
                }
            }

            //그 사이가 아닐 경우
            //모든 이동이 끝난 후
            var lastMove = moves[^1];
            if (clock >= lastMove.endTime)
            {
                return lastMove.to;
            }

            //이동구간 사이의 간격, 또는 첫 이동 시작전
            //(n의 endTime과 n+1의 startTime을 똑같이 하는걸 추천)
            for (int i = 0; i < moves.Count; i++)
            {
                if (clock < moves[i].startTime)
                {
                    return i == 0 ? moves[i].from : moves[i - 1].to;
                }
            }

            return null;
        }
    }

    [Serializable]
    public class OpeningCameraMove
    {
        public float startTime;
        public float endTime;
        public Vector3 from;
        public Vector3 to;
    }
}