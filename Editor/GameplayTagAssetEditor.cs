#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameplayTag.Editor
{
    [CustomEditor(typeof(GameplayTagAsset))]
    public class GameplayTagAssetEditor : UnityEditor.Editor
    {
        // USS Class Names (Add new ones as needed)
        private const string ScrollViewName = "tree-scroll-view";
        private const string TreeItemClassName = "tree-item";

        private const string TreeItemRowClassName = "tree-item-row";

        // private const string ArrowContainerClassName = "tree-item-arrow-container"; // No longer a separate container
        private const string ArrowClassName = "tree-item-arrow"; // Now directly in tagContainer
        private const string TagContainerClassName = "tree-item-tag-container";
        private const string TagContainerImplicitClassName = "tree-item-tag-container-implicit";
        private const string TagNameLabelClassName = "tree-item-tag-name";
        private const string TagNameLabelImplicitClassName = "tree-item-tag-name-implicit";
        private const string RenameTextFieldClassName = "tree-item-rename-field"; // For the rename input
        private const string ActionsContainerClassName = "tree-item-actions-container";
        private const string ActionButtonClassName = "tree-item-action-button";
        private const string ActionButtonAddClassName = "action-button-add";
        private const string ActionButtonEditClassName = "action-button-edit";
        private const string ActionButtonDeleteClassName = "action-button-delete";
        private const string ActionButtonCreateClassName = "action-button-create";
        private const string ChildrenContainerClassName = "children-container";
        private const string EmptyStateLabelClassName = "empty-state-label";
        private VisualElement _activeRenameActionsContainer;
        private TextField _activeRenameField;
        private Label _activeRenameLabel;
        private bool _isCommittingRenameOrCancelling;

        // Rename state
        private TagNode _nodeBeingRenamed;

        private Dictionary<string, bool> expandedStates = new();

        private VisualElement rootVisualElement;
        private TextField searchField;
        private string searchFilter = "";
        private ScrollView treeScrollView;


        public void OnEnable()
        {
            // Ensure rename state is clear if assembly reloads
            ClearRenameState();
        }

        public override VisualElement CreateInspectorGUI()
        {
            rootVisualElement = new VisualElement { style = { flexGrow = 1 } };

            var scriptAsset = MonoScript.FromScriptableObject(this);
            var scriptPath = AssetDatabase.GetAssetPath(scriptAsset);
            var scriptDirectory = Path.GetDirectoryName(scriptPath);
            var styleSheetPath = Path.Combine(scriptDirectory, "GameplayTagAssetEditor.uss");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);

            if (styleSheet != null) rootVisualElement.styleSheets.Add(styleSheet);
            else Debug.LogWarning($"[GameplayTagAssetEditor] Stylesheet not found at {styleSheetPath}.");

            var toolbar = new Toolbar();
            var titleLabel = new Label("Gameplay Tag Hierarchy")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold, marginLeft = 5, unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            toolbar.Add(titleLabel);
            toolbar.Add(new ToolbarSpacer { style = { flexGrow = 1 } });
            toolbar.Add(new ToolbarButton(() => ShowAddTagWindow("")) { text = "Add Root Tag" });
            toolbar.Add(new ToolbarButton(() => ExpandCollapseAll(true)) { text = "Expand All" });
            toolbar.Add(new ToolbarButton(() => ExpandCollapseAll(false)) { text = "Collapse All" });
            rootVisualElement.Add(toolbar);

            var searchContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, marginTop = 5, marginBottom = 5, marginLeft = 5, marginRight = 5
                }
            };
            searchContainer.Add(new Label("Search:")
                { style = { width = 50, unityTextAlign = TextAnchor.MiddleLeft } });
            searchField = new TextField { style = { flexGrow = 1 } };
            searchField.RegisterValueChangedCallback(evt =>
            {
                searchFilter = evt.newValue;
                RefreshTreeView();
            });
            searchContainer.Add(searchField);
            rootVisualElement.Add(searchContainer);

            treeScrollView = new ScrollView { name = ScrollViewName, style = { flexGrow = 1 } };
            rootVisualElement.Add(treeScrollView);

            RefreshTreeView();
            return rootVisualElement;
        }

        public void RefreshTreeView()
        {
            if (rootVisualElement == null || treeScrollView == null) return;
            ClearRenameState(); // Ensure rename is cancelled if a refresh happens externally
            treeScrollView.Clear();
            var rootTags = BuildTagHierarchy();

            if (!rootTags.Any() && string.IsNullOrEmpty(searchFilter))
            {
                ShowEmptyStateMessage();
                return;
            }

            var foundAnyResults = false;
            foreach (var rootTag in rootTags)
                if (ShouldShowTag(rootTag))
                {
                    treeScrollView.Add(CreateTreeItem(rootTag, 0));
                    foundAnyResults = true;
                }

            if (!foundAnyResults && !string.IsNullOrEmpty(searchFilter)) ShowSearchNoResultsMessage();
        }

        private void ShowEmptyStateMessage()
        {
            treeScrollView.Clear();
            var emptyLabel = new Label("No tags defined. Click 'Add Root Tag' to start.")
                { name = "emptyMessageLabel" };
            emptyLabel.AddToClassList(EmptyStateLabelClassName);
            treeScrollView.Add(emptyLabel);
        }

        private void ShowSearchNoResultsMessage()
        {
            treeScrollView.Clear();
            var noResultsLabel = new Label($"No tags found matching '{searchFilter}'.")
                { name = "noResultsMessageLabel" };
            noResultsLabel.AddToClassList(EmptyStateLabelClassName);
            treeScrollView.Add(noResultsLabel);
        }

        private VisualElement CreateTreeItem(TagNode node, int depth)
        {
            var itemContainer = new VisualElement();
            itemContainer.AddToClassList(TreeItemClassName);

            var rowContainer = new VisualElement();
            rowContainer.AddToClassList(TreeItemRowClassName);
            rowContainer.style.marginLeft = depth * 20;

            var tagContainer = new VisualElement();
            tagContainer.AddToClassList(TagContainerClassName);
            if (node.TagData == null) tagContainer.AddToClassList(TagContainerImplicitClassName);

            // Arrow (now part of tagContainer)
            var arrowLabel = new Label();
            arrowLabel.AddToClassList(ArrowClassName);
            if (node.Children.Count > 0) arrowLabel.text = GetExpandedState(node.FullPath) ? "▼" : "▶";
            else arrowLabel.style.visibility = Visibility.Hidden; // Hide arrow if no children
            tagContainer.Add(arrowLabel);

            // Tag name label
            var tagNameLabel = new Label(node.TagName);
            tagNameLabel.AddToClassList(TagNameLabelClassName);
            if (node.TagData == null) tagNameLabel.AddToClassList(TagNameLabelImplicitClassName);
            if (!string.IsNullOrEmpty(searchFilter) &&
                node.TagName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                tagNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            tagContainer.Add(tagNameLabel);

            // Rename TextField (initially hidden)
            var renameField = new TextField { name = "rename-field" }; // Name for event target checks
            renameField.AddToClassList(RenameTextFieldClassName);
            renameField.style.display = DisplayStyle.None; // Hidden by default
            tagContainer.Add(renameField);

            tagContainer.tooltip =
                $"Full Path: {node.FullPath}\n{(node.TagData != null ? "Desc: " + node.TagData.description : "(Implicit parent)")}";

            // Action buttons container
            var actionsContainer = new VisualElement();
            actionsContainer.AddToClassList(ActionsContainerClassName);
            var addChildButton = CreateActionButton("+", () => ShowAddTagWindow(node.FullPath), "Add child tag",
                ActionButtonAddClassName);
            actionsContainer.Add(addChildButton);
            if (node.TagData != null)
            {
                var editButton = CreateActionButton("✎", () => ShowEditTagWindow(node), "Edit tag properties",
                    ActionButtonEditClassName);
                actionsContainer.Add(editButton);
                var deleteButton =
                    CreateActionButton("×", () => DeleteTag(node), "Delete tag", ActionButtonDeleteClassName);
                actionsContainer.Add(deleteButton);
            }
            else
            {
                var createButton = CreateActionButton("✓", () => CreateTagAtPath(node.FullPath), "Create this tag",
                    ActionButtonCreateClassName);
                actionsContainer.Add(createButton);
            }

            tagContainer.Add(actionsContainer);
            rowContainer.Add(tagContainer);

            // Event Handling for Expand/Collapse on Tag Container
            tagContainer.RegisterCallback<MouseDownEvent>(evt =>
            {
                // Prevent toggle if click is on an action button, or the active rename field
                if (evt.target is Button ||
                    (_nodeBeingRenamed == node && (evt.target == _activeRenameField ||
                                                   _activeRenameField.Contains(evt.target as VisualElement))))
                    return;
                if (node.Children.Count > 0)
                {
                    SetExpandedState(node.FullPath, !GetExpandedState(node.FullPath));
                    RefreshTreeView();
                }
            });

            // Event Handling for Rename (Double Click on Label)
            tagNameLabel.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && node.TagData != null) // Only allow renaming explicit tags
                {
                    StartRename(node, tagNameLabel, renameField, actionsContainer);
                    evt.StopPropagation();
                }
            });

            // Context Menu
            rowContainer.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                if (_nodeBeingRenamed != null) return; // Don't show context menu during rename

                evt.menu.AppendAction($"Add Child to '{node.TagName}'", a => ShowAddTagWindow(node.FullPath));
                if (node.TagData != null)
                {
                    evt.menu.AppendAction($"Rename '{node.TagName}'",
                        a => StartRename(node, tagNameLabel, renameField, actionsContainer),
                        node.TagData != null
                            ? DropdownMenuAction.Status.Normal
                            : DropdownMenuAction.Status.Disabled); // Only explicit
                    evt.menu.AppendAction($"Edit '{node.TagName}'", a => ShowEditTagWindow(node));
                    evt.menu.AppendAction($"Delete '{node.TagName}'...", a => DeleteTag(node));
                }
                else
                {
                    evt.menu.AppendAction($"Create Tag '{node.FullPath}'", a => CreateTagAtPath(node.FullPath));
                }

                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Expand All Children", a => ExpandCollapseNodeAndChildren(node, true),
                    node.Children.Any() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Collapse All Children", a => ExpandCollapseNodeAndChildren(node, false),
                    node.Children.Any() && GetExpandedState(node.FullPath)
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
            }));

            itemContainer.Add(rowContainer);

            if (GetExpandedState(node.FullPath) && node.Children.Count > 0)
            {
                var childrenContainer = new VisualElement();
                childrenContainer.AddToClassList(ChildrenContainerClassName);
                foreach (var child in node.Children.OrderBy(c => c.TagName))
                    if (ShouldShowTag(child))
                        childrenContainer.Add(CreateTreeItem(child, depth + 1));

                if (childrenContainer.childCount > 0) itemContainer.Add(childrenContainer);
            }

            return itemContainer;
        }

        private Button CreateActionButton(string text, Action onClick, string tooltip, string extraClass = null)
        {
            var button = new Button(onClick) { text = text, tooltip = tooltip };
            button.AddToClassList(ActionButtonClassName);
            if (!string.IsNullOrEmpty(extraClass)) button.AddToClassList(extraClass);
            return button;
        }

        private bool GetExpandedState(string path)
        {
            return expandedStates.TryGetValue(path, out var expanded) && expanded;
        }

        private void SetExpandedState(string path, bool expanded)
        {
            expandedStates[path] = expanded;
        }

        private void ExpandCollapseNodeAndChildren(TagNode startNode, bool expand)
        {
            SetExpandedState(startNode.FullPath, expand);
            foreach (var child in startNode.Children) ExpandCollapseNodeAndChildren(child, expand);
            if (startNode.Children.Any()) RefreshTreeView();
        }

        private List<TagNode> BuildTagHierarchy()
        {
            var rootNodes = new Dictionary<string, TagNode>();
            var allNodes = new Dictionary<string, TagNode>();
            var asset = target as GameplayTagAsset;
            if (asset?.tagDefinitions == null) return new List<TagNode>();

            var sortedDefinitions = asset.tagDefinitions
                .Where(td => td != null && !string.IsNullOrEmpty(td.tagName))
                .OrderBy(td => td.tagName.Count(c => c == '.')).ThenBy(td => td.tagName)
                .ToList();

            foreach (var tagData in sortedDefinitions)
            {
                var parts = tagData.tagName.Split('.');
                TagNode parentNode = null;
                var currentPath = "";
                for (var i = 0; i < parts.Length; i++)
                {
                    var segment = parts[i];
                    currentPath = i == 0 ? segment : currentPath + "." + segment;
                    if (!allNodes.TryGetValue(currentPath, out var currentNode))
                    {
                        currentNode = new TagNode { TagName = segment, FullPath = currentPath };
                        allNodes[currentPath] = currentNode;
                        if (parentNode == null)
                        {
                            if (!rootNodes.ContainsKey(segment)) rootNodes[segment] = currentNode;
                        }
                        else
                        {
                            if (!parentNode.Children.Contains(currentNode)) parentNode.Children.Add(currentNode);
                            currentNode.Parent = parentNode;
                        }
                    }

                    if (currentPath == tagData.tagName) currentNode.TagData = tagData;
                    parentNode = currentNode;
                }
            }

            return rootNodes.Values.OrderBy(n => n.TagName).ToList();
        }

        private bool ShouldShowTag(TagNode node)
        {
            if (string.IsNullOrEmpty(searchFilter)) return true;
            if (node.FullPath.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return node.Children.Any(child => ShouldShowTag(child));
        }

        private void ShowAddTagWindow(string parentPath)
        {
            if (_nodeBeingRenamed != null) CancelRename(); // Cancel rename if active
            // Call the static ShowWindow method which handles Init
            AddTagWindow.ShowWindow(this, parentPath);
        }

        private void ShowEditTagWindow(TagNode node)
        {
            if (_nodeBeingRenamed != null) CancelRename(); // Cancel rename if active
            EditTagWindow.ShowWindow(this, node.TagData);
        }

        private void CreateTagAtPath(string path)
        {
            if (_nodeBeingRenamed != null) CancelRename();
            serializedObject.Update();
            var asset = target as GameplayTagAsset;
            var newTag = new GameplayTagData
                { tagName = path, description = "Newly created implicit tag", debugColor = Color.grey };
            var list = new List<GameplayTagData>(asset.tagDefinitions ?? Array.Empty<GameplayTagData>());
            if (list.Any(t => t.tagName == path))
            {
                EditorUtility.DisplayDialog("Tag Exists", $"Tag '{path}' already exists.", "OK");
                return;
            }

            list.Add(newTag);
            asset.tagDefinitions = list.ToArray();
            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            RefreshTreeView();
        }

        // --- RENAME LOGIC ---
        private void StartRename(TagNode node, Label nameLabel, TextField renameField, VisualElement actionsContainer)
        {
            if (_nodeBeingRenamed != null) CancelRename(); // Cancel any previous rename
            if (node.TagData == null) return; // Should not happen if called from valid context

            _isCommittingRenameOrCancelling = false;
            _nodeBeingRenamed = node;
            _activeRenameLabel = nameLabel;
            _activeRenameField = renameField;
            _activeRenameActionsContainer = actionsContainer;

            _activeRenameLabel.style.display = DisplayStyle.None;
            _activeRenameActionsContainer.style.display = DisplayStyle.None; // Hide actions too

            _activeRenameField.value = node.TagName;
            _activeRenameField.style.display = DisplayStyle.Flex;
            _activeRenameField.Focus();
            _activeRenameField.SelectAll();

            _activeRenameField.RegisterCallback<FocusOutEvent>(OnRenameFieldFocusOut);
            _activeRenameField.RegisterCallback<KeyDownEvent>(OnRenameFieldKeyDown);
        }

        private void OnRenameFieldFocusOut(FocusOutEvent evt)
        {
            if (_nodeBeingRenamed != null && !_isCommittingRenameOrCancelling) AttemptCommitRename();
        }

        private void OnRenameFieldKeyDown(KeyDownEvent evt)
        {
            if (_nodeBeingRenamed == null) return;
            if (evt.keyCode == KeyCode.Escape)
            {
                CancelRename();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                AttemptCommitRename();
                evt.StopPropagation();
            }
        }

        private void AttemptCommitRename()
        {
            if (_isCommittingRenameOrCancelling) return; // Prevent re-entry

            _isCommittingRenameOrCancelling = true;
            var newSegmentName = _activeRenameField.value;
            var nodeToRename = _nodeBeingRenamed; // Capture before ClearRenameState

            // Perform validation and actual rename logic
            if (string.IsNullOrWhiteSpace(newSegmentName) || newSegmentName == nodeToRename.TagName)
            {
                CancelRename(); // No change or empty, just cancel
                return;
            }

            if (!GameplayTagManager.Instance.IsValidTagName(newSegmentName)) // false for segment validation
            {
                EditorUtility.DisplayDialog("Invalid Tag Name",
                    "New tag name contains invalid characters (e.g., '.'). Use only letters, numbers, and underscores for a segment.",
                    "OK");
                // Don't cancel UI yet, let user correct it or press Esc.
                _isCommittingRenameOrCancelling = false; // Allow further interaction
                _activeRenameField.Focus(); // Refocus
                return;
            }

            var parentFullPath = nodeToRename.Parent?.FullPath;
            var siblings = nodeToRename.Parent == null
                ? BuildTagHierarchy().Where(n => n != nodeToRename)
                : nodeToRename.Parent.Children.Where(n => n != nodeToRename);
            if (siblings.Any(s => s.TagName.Equals(newSegmentName, StringComparison.OrdinalIgnoreCase)))
            {
                EditorUtility.DisplayDialog("Duplicate Tag Name",
                    $"A tag with the name '{newSegmentName}' already exists at this level.", "OK");
                _isCommittingRenameOrCancelling = false;
                _activeRenameField.Focus();
                return;
            }

            // All checks passed, proceed with rename
            CommitRename(nodeToRename, newSegmentName);
            // ClearRenameState is called by CommitRename/CancelRename
        }


        private void CommitRename(TagNode nodeToRename, string newSegmentName)
        {
            var oldOwnFullPath = nodeToRename.FullPath;
            var parentFullPath = nodeToRename.Parent?.FullPath;
            var newOwnFullPath = string.IsNullOrEmpty(parentFullPath)
                ? newSegmentName
                : $"{parentFullPath}.{newSegmentName}";

            serializedObject.Update();
            var asset = target as GameplayTagAsset;
            var definitions = new List<GameplayTagData>(asset.tagDefinitions);
            var changed = false;

            for (var i = 0; i < definitions.Count; i++)
            {
                var tagData = definitions[i];
                if (tagData.tagName == oldOwnFullPath)
                {
                    tagData.tagName = newOwnFullPath;
                    changed = true;
                }
                else if (tagData.tagName.StartsWith(oldOwnFullPath + "."))
                {
                    var restOfPath = tagData.tagName.Substring(oldOwnFullPath.Length); // Includes leading '.'
                    tagData.tagName = newOwnFullPath + restOfPath;
                    changed = true;
                }
            }

            if (changed)
            {
                asset.tagDefinitions = definitions.ToArray();
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();

                // Update expandedStates
                var newExpandedStates = new Dictionary<string, bool>();
                foreach (var kvp in expandedStates)
                {
                    var newKey = kvp.Key;
                    if (kvp.Key == oldOwnFullPath) newKey = newOwnFullPath;
                    else if (kvp.Key.StartsWith(oldOwnFullPath + "."))
                        newKey = newOwnFullPath + kvp.Key.Substring(oldOwnFullPath.Length);
                    newExpandedStates[newKey] = kvp.Value;
                }

                expandedStates = newExpandedStates;
            }

            ClearRenameState(); // Clears _nodeBeingRenamed etc.
            if (changed) RefreshTreeView(); // Refresh to show changes
            else CancelRenameUIOnly(); // Just revert UI if no actual data change but commit was called
        }

        private void CancelRenameUIOnly() // Reverts UI without triggering full refresh if no data changed
        {
            if (_activeRenameLabel != null) _activeRenameLabel.style.display = DisplayStyle.Flex;
            if (_activeRenameField != null) _activeRenameField.style.display = DisplayStyle.None;
            if (_activeRenameActionsContainer != null) _activeRenameActionsContainer.style.display = DisplayStyle.Flex;
            ClearRenameState();
        }


        private void CancelRename()
        {
            if (_nodeBeingRenamed == null && _activeRenameField == null) return; // Already cleared or never started

            _isCommittingRenameOrCancelling = true; // Prevent focusOut from re-triggering commit

            if (_activeRenameLabel != null) _activeRenameLabel.style.display = DisplayStyle.Flex;
            if (_activeRenameField != null)
            {
                _activeRenameField.style.display = DisplayStyle.None;
                _activeRenameField.UnregisterCallback<FocusOutEvent>(OnRenameFieldFocusOut);
                _activeRenameField.UnregisterCallback<KeyDownEvent>(OnRenameFieldKeyDown);
            }

            if (_activeRenameActionsContainer != null)
                _activeRenameActionsContainer.style.display = DisplayStyle.Flex; // Show actions again

            ClearRenameState();
            _isCommittingRenameOrCancelling = false;
        }

        private void ClearRenameState()
        {
            _nodeBeingRenamed = null;
            _activeRenameField = null;
            _activeRenameLabel = null;
            _activeRenameActionsContainer = null;
        }


        // --- DELETE LOGIC (largely unchanged but ensure rename state is cleared) ---
        private void DeleteTag(TagNode node)
        {
            if (_nodeBeingRenamed != null) CancelRename();
            // ... rest of delete logic from previous version
            if (node.TagData == null)
            {
                EditorUtility.DisplayDialog("Cannot Delete",
                    "This is an implicit parent node. It will be removed if all its children are removed.", "OK");
                return;
            }

            var hasChildDefinitions = HasChildTagDefinitions(node.FullPath);

            if (hasChildDefinitions)
            {
                var option = EditorUtility.DisplayDialogComplex(
                    "Delete Parent Tag",
                    $"The tag '{node.FullPath}' has child tags defined under it. What would you like to do?",
                    "Delete Tag and All Children",
                    "Cancel",
                    "Delete Tag, Move Children Up"
                );
                if (option == 1) return;
                PerformDeleteOperation(node, option);
            }
            else
            {
                if (EditorUtility.DisplayDialog("Delete Tag",
                        $"Are you sure you want to delete the tag '{node.FullPath}'?",
                        "Delete", "Cancel"))
                    PerformDeleteOperation(node, 0); // Default to delete self
            }
        }

        private void PerformDeleteOperation(TagNode nodeToDelete, int option)
        {
            serializedObject.Update();
            var asset = target as GameplayTagAsset;
            var list = new List<GameplayTagData>(asset.tagDefinitions);
            var fullPathToDelete = nodeToDelete.FullPath;

            if (option == 0)
            {
                list.RemoveAll(t => t.tagName == fullPathToDelete || t.tagName.StartsWith(fullPathToDelete + "."));
            }
            else if (option == 2)
            {
                var parentPrefix = "";
                var lastDotIndex = fullPathToDelete.LastIndexOf('.');
                if (lastDotIndex > -1) parentPrefix = fullPathToDelete.Substring(0, lastDotIndex) + ".";
                var childrenToUpdate = list.Where(t => t.tagName.StartsWith(fullPathToDelete + ".")).ToList();
                foreach (var childTag in childrenToUpdate)
                {
                    var childRemainingPath = childTag.tagName.Substring((fullPathToDelete + ".").Length);
                    childTag.tagName = parentPrefix + childRemainingPath;
                }

                list.RemoveAll(t => t.tagName == fullPathToDelete);
            }

            asset.tagDefinitions = list.ToArray();
            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            RefreshTreeView();
        }

        private bool HasChildTagDefinitions(string parentFullPath)
        {
            var asset = target as GameplayTagAsset;
            if (asset.tagDefinitions == null) return false;
            var prefix = parentFullPath + ".";
            return asset.tagDefinitions.Any(t => t != null && t.tagName.StartsWith(prefix));
        }

        private void ExpandCollapseAll(bool expand)
        {
            if (_nodeBeingRenamed != null) CancelRename();
            var currentRootTags = BuildTagHierarchy();
            Action<List<TagNode>, bool> recursiveExpandCollapse = null;
            recursiveExpandCollapse = (nodes, exp) =>
            {
                foreach (var n in nodes)
                {
                    if (n.Children.Any()) expandedStates[n.FullPath] = exp;
                    recursiveExpandCollapse(n.Children, exp);
                }
            };
            recursiveExpandCollapse(currentRootTags, expand);
            RefreshTreeView();
        }

        public class TagNode
        {
            public string TagName { get; set; }
            public string FullPath { get; set; }
            public GameplayTagData TagData { get; set; }
            public TagNode Parent { get; set; }
            public List<TagNode> Children { get; set; } = new();
        }
    }
}
#endif