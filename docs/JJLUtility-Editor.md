# JJLUtility — Editor

namespace: `JJLUtilityEditor`, `JJLUtilityEditor.IO`

---

## Debug

| 파일 | 클래스 | 역할 |
|---|---|---|
| `Editor/Debug/DebuggerEditor.cs` | `Debugger` (partial, static) | `[OnOpenAsset]` 콜백. Console 로그 항목 더블클릭 시 `Debugger.cs` 자체를 건너뛰고 실제 호출 지점으로 이동. Unity 내부 `ConsoleWindow` 리플렉션 사용. |

---

## IO / Image

| 파일 | 클래스 | 역할 |
|---|---|---|
| `Editor/IO/ImageLoaderEditor.cs` | `ImageLoaderEditor` | `ImageLoader`의 커스텀 Inspector. 텍스처 캐시 리스트를 읽기 쉬운 형태로 표시. |

---

## IO / Mesh

| 파일 | 클래스 | 역할 |
|---|---|---|
| `Editor/IO/MeshLoaderEditor.cs` | `MeshLoaderEditor` | `MeshLoader`의 커스텀 Inspector. 메시 캐시 리스트를 읽기 쉬운 형태로 표시. |
