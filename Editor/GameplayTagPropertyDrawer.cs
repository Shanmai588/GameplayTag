using UnityEditor;
using UnityEngine;

namespace GameplayTag
{
    

    /// <summary>
    /// Custom property drawer for GameplayTag in the Unity Inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(GameplayTag))]
    public class GameplayTagPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            // Get the tag name property
            var tagNameProp = property.FindPropertyRelative("tagName");
            
            // Create button that shows current tag
            string buttonText = string.IsNullOrEmpty(tagNameProp.stringValue) ? "None" : tagNameProp.stringValue;
            if (GUI.Button(position, buttonText, EditorStyles.popup))
            {
                ShowTagSelector(position, tagNameProp);
            }
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private void ShowTagSelector(Rect position, SerializedProperty property)
        {
            GenericMenu menu = new GenericMenu();
            
            // Add "None" option
            menu.AddItem(new GUIContent("None"), string.IsNullOrEmpty(property.stringValue), () =>
            {
                property.stringValue = "";
                property.serializedObject.ApplyModifiedProperties();
            });
            
            menu.AddSeparator("");
            
            // Initialize tag manager if needed
            if (!GameplayTagManager.Instance.IsInitialized)
            {
                GameplayTagManager.Instance.Initialize();
            }
            
            // Get all tags and add to menu
            var allTags = GameplayTagManager.Instance.GetAllTags();
            foreach (var tag in allTags)
            {
                string fullName = tag.GetFullTagName();
                menu.AddItem(new GUIContent(fullName.Replace(".", "/")), 
                    fullName == property.stringValue, 
                    () =>
                    {
                        property.stringValue = fullName;
                        property.serializedObject.ApplyModifiedProperties();
                    });
            }
            
            menu.ShowAsContext();
        }
    }
}