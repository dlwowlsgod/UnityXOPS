using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 직렬화된 ScriptableObject 데이터를 담는 인터페이스입니다.
    /// </summary>
    /// <typeparam name="T"><see cref="IParameterData">IParameterData</see>를 상속받는 클래스</typeparam>
    public interface IParameterList<T> where T : IParameterData
    {
        List<T> Items { get; }
    }
}