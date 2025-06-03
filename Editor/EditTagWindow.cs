#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameplayTag.Editor
{
    public sealed class EditTagWindow : EditorWindow
    {
        private readonly List<string> _children = new();

        private readonly List<string> _parents = new();
        private SerializedObject _assetSO;
        private GameplayTagData _editingData;

        private GameplayTagAssetEditor _host;
        private SerializedProperty _tagSO;
        private int _undoGroup = -1;

        private void OnEnable()
        {
            if (_host != null && _editingData != null && _host.target != null) // Ensure host.target also exists
            {
                // It's possible _editingData is stale if the tag was renamed/deleted
                // while window was closed. ResolveSerializedProperties handles finding it.
                ResolveSerializedProperties(); // This will try to find _tagSO again
                RebuildUI();
            }
            else if (_editingData != null) // If host is lost, we probably can't recover state well
            {
                // If the window was open, _editingData might persist but _host might be lost
                // In this case, it's hard to re-establish SerializedObject context.
                // The current RebuildUI will show "Tag not found" or similar if _tagSO is null.
                RebuildUI(); // Will likely show error if _tagSO can't be resolved
            }
        }

        // ... (Static ShowWindow and other fields remain the same) ...
        public static void ShowWindow(GameplayTagAssetEditor host, GameplayTagData data)
        {
            var wnd = GetWindow<EditTagWindow>();
            wnd.titleContent = new GUIContent($"Edit Tag – {data.tagName}");
            wnd.minSize = new Vector2(420, 280); // Adjusted min height if related tags can be empty
            wnd.maxSize = new Vector2(680, 760);
            wnd.Init(host, data);
            wnd.ShowUtility(); // ShowUtility is good for auxiliary windows
        }

        private void Init(GameplayTagAssetEditor host, GameplayTagData data)
        {
            _host = host;
            _editingData = data;
            _undoGroup = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName($"Edit Tag: {data.tagName}"); // More specific undo name
            Undo.RegisterCompleteObjectUndo(_host.target, $"Edit Tag: {data.tagName}");

            ResolveSerializedProperties();
            RebuildUI();
        }

        private void ResolveSerializedProperties()
        {
            // Reset _tagSO in case this is a re-resolution
            _tagSO = null;
            if (_host == null || _editingData == null || _host.target == null) return;

            _assetSO = new SerializedObject(_host.target);
            var listProp = _assetSO.FindProperty("tagDefinitions");
            if (listProp == null || !listProp.isArray)
            {
                Debug.LogError("EditTagWindow: 'tagDefinitions' property not found or not an array.");
                _assetSO = null; // Invalidate assetSO if fundamental property is missing
                return;
            }

            for (var i = 0; i < listProp.arraySize; i++)
            {
                var element = listProp.GetArrayElementAtIndex(i);
                var nameField = element.FindPropertyRelative("tagName");
                if (nameField != null && nameField.stringValue == _editingData.tagName)
                {
                    _tagSO = element;
                    break;
                }
            }

            if (_tagSO == null)
            {
                // Don't log warning here, RebuildUI will handle informing the user.
                // This state is valid if tag was deleted/renamed.
            }
        }

        private void RebuildUI()
        {
            var root = rootVisualElement;
            root.Clear();

            // Always unregister first to prevent multiple registrations on OnEnable/RebuildUI
            root.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);


            if (_tagSO == null || _assetSO == null) // Also check _assetSO
            {
                var errorLabel = new Label(_assetSO == null
                    ? "Asset not found or invalid."
                    : $"Tag '{_editingData?.tagName ?? "Unknown"}' not found.\nIt may have been renamed or deleted.")
                {
                    style =
                    {
                        color = new StyleColor(new Color(0.9f, 0.5f, 0.5f)), // Softer red
                        marginTop = 20,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        whiteSpace = WhiteSpace.Normal,
                        flexGrow = 1 // Try to take up space
                    }
                };
                root.Add(errorLabel);
                // Add a close button if the tag is not found, as there's nothing to do.
                var closeButtonContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row, justifyContent = Justify.Center, marginTop = 14,
                        position = Position.Absolute, bottom = 10, left = 0, right = 0
                    }
                };
                closeButtonContainer.Add(new Button(Close) { text = "Close" });
                root.Add(closeButtonContainer);
                return; // Stop UI construction
            }

            root.style.paddingLeft = 14;
            root.style.paddingRight = 14;
            root.style.paddingTop = 16;
            root.style.paddingBottom = 12;
            root.style.flexGrow = 1; // Ensure root fills available space

            root.Add(new Label("Edit Tag Properties")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 15, marginBottom = 10 }
            });

            CreateReadOnlyField("Tag Path", _editingData.tagName, root);

            var descProp = _tagSO.FindPropertyRelative("description");
            var categoryProp = _tagSO.FindPropertyRelative("category");
            var networkProp = _tagSO.FindPropertyRelative("isNetworked");
            var colorProp = _tagSO.FindPropertyRelative("debugColor");

            const float kDescMinHeight = 50f;
            var descField = new TextField("Description") { multiline = true };
            descField.style.minHeight = kDescMinHeight;
            // descField.style.height     = kDescMinHeight; // Allow natural height based on minHeight and content
            descField.style.maxHeight = 200f; // Prevent excessive growth
            descField.style.flexShrink = 0;
            descField.style.whiteSpace = WhiteSpace.Normal;
            descField.BindProperty(descProp);
            var textInput = descField.Q(TextField.textInputUssName); // More robust way to get text input
            if (textInput != null) textInput.style.minHeight = kDescMinHeight; // For placeholder alignment
            // textInput.style.height = kDescMinHeight; // Allow natural height
            root.Add(descField);

            var categoryField = new PropertyField(categoryProp, "Category");
            categoryField.style.marginTop = 6; // Using margin for spacing
            root.Add(categoryField);

            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginTop = 6, alignItems = Align.FlexStart }
            }; // Align items to start for multi-line toggle label
            var networkToggle = new PropertyField(networkProp, "Is Networked");
            networkToggle.style.flexGrow = 1; // Allow toggle to take available space
            row.Add(networkToggle);

            var colorField = new ColorField("Debug Color")
                { style = { width = 180, marginLeft = 10 } }; // Slightly narrower to fit well
            colorField.BindProperty(colorProp);
            row.Add(colorField);
            root.Add(row);

            DrawRelatedTagsSection(root);

            root.Add(new VisualElement { style = { flexGrow = 1 } }); // Spacer

            var footer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, marginTop = 14 }
            }; // Align buttons to right
            footer.Add(new Button(Cancel) { text = "Cancel" }); // Cancel on left
            footer.Add(new Button(Save) { text = "Save (Enter)", style = { marginLeft = 6 } }); // Save on right
            root.Add(footer);

            root.Bind(_assetSO);
            // Register callback on the rootVisualElement itself.
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }


        private static void CreateReadOnlyField(string label, string value, VisualElement parent)
        {
            var box = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(.24f, .24f, .24f, .4f),
                    paddingLeft = 8, paddingRight = 8, paddingTop = 6, paddingBottom = 6,
                    marginBottom = 10
                }
            };
            box.Add(new Label(label + ":") { style = { fontSize = 10, color = new Color(.7f, .7f, .7f) } });
            box.Add(new Label(value) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 2 } });
            parent.Add(box);
        }

        private void DrawRelatedTagsSection(VisualElement root)
        {
            GatherRelatedTags();
            if (_parents.Count == 0 && _children.Count == 0)
                // Adjust minSize if there are no related tags to save space.
                // This should ideally be done before ShowWindow or by tracking state.
                // For simplicity, we'll keep minSize, or you can add a placeholder.
                return;

            root.Add(new VisualElement
            {
                style = { height = 1, backgroundColor = new Color(.32f, .32f, .32f), marginTop = 12, marginBottom = 8 }
            });
            root.Add(new Label("Related Tags")
                { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 } });

            var listScrollView = new ScrollView
            {
                style =
                {
                    maxHeight = 130, minHeight = 40, backgroundColor = new Color(0.2f, 0.2f, 0.2f, .25f),
                    paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 4
                }
            };
            AddSectionToScrollView("Parents", _parents, '▲', listScrollView);
            AddSectionToScrollView("Children", _children, '▼', listScrollView, listScrollView.childCount > 0);
            root.Add(listScrollView);
        }

        // Helper for DrawRelatedTagsSection to add items to the ScrollView
        private void AddSectionToScrollView(string title, List<string> items, char prefix, ScrollView scrollView,
            bool addGap = false)
        {
            if (items.Count == 0) return;
            if (addGap) scrollView.Add(new VisualElement { style = { height = 6 } }); // Increased gap for clarity

            var sectionContainer = new VisualElement(); // Container for title and items
            sectionContainer.Add(new Label(title + ":")
            {
                style = { fontSize = 10, color = new Color(.62f, .62f, .62f), marginBottom = 2 }
            }); // Small margin below title
            foreach (var itm in items)
            {
                var itemLabel = new Label($"  {prefix} {itm}");
                itemLabel.style.marginLeft = 8;
                // Optional: Add tooltip or clickability here
                // itemLabel.tooltip = $"View tag: {itm}";
                // itemLabel.RegisterCallback<MouseDownEvent>(evt => PingOrOpenTag(itm));
                sectionContainer.Add(itemLabel);
            }

            scrollView.Add(sectionContainer);
        }


        private void GatherRelatedTags()
        {
            _parents.Clear();
            _children.Clear();
            if (_host?.target is not GameplayTagAsset asset || _editingData == null) return;

            var currentTagName = _editingData.tagName;
            var parts = currentTagName.Split('.');
            for (var i = 0; i < parts.Length - 1; i++)
            {
                var parentPath = string.Join('.', parts.Take(i + 1));
                // Show parent path regardless of whether it's an explicit tag, for context.
                // Or, stick to only explicit: if (asset.tagDefinitions.Any(t => t.tagName == parentPath))
                _parents.Add(parentPath);
            }

            var childPrefix = currentTagName + ".";
            foreach (var tag in asset.tagDefinitions)
            {
                if (tag == null || string.IsNullOrEmpty(tag.tagName) || tag == _editingData) continue;
                if (tag.tagName.StartsWith(childPrefix)) _children.Add(tag.tagName);
            }

            _parents.Sort();
            _children.Sort(); // Ensure consistent order
        }

        private void Save()
        {
            if (_assetSO == null || _tagSO == null) return; // Should not happen if UI is built correctly
            _assetSO.ApplyModifiedProperties();
            if (_undoGroup >= 0) Undo.CollapseUndoOperations(_undoGroup); // Check _undoGroup just in case
            EditorUtility.SetDirty(_assetSO.targetObject);
            // AssetDatabase.SaveAssets(); // Usually not needed immediately, Unity saves on exit/Ctrl+S globally
            _host?.RefreshTreeView();
            Close();
        }

        private void Cancel()
        {
            if (_undoGroup >= 0) Undo.RevertAllDownToGroup(_undoGroup);
            // No need to collapse, as we are discarding these changes from the group.
            Close();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Cancel();
                evt.StopPropagation();
            }
            // Only save if not currently focusing a text field (to allow Enter for new lines in description)
            else if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
                if (!(evt.target is TextField) || !((TextField)evt.target).multiline)
                {
                    Save();
                    evt.StopPropagation();
                }
            }
            else if ((evt.commandKey || evt.ctrlKey) && evt.keyCode == KeyCode.S)
            {
                Save();
                evt.StopPropagation();
            }
        }
    }
}
#endif