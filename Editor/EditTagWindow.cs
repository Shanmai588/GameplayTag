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
    /// Overhauled editor window for editing <see cref="GameplayTagData"/> entries with full Undo/Redo and Cancel support.
    /// Includes auto-expanding description field, extra spacing, and a robust debug-color picker.
    /// </summary>
    public sealed class EditTagWindow : EditorWindow
    {
        /* ------------------------------------------------------------
         *  Static entry – called from GameplayTagAssetEditor
         * ------------------------------------------------------------*/
        public static void ShowWindow(GameplayTagAssetEditor host, GameplayTagData data)
        {
            var wnd = GetWindow<EditTagWindow>();
            wnd.titleContent = new GUIContent($"Edit Tag – {data.tagName}");
            wnd.minSize      = new Vector2(420, 400);
            wnd.maxSize      = new Vector2(680, 760);
            wnd.Init(host, data);
            wnd.ShowUtility();
        }

        /* ------------------------------------------------------------*/
        private GameplayTagAssetEditor _host;
        private GameplayTagData        _editingData;

        private SerializedObject   _assetSO;
        private SerializedProperty _tagSO;

        private int _undoGroup = -1; // first undo group created for this edit session

        /* ------------------------------------------------------------*/
        #region Init / Domain-reload safety
        private void Init(GameplayTagAssetEditor host, GameplayTagData data)
        {
            _host        = host;
            _editingData = data;

            // Snapshot for undo-on-cancel
            _undoGroup = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Edit Tag");
            Undo.RegisterCompleteObjectUndo(_host.target, "Edit Tag");

            ResolveSerializedProperties();
            RebuildUI();
        }

        private void OnEnable()
        {
            if (_host != null && _editingData != null)
            {
                ResolveSerializedProperties();
                RebuildUI();
            }
        }
        #endregion

        /* ------------------------------------------------------------*/
        #region Serialized-property plumbing
        private void ResolveSerializedProperties()
        {
            if (_host == null || _editingData == null || _host.target == null) return;

            _assetSO = new SerializedObject(_host.target);
            var listProp = _assetSO.FindProperty("tagDefinitions");
            _tagSO = null;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var element   = listProp.GetArrayElementAtIndex(i);
                var nameField = element.FindPropertyRelative("tagName");
                if (nameField.stringValue == _editingData.tagName)
                {
                    _tagSO = element;
                    break;
                }
            }
            if (_tagSO == null)
                Debug.LogWarning($"EditTagWindow: Could not find serialized element for '{_editingData.tagName}'.");
        }
        #endregion

        /* ------------------------------------------------------------*/
        #region UI construction
        private void RebuildUI()
        {
            var root = rootVisualElement;
            root.Clear();
            if (_tagSO == null)
            {
                root.Add(new Label("Tag not found – was it renamed or deleted?")
                {
                    style = { color = Color.red, marginTop = 10 }
                });
                return;
            }

            root.style.paddingLeft   = 14;
            root.style.paddingRight  = 14;
            root.style.paddingTop    = 16;
            root.style.paddingBottom = 12;
            root.style.flexGrow      = 1;

            /* Title */
            root.Add(new Label("Edit Tag Properties")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 15, marginBottom = 10 }
            });

            /* Read-only tag path */
            CreateReadOnlyField("Tag Path", _editingData.tagName, root);

            /* Bound property fields */
            var descProp     = _tagSO.FindPropertyRelative("description");
            var categoryProp = _tagSO.FindPropertyRelative("category");
            var networkProp  = _tagSO.FindPropertyRelative("isNetworked");
            var colorProp    = _tagSO.FindPropertyRelative("debugColor");

            /* Description – multi-line with auto-expand */
            const float kDescMinHeight = 50f;
            var descField = new TextField("Description") { multiline = true };
            descField.style.minHeight  = kDescMinHeight;
            descField.style.height     = kDescMinHeight;
            descField.style.flexShrink = 0;
            descField.style.whiteSpace = WhiteSpace.Normal;
            descField.BindProperty(descProp);

            // internal input styling so the caret starts centred vertically
            var textInput = descField.Q("unity-text-input");
            if (textInput != null)
            {
                textInput.style.minHeight = kDescMinHeight;
                textInput.style.height    = kDescMinHeight;
            }

            root.Add(descField);

            /* Spacer between Description and Category */
            root.Add(new VisualElement { style = { height = 6 } });

            /* Category */
            var categoryField = new PropertyField(categoryProp, "Category");
            root.Add(categoryField);

            /* Network + Debug-color row */
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 6 } };
            row.Add(new PropertyField(networkProp, "Is Networked") { style = { flexGrow = 1 } });

            var colorField = new ColorField("Debug Color")
            {
                style = { width = 200, marginLeft = 10 }
            };
            colorField.BindProperty(colorProp);
            row.Add(colorField);
            root.Add(row);

            /* Related tags */
            DrawRelatedTagsSection(root);

            /* Spacer */
            root.Add(new VisualElement { style = { flexGrow = 1 } });

            /* Footer */
            var footer = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.Center, marginTop = 14 } };
            footer.Add(new Button(Save) { text = "Save (Enter)" });
            footer.Add(new Button(Cancel) { text = "Cancel", style = { marginLeft = 6 } });
            root.Add(footer);

            /* Bind + shortcuts */
            root.Bind(_assetSO);
            root.parent?.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        private static void CreateReadOnlyField(string label, string value, VisualElement parent)
        {
            var box = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(.24f, .24f, .24f, .4f),
                    paddingLeft            = 8,
                    paddingRight           = 8,
                    paddingTop             = 6,
                    paddingBottom          = 6,
                    marginBottom           = 10,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius= 3,
                    borderTopLeftRadius    = 3,
                    borderTopRightRadius   = 3
                }
            };
            box.Add(new Label(label + ":") { style = { fontSize = 10, color = new Color(.7f, .7f, .7f) } });
            box.Add(new Label(value)        { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 2 } });
            parent.Add(box);
        }
        #endregion

        /* ------------------------------------------------------------*/
        #region Related tags helper
        private readonly List<string> _parents  = new();
        private readonly List<string> _children = new();

        private void DrawRelatedTagsSection(VisualElement root)
        {
            GatherRelatedTags();
            if (_parents.Count == 0 && _children.Count == 0) return;

            root.Add(new VisualElement { style = { height = 1, backgroundColor = new Color(.32f, .32f, .32f), marginTop = 8, marginBottom = 8 } });
            root.Add(new Label("Related Tags") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 } });

            var list = new ScrollView { style = { maxHeight = 130, backgroundColor = new Color(0.2f, 0.2f, 0.2f, .25f), paddingLeft = 6, paddingRight = 6 } };
            AddSection("Parents",  _parents,  '▲');
            AddSection("Children", _children, '▼', true);
            root.Add(list);

            void AddSection(string title, List<string> items, char prefix, bool gap = false)
            {
                if (items.Count == 0) return;
                if (gap && list.childCount > 0) list.Add(new VisualElement { style = { height = 4 } });
                list.Add(new Label(title + ":") { style = { fontSize = 10, color = new Color(.62f, .62f, .62f) } });
                foreach (var itm in items)
                    list.Add(new Label($"  {prefix} {itm}") { style = { marginLeft = 8 } });
            }
        }

        private void GatherRelatedTags()
        {
            _parents.Clear();
            _children.Clear();
            if (_host?.target is not GameplayTagAsset asset) return;

            string tagName = _editingData.tagName;
            var parts = tagName.Split('.');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var p = string.Join('.', parts.Take(i + 1));
                if (asset.tagDefinitions.Any(t => t.tagName == p)) _parents.Add(p);
            }

            foreach (var tag in asset.tagDefinitions)
            {
                if (tag is null || tag == _editingData) continue;
                if (tag.tagName.StartsWith(tagName + '.')) _children.Add(tag.tagName);
            }
        }
        #endregion

        /* ------------------------------------------------------------*/
        #region Save / Cancel / Shortcuts
        private void Save()
        {
            if (_assetSO == null) return;
            _assetSO.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(_undoGroup);
            EditorUtility.SetDirty(_assetSO.targetObject);
            _host?.RefreshTreeView();
            Close();
        }

        private void Cancel()
        {
            if (_undoGroup >= 0)
            {
                Undo.RevertAllDownToGroup(_undoGroup);
                Undo.CollapseUndoOperations(_undoGroup);
            }
            Close();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Cancel();
                evt.StopPropagation();
            }
            else if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
                Save();
                evt.StopPropagation();
            }
            else if ((evt.commandKey || evt.ctrlKey) && evt.keyCode == KeyCode.S)
            {
                Save();
                evt.StopPropagation();
            }
        }
        #endregion
    }
}
#endif