using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 게임 설정 전체를 담는 루트 데이터 클래스. 섹션 목록(General/Graphic/Input 등)과 입력 바인딩을 함께 직렬화한다.
    /// 스칼라 설정은 sections에 타입 태그로 담고, 구조가 다른 입력 바인딩은 별도 bindings 배열로 둔다.
    /// </summary>
    [Serializable]
    public class GameConfig
    {
        public List<ConfigSection> sections;
        public InputActionDefinition[] bindings;
    }

    /// <summary>
    /// 한 설정 섹션. 이름과 그 안의 설정 목록을 가진다. 모드가 섹션 자체를 추가할 수 있다.
    /// </summary>
    [Serializable]
    public class ConfigSection
    {
        public string name;
        public List<ConfigSetting> settings;
    }

    /// <summary>
    /// 단일 설정 값. 값은 문자열로 저장하고 type으로 파싱한다. type: "int" / "float" / "bool" / "string".
    /// min/max는 int/float에서 min &lt; max 일 때만 클램프 범위로 쓰이고, min &gt;= max 면 무제한으로 간주된다.
    /// </summary>
    [Serializable]
    public class ConfigSetting
    {
        public string name;
        public string type;
        public string value;
        public float min;
        public float max;
    }
}
