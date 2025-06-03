#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameplayTag.Editor
{
    /// <summary>
    ///     Property drawer for GameplayTagData
    /// </summary>
    [CustomPropertyDrawer(typeof(GameplayTagData))]
    public class GameplayTagDataDrawer : PropertyDrawer
    {
        private const float lineHeight = 18f;
        private const float spacing = 2f;
        private bool isExpanded;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var tagNameProp = property.FindPropertyRelative("tagName");
            var descriptionProp = property.FindPropertyRelative("description");
            var categoryProp = property.FindPropertyRelative("category");
            var isNetworkedProp = property.FindPropertyRelative("isNetworked");
            var debugColorProp = property.FindPropertyRelative("debugColor");

            // First line - foldout with tag name
            var foldoutRect = new Rect(position.x, position.y, position.width, lineHeight);
            isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, tagNameProp.stringValue, true);

            if (isExpanded)
            {
                EditorGUI.indentLevel++;
                var y = position.y + lineHeight + spacing;

                // Tag name (read-only in drawer)
                var tagNameRect = new Rect(position.x, y, position.width, lineHeight);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(tagNameRect, tagNameProp);
                EditorGUI.EndDisabledGroup();
                y += lineHeight + spacing;

                // Description
                var descRect = new Rect(position.x, y, position.width, lineHeight * 2);
                EditorGUI.PropertyField(descRect, descriptionProp);
                y += lineHeight * 2 + spacing;

                // Category
                var categoryRect = new Rect(position.x, y, position.width, lineHeight);
                EditorGUI.PropertyField(categoryRect, categoryProp);
                y += lineHeight + spacing;

                // Networked and Color on same line
                var halfWidth = position.width * 0.5f;
                var networkRect = new Rect(position.x, y, halfWidth - 5, lineHeight);
                EditorGUI.PropertyField(networkRect, isNetworkedProp);

                var colorRect = new Rect(position.x + halfWidth, y, halfWidth, lineHeight);
                EditorGUI.PropertyField(colorRect, debugColorProp);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (isExpanded)
                // Foldout + tagName + description(2 lines) + category + (networked/color)
                return lineHeight * 6 + spacing * 5;
            return lineHeight;
        }
    }
}
#endif