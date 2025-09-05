namespace UnityXOPS
{
    /// <summary>
    /// ScriptableObject를 직렬화하기 위한 Wrapper 클래스의 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// FinalName은 파라미터의 이름을 덮어씌우는게 권장됩니다.
    /// </remarks>
    public interface IParameterData
    {
        string Name { get; }
    }
}