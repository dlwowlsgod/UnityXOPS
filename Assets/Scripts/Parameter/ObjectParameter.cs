using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 Object 정보를 담는 Parameter입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 에디터 상에서 생성하여 <see cref="ParameterManager">ParameterManager</see>에 추가할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "ObjectParameter", menuName = "UnityXOPS/ObjectParameter")]
    public class ObjectParameter : ScriptableObject
    {
        [Tooltip("Object의 이름입니다.")]
        public string finalName;
        [Tooltip("Object의 메시 경로입니다.")]
        public string meshPath;
        [Tooltip("Object의 텍스쳐 경로입니다.")]
        public string texturePath;
        [Tooltip("Object의 사운드 경로입니다.")]
        public string soundPath;
        [Tooltip("Object의 충돌 타입입니다.")]
        public ColliderType colliderType;
        [Tooltip("Object의 충돌 판정 크기입니다.")]
        public float colliderSize;
        [Tooltip("Object의 통과 여부입니다.")]
        public bool through;
        [Tooltip("Object의 체력입니다.")]
        public int hp;
        [Tooltip("Object 파괴 시 튀는 정도입니다.")]
        public float bounce;
    }

    /// <summary>
    /// <see cref="ObjectParameter">ObjectParameter</see>를 직렬화하기 위한 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class ObjectParameterWrapper : IParameterData
    {
        public string finalName;
        public string meshPath;
        public string texturePath;
        public string soundPath;
        public ColliderType colliderType;
        public float colliderSize;
        public bool through;
        public int hp;
        public float bounce;
        
        public string FinalName => finalName;
    }

    /// <summary>
    /// <see cref="ObjectParameterWrapper">ObjectParameterWrapper</see>를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class ObjectParameterList : IParameterList<ObjectParameterWrapper>
    {
        public List<ObjectParameterWrapper> items;
        public List<ObjectParameterWrapper> Items => items;
    }

    public enum ColliderType
    {
        Sphere,
        Box,
        Capsule,
        Mesh
    }
}