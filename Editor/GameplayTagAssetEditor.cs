#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GameplayTag.Editor
{
    /// <summary>
    /// Custom editor for GameplayTagAsset with hierarchical tree view using UI Toolkit
    /// </summary>
    [CustomEditor(typeof(GameplayTagAsset))]
    public class GameplayTagAssetEditor : UnityEditor.Editor
    {
        private VisualElement rootVisualElement;
        private SerializedProperty tagDefinitionsProperty;
        private Dictionary<string, bool> expandedStates = new Dictionary<string, bool>();
        private string searchFilter = "";

        private void OnEnable()
        {
            tagDefinitionsProperty = serializedObject.FindProperty("tagDefinitions");
        }

        public override VisualElement CreateInspectorGUI()
        {
            rootVisualElement = new VisualElement();
            
            // Add USS styling
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.ui/PackageResources/StyleSheets/Default.uss");
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);
            
            // Create toolbar
            var toolbar = new Toolbar();
            toolbar.style.height = 25;
            
            var titleLabel = new Label("Gameplay Tag Hierarchy");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginLeft = 5;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            toolbar.Add(titleLabel);
            
            toolbar.Add(new ToolbarSpacer() { style = { flexGrow = 1 } });
            
            var addRootButton = new ToolbarButton(() => ShowAddTagWindow("")) { text = "Add Root Tag" };
            toolbar.Add(addRootButton);
            
            var expandAllButton = new ToolbarButton(() => ExpandCollapseAll(true)) { text = "Expand All" };
            toolbar.Add(expandAllButton);
            
            var collapseAllButton = new ToolbarButton(() => ExpandCollapseAll(false)) { text = "Collapse All" };
            toolbar.Add(collapseAllButton);
            
            rootVisualElement.Add(toolbar);
            
            // Search field
            var searchContainer = new VisualElement();
            searchContainer.style.flexDirection = FlexDirection.Row;
            searchContainer.style.marginTop = 5;
            searchContainer.style.marginBottom = 5;
            searchContainer.style.marginLeft = 5;
            searchContainer.style.marginRight = 5;
            
            var searchLabel = new Label("Search:");
            searchLabel.style.width = 50;
            searchLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            searchContainer.Add(searchLabel);
            
            var searchField = new TextField();
            searchField.style.flexGrow = 1;
            searchField.RegisterValueChangedCallback(evt =>
            {
                searchFilter = evt.newValue;
                RefreshTreeView();
            });
            searchContainer.Add(searchField);
            
            rootVisualElement.Add(searchContainer);
            
            // Create scroll view for tree
            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            scrollView.style.marginTop = 5;
            scrollView.name = "tree-scroll-view";
            rootVisualElement.Add(scrollView);
            
            RefreshTreeView();
            
            return rootVisualElement;
        }

        public void RefreshTreeView()
        {
            if (rootVisualElement == null)
                return;

            var scrollView = rootVisualElement.Q<ScrollView>("tree-scroll-view");
            scrollView.Clear();
            
            // Build tag hierarchy
            var rootTags = BuildTagHierarchy();
            
            // Create tree items
            foreach (var rootTag in rootTags)
            {
                if (ShouldShowTag(rootTag))
                {
                    var treeItem = CreateTreeItem(rootTag, 0);
                    scrollView.Add(treeItem);
                }
            }
        }

        private VisualElement CreateTreeItem(TagNode node, int depth)
        {
            var itemContainer = new VisualElement();
            
            var rowContainer = new VisualElement();
            rowContainer.style.flexDirection = FlexDirection.Row;
            rowContainer.style.marginLeft = depth * 20;
            rowContainer.style.marginTop = 2;
            rowContainer.style.marginBottom = 2;
            rowContainer.style.minHeight = 22;
            
            // Foldout arrow container
            var arrowContainer = new VisualElement();
            arrowContainer.style.width = 20;
            arrowContainer.style.height = 20;
            arrowContainer.style.justifyContent = Justify.Center;
            arrowContainer.style.alignItems = Align.Center;
            
            if (node.Children.Count > 0)
            {
                var arrow = new Label(GetExpandedState(node.FullPath) ? "▼" : "▶");
                arrow.style.unityTextAlign = TextAnchor.MiddleCenter;
                arrow.style.fontSize = 10;
                arrow.style.color = new Color(0.7f, 0.7f, 0.7f);
                arrowContainer.Add(arrow);
                
                // Make arrow clickable
                arrowContainer.RegisterCallback<MouseDownEvent>(evt =>
                {
                    SetExpandedState(node.FullPath, !GetExpandedState(node.FullPath));
                    RefreshTreeView();
                    evt.StopPropagation();
                });
                
                // Hover effect
                arrowContainer.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    arrow.style.color = Color.white;
                });
                arrowContainer.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    arrow.style.color = new Color(0.7f, 0.7f, 0.7f);
                });
            }
            
            rowContainer.Add(arrowContainer);
            
            // Tag content container
            var tagContainer = new VisualElement();
            tagContainer.style.flexDirection = FlexDirection.Row;
            tagContainer.style.flexGrow = 1;
            tagContainer.style.backgroundColor = node.TagData != null 
                ? new Color(0.25f, 0.25f, 0.25f, 0.3f) 
                : new Color(0.3f, 0.3f, 0.3f, 0.1f);
            tagContainer.style.borderTopLeftRadius = 3;
            tagContainer.style.borderTopRightRadius = 3;
            tagContainer.style.borderBottomLeftRadius = 3;
            tagContainer.style.borderBottomRightRadius = 3;
            tagContainer.style.paddingLeft = 8;
            tagContainer.style.paddingRight = 5;
            tagContainer.style.paddingTop = 2;
            tagContainer.style.paddingBottom = 2;
            tagContainer.style.alignItems = Align.Center;
            
            // Tag name label
            var tagNameLabel = new Label(node.TagName);
            tagNameLabel.style.flexGrow = 1;
            tagNameLabel.style.unityFontStyleAndWeight = node.TagData != null ? FontStyle.Normal : FontStyle.Italic;
            tagNameLabel.style.color = node.TagData != null ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            tagNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            tagContainer.Add(tagNameLabel);
            
            // Full path tooltip
            tagContainer.tooltip = $"Full Path: {node.FullPath}";
            
            // Action buttons container
            var actionsContainer = new VisualElement();
            actionsContainer.style.flexDirection = FlexDirection.Row;
            actionsContainer.style.marginLeft = 10;
            
            // Add child button
            var addChildButton = CreateActionButton("+", new Color(0.3f, 0.7f, 0.3f), 
                () => ShowAddTagWindow(node.FullPath));
            addChildButton.tooltip = "Add child tag";
            actionsContainer.Add(addChildButton);
            
            if (node.TagData != null)
            {
                // Edit button
                var editButton = CreateActionButton("✎", new Color(0.5f, 0.5f, 0.7f), 
                    () => ShowEditTagWindow(node));
                editButton.tooltip = "Edit tag properties";
                actionsContainer.Add(editButton);
                
                // Delete button
                var deleteButton = CreateActionButton("×", new Color(0.7f, 0.3f, 0.3f), 
                    () => DeleteTag(node));
                deleteButton.tooltip = "Delete tag";
                actionsContainer.Add(deleteButton);
            }
            else
            {
                // Create tag button for non-existent parent tags
                var createButton = CreateActionButton("✓", new Color(0.3f, 0.5f, 0.7f), 
                    () => CreateTagAtPath(node.FullPath));
                createButton.tooltip = "Create this tag";
                actionsContainer.Add(createButton);
            }
            
            tagContainer.Add(actionsContainer);
            rowContainer.Add(tagContainer);
            itemContainer.Add(rowContainer);
            
            // Add children if expanded
            if (GetExpandedState(node.FullPath) && node.Children.Count > 0)
            {
                var childrenContainer = new VisualElement();
                
                foreach (var child in node.Children.OrderBy(c => c.TagName))
                {
                    if (ShouldShowTag(child))
                    {
                        var childItem = CreateTreeItem(child, depth + 1);
                        childrenContainer.Add(childItem);
                    }
                }
                
                itemContainer.Add(childrenContainer);
            }
            
            return itemContainer;
        }

        private Button CreateActionButton(string text, Color backgroundColor, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.width = 22;
            button.style.height = 18;
            button.style.marginLeft = 2;
            button.style.fontSize = 12;
            button.style.backgroundColor = backgroundColor;
            button.style.borderTopLeftRadius = 2;
            button.style.borderTopRightRadius = 2;
            button.style.borderBottomLeftRadius = 2;
            button.style.borderBottomRightRadius = 2;
            button.style.paddingLeft = 0;
            button.style.paddingRight = 0;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;
            
            // Hover effect
            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                button.style.backgroundColor = backgroundColor * 1.2f;
            });
            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                button.style.backgroundColor = backgroundColor;
            });
            
            return button;
        }

        private bool GetExpandedState(string path)
        {
            return expandedStates.TryGetValue(path, out bool expanded) ? expanded : false;
        }

        private void SetExpandedState(string path, bool expanded)
        {
            expandedStates[path] = expanded;
        }

        private List<TagNode> BuildTagHierarchy()
        {
            var rootNodes = new Dictionary<string, TagNode>();
            var allNodes = new Dictionary<string, TagNode>();
            
            // First, create all nodes from existing tags
            var asset = target as GameplayTagAsset;
            if (asset.tagDefinitions != null)
            {
                foreach (var tagData in asset.tagDefinitions)
                {
                    if (tagData == null || string.IsNullOrEmpty(tagData.tagName))
                        continue;
                    
                    var parts = tagData.tagName.Split('.');
                    var currentPath = "";
                    
                    for (int i = 0; i < parts.Length; i++)
                    {
                        currentPath = i == 0 ? parts[i] : currentPath + "." + parts[i];
                        
                        if (!allNodes.ContainsKey(currentPath))
                        {
                            var node = new TagNode
                            {
                                TagName = parts[i],
                                FullPath = currentPath,
                                TagData = currentPath == tagData.tagName ? tagData : null
                            };
                            
                            allNodes[currentPath] = node;
                            
                            if (i == 0)
                            {
                                rootNodes[parts[i]] = node;
                            }
                        }
                        else if (currentPath == tagData.tagName)
                        {
                            allNodes[currentPath].TagData = tagData;
                        }
                    }
                }
            }
            
            // Then, establish parent-child relationships
            foreach (var kvp in allNodes)
            {
                var node = kvp.Value;
                var lastDotIndex = node.FullPath.LastIndexOf('.');
                
                if (lastDotIndex > 0)
                {
                    var parentPath = node.FullPath.Substring(0, lastDotIndex);
                    if (allNodes.TryGetValue(parentPath, out var parentNode))
                    {
                        parentNode.Children.Add(node);
                        node.Parent = parentNode;
                    }
                }
            }
            
            return rootNodes.Values.OrderBy(n => n.TagName).ToList();
        }

        private bool ShouldShowTag(TagNode node)
        {
            if (string.IsNullOrEmpty(searchFilter))
                return true;
            
            // Check if this node or any of its children match the search
            return node.FullPath.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   node.Children.Any(child => ShouldShowTag(child));
        }

        private void ShowAddTagWindow(string parentPath)
        {
            var window = EditorWindow.GetWindow<AddEditTagWindow>(true, "Add New Tag", true);
            window.Initialize(this, null, parentPath);
            window.ShowModal();
        }

        private void ShowEditTagWindow(TagNode node)
        {
            var window = EditorWindow.GetWindow<AddEditTagWindow>(true, "Edit Tag", true);
            window.Initialize(this, node.TagData, node.FullPath);
            window.ShowModal();
        }

        private void CreateTagAtPath(string path)
        {
            serializedObject.Update();
            
            var asset = target as GameplayTagAsset;
            var newTag = new GameplayTagData
            {
                tagName = path,
                description = "",
                category = "",
                isNetworked = false,
                debugColor = Color.white
            };
            
            var list = new List<GameplayTagData>(asset.tagDefinitions ?? new GameplayTagData[0]);
            list.Add(newTag);
            asset.tagDefinitions = list.ToArray();
            
            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
            RefreshTreeView();
        }

        private void DeleteTag(TagNode node)
        {
            if (EditorUtility.DisplayDialog("Delete Tag", 
                $"Are you sure you want to delete the tag '{node.FullPath}'?", 
                "Delete", "Cancel"))
            {
                serializedObject.Update();
                
                var asset = target as GameplayTagAsset;
                var list = new List<GameplayTagData>(asset.tagDefinitions);
                list.RemoveAll(t => t == node.TagData);
                asset.tagDefinitions = list.ToArray();
                
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();
                RefreshTreeView();
            }
        }

        private void ExpandCollapseAll(bool expand)
        {
            var allNodes = BuildTagHierarchy();
            ExpandCollapseRecursive(allNodes, expand);
            RefreshTreeView();
        }

        private void ExpandCollapseRecursive(List<TagNode> nodes, bool expand)
        {
            foreach (var node in nodes)
            {
                expandedStates[node.FullPath] = expand;
                ExpandCollapseRecursive(node.Children, expand);
            }
        }

        private class TagNode
        {
            public string TagName { get; set; }
            public string FullPath { get; set; }
            public GameplayTagData TagData { get; set; }
            public TagNode Parent { get; set; }
            public List<TagNode> Children { get; set; } = new List<TagNode>();
        }
    }

    /// <summary>
    /// Window for adding or editing tags
    /// </summary>
    public class AddEditTagWindow : EditorWindow
    {
        private GameplayTagAssetEditor parentEditor;
        private GameplayTagData editingData;
        private string parentPath;
        private bool isEditing;
        
        private string tagName = "";
        private string description = "";
        private string category = "";
        private bool isNetworked = false;
        private Color debugColor = Color.white;

        public void Initialize(GameplayTagAssetEditor editor, GameplayTagData existingData, string parent)
        {
            parentEditor = editor;
            editingData = existingData;
            parentPath = parent;
            isEditing = existingData != null;
            
            if (isEditing)
            {
                // Extract just the tag name without parent path
                var lastDot = existingData.tagName.LastIndexOf('.');
                tagName = lastDot >= 0 ? existingData.tagName.Substring(lastDot + 1) : existingData.tagName;
                description = existingData.description;
                category = existingData.category;
                isNetworked = existingData.isNetworked;
                debugColor = existingData.debugColor;
            }
            
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
            
            var titleLabel = new Label(isEditing ? "Edit Gameplay Tag" : "Add New Gameplay Tag");
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
            if (isEditing) nameField.SetEnabled(false);
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
            
            var saveButton = new Button(() => SaveTag()) { text = isEditing ? "Save" : "Add" };
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
            if (!isEditing && string.IsNullOrEmpty(tagName))
            {
                EditorUtility.DisplayDialog("Invalid Tag Name", "Please enter a tag name.", "OK");
                return;
            }
            
            var fullTagName = string.IsNullOrEmpty(parentPath) ? tagName : $"{parentPath}.{tagName}";
            
            if (!isEditing && !GameplayTagManager.Instance.IsValidTagName(fullTagName))
            {
                EditorUtility.DisplayDialog("Invalid Tag Name", 
                    "Tag name contains invalid characters. Use only letters, numbers, dots, and underscores.", "OK");
                return;
            }
            
            var serializedObject = parentEditor.serializedObject;
            serializedObject.Update();
            
            if (isEditing)
            {
                // Update existing tag
                editingData.description = description;
                editingData.category = category;
                editingData.isNetworked = isNetworked;
                editingData.debugColor = debugColor;
            }
            else
            {
                // Add new tag
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
            }
            
            EditorUtility.SetDirty(parentEditor.target);
            serializedObject.ApplyModifiedProperties();
            parentEditor.RefreshTreeView();
            
            Close();
        }
    }
}
#endif