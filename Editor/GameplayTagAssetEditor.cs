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
            var window = EditorWindow.GetWindow<AddTagWindow>(true, "Add New Tag", true);
            window.Initialize(this, parentPath);
            window.ShowModal();
        }

        private void ShowEditTagWindow(TagNode node)
        {
            EditTagWindow.ShowWindow(this, node.TagData);
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
            // Check if this tag has children
            bool hasChildren = HasChildTags(node);
            
            if (hasChildren)
            {
                // Show options dialog
                int option = EditorUtility.DisplayDialogComplex(
                    "Delete Parent Tag",
                    $"The tag '{node.FullPath}' has child tags. What would you like to do?",
                    "Delete All", // Option 0
                    "Cancel",     // Option 1
                    "Move Children Up" // Option 2
                );
                
                if (option == 1) // Cancel
                    return;
                
                serializedObject.Update();
                var asset = target as GameplayTagAsset;
                var list = new List<GameplayTagData>(asset.tagDefinitions);
                
                if (option == 0) // Delete all
                {
                    // Remove this tag and all children
                    list.RemoveAll(t => t.tagName == node.FullPath || t.tagName.StartsWith(node.FullPath + "."));
                }
                else if (option == 2) // Move children up
                {
                    // First, update all child tags to remove this level
                    var tagToRemove = node.FullPath + ".";
                    var parentPrefix = "";
                    
                    // Determine the new parent prefix
                    var lastDot = node.FullPath.LastIndexOf('.');
                    if (lastDot > 0)
                    {
                        parentPrefix = node.FullPath.Substring(0, lastDot) + ".";
                    }
                    
                    // Update child tags
                    foreach (var tag in list)
                    {
                        if (tag.tagName.StartsWith(tagToRemove))
                        {
                            // Remove the deleted tag level from the path
                            var remainingPath = tag.tagName.Substring(tagToRemove.Length);
                            tag.tagName = parentPrefix + remainingPath;
                            
                            // If we're at root level, just use the remaining path
                            if (string.IsNullOrEmpty(parentPrefix))
                            {
                                tag.tagName = remainingPath;
                            }
                        }
                    }
                    
                    // Then remove the parent tag
                    list.RemoveAll(t => t.tagName == node.FullPath);
                }
                
                asset.tagDefinitions = list.ToArray();
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();
                RefreshTreeView();
            }
            else
            {
                // Simple delete for leaf tags
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
        }
        
        private bool HasChildTags(TagNode node)
        {
            var asset = target as GameplayTagAsset;
            if (asset.tagDefinitions == null) return false;
            
            var prefix = node.FullPath + ".";
            return asset.tagDefinitions.Any(t => t.tagName.StartsWith(prefix));
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
}
#endif