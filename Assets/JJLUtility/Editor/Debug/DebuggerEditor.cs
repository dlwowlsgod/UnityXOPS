using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using System;
using UnityEngine;

namespace JJLUtilityEditor
{
    public static partial class Debugger
    {
        [UnityEditor.Callbacks.OnOpenAsset()]
        private static bool OnOpenLog(int instanceID)
        {
            // OpenAsset이 Debug 콘솔창인지 확인
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

            // EditorWindow 어셈블리에서 internal/private 클래스인 ConsoleWindow 타입 가져오기
            var consoleWindowType = editorWindowAssembly.GetType("UnityEditor.ConsoleWindow");
            if (consoleWindowType == null)
            {
                return false;
            }

            // typeof(ConsoleWindow)를 사용할 수 없는 private static 필드인 ms_ConsoleWindow 가져오기
            var consoleWindowField = consoleWindowType.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
            if (consoleWindowField == null)
            {
                return false;
            }

            // ms_ConsoleWindow 필드에서 실제 consoleWindow 인스턴스 읽기
            var consoleWindowInstance = consoleWindowField.GetValue(null);
            if (consoleWindowInstance == null) return false;

            if (consoleWindowInstance != (object)EditorWindow.focusedWindow)
            {
                return false;
            }

            // ConsoleWindow 인스턴스에서 private 필드인 m_ActiveText 가져오기
            var activeTextField = consoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
            if (activeTextField == null)
            {
                return false;
            }

            // m_ActiveText 필드에서 실제 activeText 값 읽기
            string activeTextValue = activeTextField.GetValue(consoleWindowInstance).ToString();
            if (string.IsNullOrEmpty(activeTextValue)) return false;

            // stackTrace를 정규식으로 파싱하여 파일 경로를 추적
            var match = Regex.Match(activeTextValue, @"\(at (.+)\)");
            if (match.Success)
            {
                // 첫 번째 매칭은 생략 (첫 번쨰 매칭은 Debugger.cs를 가리킴)
                match.NextMatch();
            }

            if (match.Success)
            {
                string scriptPath = match.Groups[1].Value;
                var split = scriptPath.Split(':');
                string filePath = split[0];
                int lineNumber = Convert.ToInt32(split[1]);

                string dataPath = Application.dataPath[..Application.dataPath.LastIndexOf("Assets")];
                InternalEditorUtility.OpenFileAtLineExternal(dataPath + filePath, lineNumber);

                // true를 반환해야 UnityEditor의 OnOpenAsset 콜백이 처리됨
                return true;
            }

            return false;
        }
    }
}