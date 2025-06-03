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
    /// Window for editing existing tags
    /// </summary>
    public class EditTagWindow : EditorWindow
    {
        private static GameplayTagAssetEditor pendingParentEditor;
        private static GameplayTagData pendingEditingData;
        
        private GameplayTagAssetEditor parentEditor;
        private GameplayTagData editingData;
        
        private string description;
        private string category;
        private bool isNetworked;
        private Color debugColor;
        
        // For showing related tags
        private List<string> parentTags = new List<string>();
        private List<string> childTags = new List<string>();
        
        // UI Elements that need to be updated
        private TextField descField;
        private TextField categoryField;
        private Toggle networkToggle;
        private ColorField colorField;
        
        private bool uiBuilt = false;

        public static void ShowWindow(GameplayTagAssetEditor editor, GameplayTagData data)
        {
            pendingParentEditor = editor;
            pendingEditingData = data;
            
            var window = GetWindow<EditTagWindow>("Edit Tag Properties", true);
            window.minSize = new Vector2(450, 400);
            window.maxSize = new Vector2(450, 600);
            window.Show();
        }

        private void OnEnable()
        {
            if (pendingParentEditor != null && pendingEditingData != null)
            {
                Initialize(pendingParentEditor, pendingEditingData);
                pendingParentEditor = null;
                pendingEditingData = null;
            }
        }

        private void Initialize(GameplayTagAssetEditor editor, GameplayTagData data)
        {
            parentEditor = editor;
            editingData = data;
            
            // Copy current values
            description = data.description ?? "";
            category = data.category ?? "";
            isNetworked = data.isNetworked;
            debugColor = data.debugColor;
            
            // Find related tags
            FindRelatedTags();
        }

        private void FindRelatedTags()
        {
            if (parentEditor == null || parentEditor.target == null || editingData == null)
                return;
                
            parentTags.Clear();
            childTags.Clear();
            
            var asset = parentEditor.target as GameplayTagAsset;
            if (asset.tagDefinitions == null) return;
            
            // Find parent tags
            var parts = editingData.tagName.Split('.');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var parentPath = string.Join(".", parts.Take(i + 1));
                if (asset.tagDefinitions.Any(t => t.tagName == parentPath))
                {
                    parentTags.Add(parentPath);
                }
            }
            
            // Find child tags
            foreach (var tag in asset.tagDefinitions)
            {
                if (tag != null && tag != editingData && 
                    !string.IsNullOrEmpty(tag.tagName) && 
                    tag.tagName.StartsWith(editingData.tagName + "."))
                {
                    childTags.Add(tag.tagName);
                }
            }
        }

        private void CreateGUI()
        {
            if (uiBuilt)
                return;
                
            if (editingData == null)
                return;
                
            BuildUI();
            uiBuilt = true;
        }

        private void BuildUI()
        {
            var root = rootVisualElement;
            root.Clear();
            
            root.style.paddingLeft = 15;
            root.style.paddingRight = 15;
            root.style.paddingTop = 15;
            root.style.paddingBottom = 15;
            
            // Title
            var titleLabel = new Label("Edit Tag Properties");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 16;
            titleLabel.style.marginBottom = 15;
            root.Add(titleLabel);
            
            // Tag name section
            var tagNameSection = new VisualElement();
            tagNameSection.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            tagNameSection.style.borderTopLeftRadius = 5;
            tagNameSection.style.borderTopRightRadius = 5;
            tagNameSection.style.borderBottomLeftRadius = 5;
            tagNameSection.style.borderBottomRightRadius = 5;
            tagNameSection.style.paddingLeft = 10;
            tagNameSection.style.paddingRight = 10;
            tagNameSection.style.paddingTop = 10;
            tagNameSection.style.paddingBottom = 10;
            tagNameSection.style.marginBottom = 15;
            
            var tagNameLabel = new Label("Tag Path:");
            tagNameLabel.style.fontSize = 12;
            tagNameLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            tagNameSection.Add(tagNameLabel);
            
            var tagNameValue = new Label(editingData.tagName);
            tagNameValue.style.fontSize = 14;
            tagNameValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            tagNameValue.style.marginTop = 5;
            tagNameSection.Add(tagNameValue);
            
            root.Add(tagNameSection);
            
            // Properties section
            var propertiesLabel = new Label("Properties");
            propertiesLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            propertiesLabel.style.marginBottom = 10;
            root.Add(propertiesLabel);
            
            // Description field
            descField = new TextField("Description:");
            descField.value = description;
            descField.multiline = true;
            descField.style.height = 60;
            descField.RegisterValueChangedCallback(evt => description = evt.newValue);
            root.Add(descField);
            
            // Category field
            categoryField = new TextField("Category:");
            categoryField.value = category;
            categoryField.RegisterValueChangedCallback(evt => category = evt.newValue);
            categoryField.style.marginTop = 10;
            root.Add(categoryField);
            
            // Horizontal container for toggle and color
            var horizontalContainer = new VisualElement();
            horizontalContainer.style.flexDirection = FlexDirection.Row;
            horizontalContainer.style.marginTop = 10;
            horizontalContainer.style.marginBottom = 15;
            
            // Networked toggle
            networkToggle = new Toggle("Is Networked");
            networkToggle.value = isNetworked;
            networkToggle.RegisterValueChangedCallback(evt => isNetworked = evt.newValue);
            networkToggle.style.flexGrow = 1;
            horizontalContainer.Add(networkToggle);
            
            // Color field
            var colorContainer = new VisualElement();
            colorContainer.style.flexDirection = FlexDirection.Row;
            colorContainer.style.alignItems = Align.Center;
            
            var colorLabel = new Label("Debug Color:");
            colorLabel.style.marginRight = 5;
            colorContainer.Add(colorLabel);
            
            colorField = new ColorField();
            colorField.value = debugColor;
            colorField.RegisterValueChangedCallback(evt => debugColor = evt.newValue);
            colorField.style.width = 60;
            colorContainer.Add(colorField);
            
            horizontalContainer.Add(colorContainer);
            root.Add(horizontalContainer);
            
            // Related tags section
            if (parentTags.Count > 0 || childTags.Count > 0)
            {
                var separator = new VisualElement();
                separator.style.height = 1;
                separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                separator.style.marginTop = 10;
                separator.style.marginBottom = 15;
                root.Add(separator);
                
                var relatedLabel = new Label("Related Tags");
                relatedLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                relatedLabel.style.marginBottom = 10;
                root.Add(relatedLabel);
                
                var scrollView = new ScrollView();
                scrollView.style.maxHeight = 150;
                scrollView.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.2f);
                scrollView.style.borderTopLeftRadius = 3;
                scrollView.style.borderTopRightRadius = 3;
                scrollView.style.borderBottomLeftRadius = 3;
                scrollView.style.borderBottomRightRadius = 3;
                scrollView.style.paddingLeft = 5;
                scrollView.style.paddingRight = 5;
                scrollView.style.paddingTop = 5;
                scrollView.style.paddingBottom = 5;
                
                if (parentTags.Count > 0)
                {
                    var parentHeader = new Label("Parents:");
                    parentHeader.style.fontSize = 11;
                    parentHeader.style.color = new Color(0.6f, 0.6f, 0.6f);
                    scrollView.Add(parentHeader);
                    
                    foreach (var parent in parentTags)
                    {
                        var parentItem = new Label($"  ▲ {parent}");
                        parentItem.style.fontSize = 12;
                        parentItem.style.marginLeft = 10;
                        scrollView.Add(parentItem);
                    }
                }
                
                if (childTags.Count > 0)
                {
                    if (parentTags.Count > 0)
                    {
                        var spacer = new VisualElement();
                        spacer.style.height = 5;
                        scrollView.Add(spacer);
                    }
                    
                    var childHeader = new Label("Children:");
                    childHeader.style.fontSize = 11;
                    childHeader.style.color = new Color(0.6f, 0.6f, 0.6f);
                    scrollView.Add(childHeader);
                    
                    foreach (var child in childTags)
                    {
                        var childItem = new Label($"  ▼ {child}");
                        childItem.style.fontSize = 12;
                        childItem.style.marginLeft = 10;
                        scrollView.Add(childItem);
                    }
                }
                
                root.Add(scrollView);
            }
            
            // Spacer to push buttons to bottom
            var btnSpacer = new VisualElement();
            btnSpacer.style.flexGrow = 1;
            root.Add(btnSpacer);    
            
            // Buttons
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.marginTop = 20;
            buttonContainer.style.justifyContent = Justify.Center;
            
            var saveButton = new Button(() => SaveEditChanges()) 
            { 
                text = "Save Changes" 
            };
            saveButton.style.width = 120;
            saveButton.style.marginRight = 10;
            saveButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.7f);
            buttonContainer.Add(saveButton);
            
            var cancelButton = new Button(() => Close()) 
            { 
                text = "Cancel" 
            };
            cancelButton.style.width = 100;
            buttonContainer.Add(cancelButton);
            
            root.Add(buttonContainer);
        }

        private void SaveEditChanges()
        {
            if (parentEditor == null || editingData == null)
            {
                Close();
                return;
            }
            
            var serializedObject = parentEditor.serializedObject;
            serializedObject.Update();
            
            // Update the tag data
            editingData.description = description;
            editingData.category = category;
            editingData.isNetworked = isNetworked;
            editingData.debugColor = debugColor;
            
            EditorUtility.SetDirty(parentEditor.target);
            serializedObject.ApplyModifiedProperties();
            parentEditor.RefreshTreeView();
            
            Close();
        }
    }
}
#endif