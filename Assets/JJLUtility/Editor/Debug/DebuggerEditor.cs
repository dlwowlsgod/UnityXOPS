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
            // OpenAsset�� Debug �ܼ�â���� Ȯ��
            string name = EditorUtility.InstanceIDToObject(instanceID).name;
            if (!name.Equals("Debug"))
            {
                return false;
            }

            // EditorWindow ������� ��������
            var editorWindowAssembly = Assembly.GetAssembly(typeof(EditorWindow));
            if (editorWindowAssembly == null)
            {
                return false;
            }

            // EditorWindow ����������� internal/private Ŭ������ ConsoleWindow Ÿ�� ��������
            var consoleWindowType = editorWindowAssembly.GetType("UnityEditor.ConsoleWindow");
            if (consoleWindowType == null)
            {
                return false;
            }

            // typeof(ConsoleWindow)�� ����� �� ���� private static �ʵ��� ms_ConsoleWindow ��������
            var consoleWindowField = consoleWindowType.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
            if (consoleWindowField == null)
            {
                return false;
            }

            // ms_ConsoleWindow �ʵ忡�� ���� consoleWindow �ν��Ͻ� �б�
            var consoleWindowInstance = consoleWindowField.GetValue(null);
            if (consoleWindowInstance == null) return false;

            if (consoleWindowInstance != (object)EditorWindow.focusedWindow)
            {
                return false;
            }

            // ConsoleWindow �ν��Ͻ����� private �ʵ��� m_ActiveText ��������
            var activeTextField = consoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
            if (activeTextField == null)
            {
                return false;
            }

            // m_ActiveText �ʵ忡�� ���� activeText �� �б�
            string activeTextValue = activeTextField.GetValue(consoleWindowInstance).ToString();
            if (string.IsNullOrEmpty(activeTextValue)) return false;

            // stackTrace�� ���Խ����� �Ľ��Ͽ� ���� ��θ� ����
            var match = Regex.Match(activeTextValue, @"\(at (.+)\)");
            if (match.Success)
            {
                // ù ��° ��Ī�� ���� (ù ���� ��Ī�� Debugger.cs�� ����Ŵ)
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

                // true�� ��ȯ�ؾ� UnityEditor�� OnOpenAsset �ݹ��� ó����
                return true;
            }

            return false;
        }
    }
}