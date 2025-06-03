#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameplayTag.Editor
{
    /// <summary>
    ///     Window for adding new tags, styled consistently with EditTagWindow.
    /// </summary>
    public class AddTagWindow : EditorWindow
    {
        private string _category = "";
        private Color _debugColor = Color.white; // Default to white like EditTagWindow's potential default
        private string _description = "";
        private TextField _descriptionField; // Keep a reference if needed for keydown
        private bool _isNetworked;
        private GameplayTagAssetEditor _parentEditor;
        private string _parentTagPath;
        private Button _saveButton;

        private string _tagName = "";

        // UI Elements
        private TextField _tagNameField;
        private Label _validationMessageLabel;

        private void OnEnable()
        {
            // Rebuild UI if necessary, e.g., if domain reload occurred
            // For AddTagWindow, CreateGUI is called by Unity when window is shown/enabled.
            // If _parentEditor is null here and it's needed before CreateGUI, error handling might be required.
            // However, ShowWindow should always call Init before this.
        }


        private void CreateGUI()
        {
            if (GameplayTagManager.Instance == null)
            {
                // Using a simple label for error as DisplayDialog might not work well before GUI is fully set up
                var errorLabel = new Label("GameplayTagManager not found. Please ensure it is set up correctly.")
                {
                    style =
                    {
                        color = new StyleColor(Color.red), unityTextAlign = TextAnchor.MiddleCenter, marginTop = 20
                    } // Added StyleColor
                };
                rootVisualElement.Add(errorLabel);
                rootVisualElement.Add(new Button(Close)
                    { text = "Close", style = { marginTop = 10, alignSelf = Align.Center } });
                return;
            }

            var root = rootVisualElement;
            root.style.paddingLeft = 14;
            root.style.paddingRight = 14;
            root.style.paddingTop = 16;
            root.style.paddingBottom = 12;
            root.style.flexGrow = 1; // Ensure root fills available space

            // Title
            var titleLabel = new Label("Add New Tag Properties") // Consistent naming with EditTagWindow
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 15, marginBottom = 10 }
            };
            root.Add(titleLabel);

            // Parent Path Display (styled like CreateReadOnlyField from EditTagWindow)
            if (!string.IsNullOrEmpty(_parentTagPath)) CreateReadOnlyField("Parent Path", _parentTagPath, root);

            // Tag Name Field
            _tagNameField = new TextField("Tag Name") // Removed colon for consistency with EditTagWindow PropertyFields
            {
                value = _tagName
            };
            _tagNameField.RegisterValueChangedCallback(OnTagNameChanged);
            _tagNameField.style.marginTop = 6; // Consistent spacing
            root.Add(_tagNameField);

            // Validation Message Label (for tag name)
            _validationMessageLabel = new Label
            {
                style =
                {
                    color = new StyleColor(new Color(0.9f, 0.5f, 0.5f)), // Softer red like EditTagWindow error
                    minHeight = 18, // Reserve space, adjusted
                    marginTop = 4, // Adjusted spacing
                    marginBottom = 4,
                    whiteSpace = WhiteSpace.Normal // Allow multi-line
                }
            };
            root.Add(_validationMessageLabel);

            // Description Field
            const float kDescMinHeight = 50f;
            _descriptionField = new TextField("Description") { multiline = true }; // Store reference
            _descriptionField.style.minHeight = kDescMinHeight;
            _descriptionField.style.maxHeight = 150f; // Slightly less than EditTagWindow, as it's for adding
            _descriptionField.style.flexShrink = 0;
            _descriptionField.style.whiteSpace = WhiteSpace.Normal;
            _descriptionField.style.marginTop = 6;
            _descriptionField.RegisterValueChangedCallback(evt => _description = evt.newValue);
            var descTextInputElement = _descriptionField.Q(TextField.textInputUssName); // Get the actual input element
            if (descTextInputElement != null)
                descTextInputElement.style.minHeight = kDescMinHeight; // Apply minHeight to the input element itself

            root.Add(_descriptionField);

            // Category Field
            var categoryField = new TextField("Category")
            {
                value = _category
            };
            categoryField.style.marginTop = 6;
            categoryField.RegisterValueChangedCallback(evt => _category = evt.newValue);
            root.Add(categoryField);

            // Networked Toggle and Color Field Row
            var row = new VisualElement
                { style = { flexDirection = FlexDirection.Row, marginTop = 6, alignItems = Align.FlexStart } };
            var networkToggle = new Toggle("Is Networked")
            {
                value = _isNetworked,
                style = { flexGrow = 1 } // Allow toggle to take available space
            };
            networkToggle.RegisterValueChangedCallback(evt => _isNetworked = evt.newValue);
            row.Add(networkToggle);

            var colorField = new ColorField("Debug Color")
            {
                value = _debugColor,
                style = { width = 180, marginLeft = 10 } // Consistent with EditTagWindow
            };
            colorField.RegisterValueChangedCallback(evt => _debugColor = evt.newValue);
            row.Add(colorField);
            root.Add(row);

            root.Add(new VisualElement { style = { flexGrow = 1 } }); // Spacer

            // Buttons Footer
            var footer = new VisualElement
                { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, marginTop = 14 } };
            var cancelButton = new Button(CancelAndClose) { text = "Cancel" }; // Use method group
            footer.Add(cancelButton);

            _saveButton = new Button(SaveTag) { text = "Save (Ctrl+S)" }; // Updated button text
            _saveButton.style.marginLeft = 6; // Consistent spacing
            footer.Add(_saveButton);
            root.Add(footer);

            // Initial validation and focus
            ValidateTagName(_tagName);
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            // Schedule focus to ensure the field is ready
            root.schedule.Execute(() => _tagNameField.Focus()).StartingIn(50);
        }

        public static void ShowWindow(GameplayTagAssetEditor editor, string parentPath)
        {
            var wnd = GetWindow<AddTagWindow>(true); // true for utility, non-dockable
            wnd.titleContent = new GUIContent("Add New Gameplay Tag");
            wnd.minSize =
                new Vector2(420, 320); // Adjusted to be similar to EditTagWindow, slightly taller for validation
            wnd.maxSize = new Vector2(680, 450); // Allow some flexibility
            wnd.Init(editor, parentPath);
            wnd.ShowModalUtility(); // Modal behavior
        }

        private void Init(GameplayTagAssetEditor editor, string currentParentPath)
        {
            _parentEditor = editor;
            _parentTagPath = currentParentPath;
        }

        private static void CreateReadOnlyField(string label, string value, VisualElement parent)
        {
            var box = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(.24f, .24f, .24f, .4f),
                    paddingLeft = 8, paddingRight = 8, paddingTop = 6, paddingBottom = 6,
                    marginBottom = 10, // Consistent with EditTagWindow
                    marginTop = 6 // Added for spacing after title
                }
            };
            box.Add(new Label(label + ":")
                { style = { fontSize = 10, color = new StyleColor(new Color(.7f, .7f, .7f)) } }); // Added StyleColor
            box.Add(new Label(value) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 2 } });
            parent.Add(box);
        }


        private void OnTagNameChanged(ChangeEvent<string> evt)
        {
            _tagName = evt.newValue;
            ValidateTagName(_tagName);
        }

        private void ValidateTagName(string currentTagNameSegment)
        {
            var fullTagName = string.IsNullOrEmpty(_parentTagPath)
                ? currentTagNameSegment
                : $"{_parentTagPath}.{currentTagNameSegment}";
            _validationMessageLabel.text = ""; // Clear previous message

            if (string.IsNullOrWhiteSpace(currentTagNameSegment))
            {
                _validationMessageLabel.text = "Tag name segment cannot be empty.";
                _saveButton.SetEnabled(false);
                return;
            }

            // Validate the current segment for allowed characters
            if (!GameplayTagManager.Instance.IsValidTagName(currentTagNameSegment)) // false for segment validation
            {
                _validationMessageLabel.text = "Invalid characters in segment. Use A-Z, a-z, 0-9, _";
                _saveButton.SetEnabled(false);
                return;
            }

            // Check if the full tag already exists in the asset being edited
            if (_parentEditor?.target is GameplayTagAsset asset && asset.tagDefinitions != null &&
                asset.tagDefinitions.Any(t => t.tagName.Equals(fullTagName, StringComparison.OrdinalIgnoreCase)))
            {
                _validationMessageLabel.text = $"Tag '{fullTagName}' already exists in this asset.";
                _saveButton.SetEnabled(false);
                return;
            }

            _saveButton.SetEnabled(true);
        }


        private void SaveTag()
        {
            // Re-validate before saving, especially the full path structure
            ValidateTagName(_tagName); // This updates saveButton enablement
            if (!_saveButton.enabledSelf)
            {
                _tagNameField.Focus(); // Focus field if validation failed
                return;
            }

            var fullTagName = string.IsNullOrEmpty(_parentTagPath) ? _tagName : $"{_parentTagPath}.{_tagName}";

            // Final check on the full path structure (dots, empty segments)
            if (!GameplayTagManager.Instance.IsValidTagName(fullTagName, true)) // true for full path validation
            {
                // This message should ideally be more specific based on what IsValidTagName(full, true) checks
                _validationMessageLabel.text = "Invalid full tag structure (e.g., leading/trailing dots, double dots).";
                _saveButton.SetEnabled(false); // Disable save if this specific check fails
                _tagNameField.Focus();
                return;
            }


            if (_parentEditor == null || _parentEditor.target == null)
            {
                EditorUtility.DisplayDialog("Error", "Parent editor or target asset not found.", "OK");
                Close();
                return;
            }

            var serializedObject = _parentEditor.serializedObject;
            Undo.RecordObject(_parentEditor.target,
                $"Add Tag: {fullTagName}"); // Record Undo for the asset modification

            serializedObject.Update();

            var asset = _parentEditor.target as GameplayTagAsset;
            if (asset == null)
            {
                EditorUtility.DisplayDialog("Error", "Target asset is not a GameplayTagAsset.", "OK");
                Close();
                return;
            }

            var newTag = new GameplayTagData
            {
                tagName = fullTagName,
                description = _description,
                category = _category,
                isNetworked = _isNetworked,
                debugColor = _debugColor
            };

            var list = new List<GameplayTagData>(asset.tagDefinitions ?? Array.Empty<GameplayTagData>());

            // This check should have been caught by ValidateTagName, but as a final safeguard
            if (list.Any(t => t.tagName.Equals(fullTagName, StringComparison.OrdinalIgnoreCase)))
            {
                _validationMessageLabel.text = $"Tag '{fullTagName}' already exists. Save aborted.";
                _saveButton.SetEnabled(false);
                _tagNameField.Focus();
                return;
            }

            list.Add(newTag);
            asset.tagDefinitions = list.ToArray();

            EditorUtility.SetDirty(_parentEditor.target);
            serializedObject.ApplyModifiedProperties();
            // AssetDatabase.SaveAssets(); // Usually not needed immediately

            _parentEditor.RefreshTreeView();
            Close();
        }

        private void CancelAndClose()
        {
            Close();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                CancelAndClose();
                evt.StopPropagation();
            }
            // Ctrl+S or Command+S to Save
            else if ((evt.commandKey || evt.ctrlKey) && evt.keyCode == KeyCode.S)
            {
                if (_saveButton.enabledSelf) // Only if save button is enabled
                {
                    SaveTag();
                    evt.StopPropagation();
                }
            }
        }
    }
}
#endif