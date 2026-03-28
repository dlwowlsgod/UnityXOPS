using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using System;
using UnityEngine;

namespace JJLUtilityEditor
{
    /// <summary>
    /// 에디터에서 로그 클릭 시 실제 호출 위치로 파일을 여는 기능을 제공하는 Debugger 에디터 확장.
    /// </summary>
    public static partial class Debugger
    {
        /// <summary>
        /// 에디터에서 Debug 오브젝트를 열 때 콘솔 스택 트레이스를 파싱해 실제 호출 소스 파일을 연다.
        /// </summary>
        [UnityEditor.Callbacks.OnOpenAsset()]
        private static bool OnOpenLog(int instanceID)
        {
            // 클릭된 오브젝트가 Debug인지 확인
            string name = EditorUtility.InstanceIDToObject(instanceID).name;
            if (!name.Equals("Debug"))
            {
                return false;
            }

            // EditorWindow 어셈블리 가져오기
            var editorWindowAssembly = Assembly.GetAssembly(typeof(EditorWindow));
            if (editorWindowAssembly == null)
            {
                return false;
            }

            // EditorWindow 어셈블리에서 internal 클래스인 ConsoleWindow 타입 가져오기
            var consoleWindowType = editorWindowAssembly.GetType("UnityEditor.ConsoleWindow");
            if (consoleWindowType == null)
            {
                return false;
            }

            // ConsoleWindow 타입에서 현재 열린 창을 가리키는 private static 필드 ms_ConsoleWindow 가져오기
            var consoleWindowField = consoleWindowType.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
            if (consoleWindowField == null)
            {
                return false;
            }

            // ms_ConsoleWindow 필드에서 ConsoleWindow 인스턴스 읽기
            var consoleWindowInstance = consoleWindowField.GetValue(null);
            if (consoleWindowInstance == null) return false;

            if (consoleWindowInstance != (object)EditorWindow.focusedWindow)
            {
                return false;
            }

            // ConsoleWindow 인스턴스에서 선택된 로그 텍스트를 담는 private 필드 m_ActiveText 가져오기
            var activeTextField = consoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
            if (activeTextField == null)
            {
                return false;
            }

            // m_ActiveText 필드에서 현재 선택된 로그의 스택 트레이스 텍스트 읽기
            string activeTextValue = activeTextField.GetValue(consoleWindowInstance).ToString();
            if (string.IsNullOrEmpty(activeTextValue)) return false;

            // 스택 트레이스를 정규식으로 파싱하여 "(at 파일경로:줄번호)" 패턴 추출
            var match = Regex.Match(activeTextValue, @"\(at (.+)\)");
            if (match.Success)
            {
                // 첫 번째 매칭 건너뜀 (Debugger.cs 자신을 가리키므로)
                match = match.NextMatch();
            }

            if (match.Success)
            {
                string scriptPath = match.Groups[1].Value;
                var split = scriptPath.Split(':');
                string filePath = split[0];
                int lineNumber = Convert.ToInt32(split[1]);

                string dataPath = Application.dataPath[..Application.dataPath.LastIndexOf("Assets")];
                InternalEditorUtility.OpenFileAtLineExternal(dataPath + filePath, lineNumber);

                // true 반환 시 UnityEditor가 OnOpenAsset 이벤트를 처리 완료로 간주
                return true;
            }

            return false;
        }
    }
}