using Codice.CM.SEIDInfo;
using UnityEngine;
using UnityEditor;

namespace UnityXOPS
{
    [CustomPropertyDrawer(typeof(VisualData))]
    public class VisualDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                var modelPathProperty = property.FindPropertyRelative("modelPath");
                EditorGUILayout.PropertyField(modelPathProperty, new GUIContent("Model"));
                if (GUILayout.Button("Load", GUILayout.Width(65)))
                {
                    var path = EditorUtility.OpenFilePanel("Select Model", Application.streamingAssetsPath, "x");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (path.StartsWith(Application.streamingAssetsPath))
                        {
                            path = path.Replace(Application.streamingAssetsPath, "").TrimStart('\\').TrimStart('/');
                        }
                        else
                        {
                            path = "";
                        }
                    
                        property.FindPropertyRelative("modelPath").stringValue = path;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.PropertyField(property.FindPropertyRelative("baseTexture"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("metallicTexture"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("roughnessTexture"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("normalTexture"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("occlusionTexture"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("emissiveTexture"));

                EditorGUI.indentLevel--;
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0;
    }

    /// <summary>
    /// TextureData를 상속하는 클래스들을 위한 기본 PropertyDrawer입니다.
    /// 텍스처 경로 필드와 파일 선택 버튼을 그리는 공통 로직을 포함합니다.
    /// </summary>
    public class TextureDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            EditorGUILayout.LabelField(label, EditorStyles.label);
            EditorGUI.indentLevel++;
            
            DrawTexturePath(property);
            DrawSpecificFields(property);
            
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// 파생 클래스에서 특정 필드를 그리기 위해 재정의할 수 있습니다.
        /// </summary>
        protected virtual void DrawSpecificFields(SerializedProperty property)
        {
        }

        /// <summary>
        /// 공통된 'texturePath' 필드와 파일 선택 UI를 그립니다.
        /// </summary>
        protected void DrawTexturePath(SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property.FindPropertyRelative("texturePath"));
            if (GUILayout.Button("Load", GUILayout.Width(65)))
            {
                var path = EditorUtility.OpenFilePanel("Select Texture", Application.streamingAssetsPath, "png,jpg,jpeg,dds,bmp");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.streamingAssetsPath))
                    {
                        path = path.Replace(Application.streamingAssetsPath, "").TrimStart('\\').TrimStart('/');
                    }
                    else
                    {
                        path = "";
                    }
                    
                    property.FindPropertyRelative("texturePath").stringValue = path;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0;
    }

    /// <summary>
    /// RGBTextureData를 클래스를 위한 PropertyDrawer입니다.
    /// Color Picker를 포함합니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(RGBTextureData))]
    public class RGBTextureDataDrawer : TextureDataDrawer
    {
        protected override void DrawSpecificFields(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative("color"));
        }
    }
    
    /// <summary>
    /// RGBATextureData를 클래스를 위한 PropertyDrawer입니다.
    /// Color Picker와 float 값의 슬라이더를 포함합니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(RGBATextureData))]
    public class RGBATextureDataDrawer : RGBTextureDataDrawer
    {
        protected override void DrawSpecificFields(SerializedProperty property)
        {
            base.DrawSpecificFields(property);
            
            var alphaProperty = property.FindPropertyRelative("alpha");
            EditorGUILayout.Slider(alphaProperty, 0f, 1f, new GUIContent(alphaProperty.displayName));
        }
    }
    
    /// <summary>
    /// FloatTextureData를 클래스를 위한 PropertyDrawer입니다.
    /// float 값의 슬라이더를 포함합니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(FloatTextureData))]
    public class FloatTextureDataDrawer : TextureDataDrawer
    {
        protected override void DrawSpecificFields(SerializedProperty property)
        {
            var value = property.FindPropertyRelative("value");
            EditorGUILayout.Slider(value, 0f, 1f, new GUIContent(value.displayName));
        }
    }
}