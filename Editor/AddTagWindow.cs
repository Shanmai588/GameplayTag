#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameplayTag.Editor
{
    /// <summary>
    /// Window for adding new tags
    /// </summary>
    public class AddTagWindow : EditorWindow
    {
        private GameplayTagAssetEditor parentEditor;
        private string parentPath;
        
        private string tagName = "";
        private string description = "";
        private string category = "";
        private bool isNetworked = false;
        private Color debugColor = Color.white;

        public void Initialize(GameplayTagAssetEditor editor, string parent)
        {
            parentEditor = editor;
            parentPath = parent;
            
            minSize = new Vector2(400, 250);
            maxSize = new Vector2(400, 250);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            
            var titleLabel = new Label("Add New Gameplay Tag");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 14;
            titleLabel.style.marginBottom = 10;
            root.Add(titleLabel);
            
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parentLabel = new Label($"Parent: {parentPath}");
                parentLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                parentLabel.style.marginBottom = 5;
                root.Add(parentLabel);
            }
            
            // Tag name field
            var nameField = new TextField("Tag Name:");
            nameField.value = tagName;
            nameField.RegisterValueChangedCallback(evt => tagName = evt.newValue);
            root.Add(nameField);
            
            // Description field
            var descField = new TextField("Description:");
            descField.value = description;
            descField.multiline = true;
            descField.style.height = 50;
            descField.RegisterValueChangedCallback(evt => description = evt.newValue);
            root.Add(descField);
            
            // Category field
            var categoryField = new TextField("Category:");
            categoryField.value = category;
            categoryField.RegisterValueChangedCallback(evt => category = evt.newValue);
            root.Add(categoryField);
            
            // Networked toggle
            var networkToggle = new Toggle("Is Networked");
            networkToggle.value = isNetworked;
            networkToggle.RegisterValueChangedCallback(evt => isNetworked = evt.newValue);
            root.Add(networkToggle);
            
            // Color field
            var colorField = new ColorField("Debug Color:");
            colorField.value = debugColor;
            colorField.RegisterValueChangedCallback(evt => debugColor = evt.newValue);
            root.Add(colorField);
            
            // Buttons
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.marginTop = 20;
            buttonContainer.style.justifyContent = Justify.Center;
            
            var saveButton = new Button(() => SaveTag()) { text = "Add Tag" };
            saveButton.style.width = 100;
            saveButton.style.marginRight = 10;
            buttonContainer.Add(saveButton);
            
            var cancelButton = new Button(() => Close()) { text = "Cancel" };
            cancelButton.style.width = 100;
            buttonContainer.Add(cancelButton);
            
            root.Add(buttonContainer);
        }

        private void SaveTag()
        {
            if (string.IsNullOrEmpty(tagName))
            {
                EditorUtility.DisplayDialog("Invalid Tag Name", "Please enter a tag name.", "OK");
                return;
            }
            
            var fullTagName = string.IsNullOrEmpty(parentPath) ? tagName : $"{parentPath}.{tagName}";
            
            if (!GameplayTagManager.Instance.IsValidTagName(fullTagName))
            {
                EditorUtility.DisplayDialog("Invalid Tag Name", 
                    "Tag name contains invalid characters. Use only letters, numbers, dots, and underscores.", "OK");
                return;
            }
            
            var serializedObject = parentEditor.serializedObject;
            serializedObject.Update();
            
            var asset = parentEditor.target as GameplayTagAsset;
            var newTag = new GameplayTagData
            {
                tagName = fullTagName,
                description = description,
                category = category,
                isNetworked = isNetworked,
                debugColor = debugColor
            };
            
            var list = new List<GameplayTagData>(asset.tagDefinitions ?? new GameplayTagData[0]);
            
            // Check if tag already exists
            if (list.Any(t => t.tagName == fullTagName))
            {
                EditorUtility.DisplayDialog("Tag Already Exists", 
                    $"A tag with the name '{fullTagName}' already exists.", "OK");
                return;
            }
            
            list.Add(newTag);
            asset.tagDefinitions = list.ToArray();
            
            EditorUtility.SetDirty(parentEditor.target);
            serializedObject.ApplyModifiedProperties();
            parentEditor.RefreshTreeView();
            
            Close();
        }
    }
}
#endif