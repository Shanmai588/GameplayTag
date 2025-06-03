using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
// Added for Regex

namespace GameplayTag
{
    /// <summary>
    ///     Singleton manager for all gameplay tags in the project
    /// </summary>
    public class GameplayTagManager
    {
        private static GameplayTagManager instance;
        private GameplayTag rootTag;
        private readonly Dictionary<string, GameplayTag> tagMap;

        private GameplayTagManager()
        {
            tagMap = new Dictionary<string, GameplayTag>();
            IsInitialized = false;
            // rootTag should be initialized in Initialize() to ensure clean state if Initialize() is called multiple times.
        }

        public bool IsInitialized { get; private set; }

        public static GameplayTagManager Instance
        {
            get
            {
                if (instance == null) instance = new GameplayTagManager();
                // Consider if Initialize() should be called here or explicitly by game setup.
                return instance;
            }
        }

        public event Action<GameplayTag> OnTagRegistered;

        public void Initialize()
        {
            if (IsInitialized) return;

            // Clear existing tags if re-initializing, though typically Initialize is called once.
            tagMap.Clear();

            rootTag = new GameplayTag("Root"); // Assuming GameplayTag constructor handles this name.
            // The rootTag itself isn't usually added to the public tagMap unless explicitly named "Root".
            // If "Root" is a reserved name not meant to be user-creatable via assets, that's fine.
            // For simplicity, let's assume "Root" is an internal concept.

            IsInitialized = true; // Set true after basic setup

            // Load all GameplayTagAssets from Resources
            // Ensure the path "Resources/GameplayTags" is where your assets are.
            var assets = Resources.LoadAll<GameplayTagAsset>("GameplayTags");
            foreach (var asset in assets) LoadTagsFromAsset(asset);

            Debug.Log($"GameplayTagManager initialized with {tagMap.Count} tags from {assets.Length} assets.");
        }

        public GameplayTag RequestGameplayTag(string tagName)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("GameplayTagManager accessed before Initialize. Attempting to initialize now.");
                Initialize(); // Auto-initialize if not ready, common for singletons.
            }

            if (string.IsNullOrEmpty(tagName))
            {
                Debug.LogWarning("Requested GameplayTag with null or empty name.");
                return null;
            }

            if (tagMap.TryGetValue(tagName, out var existingTag)) return existingTag;

            // If the tag is not in the map, and it's a valid name,
            // this implies it wasn't defined in any asset but is being requested.
            // Depending on desired behavior, you might create it on-the-fly or return null.
            // The current CreateGameplayTag handles on-the-fly creation.
            Debug.LogWarning(
                $"GameplayTag '{tagName}' requested but not found in pre-loaded tags. Attempting to create dynamically.");
            return CreateGameplayTag(tagName);
        }

        public GameplayTag CreateGameplayTag(string tagName)
        {
            if (!IsInitialized) Initialize(); // Ensure manager is ready

            // Validate the full tag name structure first.
            // The IsValidTagName used by AddTagWindow is for UI validation.
            // Here, we ensure the name makes sense for the runtime map.
            if (!IsValidTagName(tagName, true)) // Assume tagName here is a full path
            {
                Debug.LogError($"Attempted to create GameplayTag with invalid name: {tagName}");
                return null;
            }

            if (tagMap.TryGetValue(tagName, out var existingTag)) return existingTag; // Already exists

            var parts = tagName.Split('.');
            var parent = rootTag; // Assume rootTag is non-null after Initialize()
            var currentPath = "";

            for (var i = 0; i < parts.Length; i++)
            {
                // Reconstruct path carefully to match dictionary keys
                currentPath = string.IsNullOrEmpty(currentPath) ? parts[i] : $"{currentPath}.{parts[i]}";

                if (!tagMap.TryGetValue(currentPath, out var currentTag))
                {
                    // Ensure parent is valid. If rootTag was null, this could fail.
                    if (parent == null && currentPath != "Root")
                    {
                        Debug.LogError($"Cannot create tag '{currentPath}' because rootTag is not initialized.");
                        return null; // Or handle rootTag initialization more robustly
                    }

                    currentTag =
                        new GameplayTag(parts[i],
                            parent); // GameplayTag needs to handle its name and parent relationship
                    tagMap[currentPath] = currentTag;
                    OnTagRegistered?.Invoke(currentTag);
                    // Debug.Log($"Dynamically created and registered tag: {currentPath}");
                }

                parent = currentTag;
            }

            return tagMap[tagName]; // Return the final tag
        }

        /// <summary>
        ///     Validates a tag name segment or a full tag path.
        ///     A segment is a single part of a tag (e.g., "Player", "Ability").
        ///     A full path can be hierarchical (e.g., "Player.Ability.Jump").
        /// </summary>
        /// <param name="name">The tag name or segment to validate.</param>
        /// <param name="isFullPath">True if 'name' should be validated as a full path, false for a segment.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public bool
            IsValidTagName(string name,
                bool isFullPath =
                    false) // Default isFullPath to false for backward compatibility if only one arg is passed elsewhere
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (isFullPath)
            {
                // Rules for full path:
                // 1. No leading or trailing dots.
                // 2. No consecutive dots (empty segments).
                // 3. Each segment must be a valid tag segment.
                if (name.StartsWith(".") || name.EndsWith(".")) return false;
                if (name.Contains("..")) return false;

                var segments = name.Split('.');
                // After splitting, if any segment is empty (e.g. "Tag..Other" already caught by "Contains("..")")
                // or just whitespace (shouldn't happen if IsValidTagNameSegment is robust), it's invalid.
                // IsValidTagNameSegment will handle whitespace within a segment.
                return segments.All(IsValidTagNameSegment);
            }

            // Rules for a single segment:
            // 1. Must not contain any dots.
            // 2. Must adhere to segment character rules (e.g., starts with letter, then alphanumeric/underscore).
            if (name.Contains(".")) return false; // Segments cannot contain dots
            return IsValidTagNameSegment(name);
        }

        /// <summary>
        ///     Validates a single segment of a gameplay tag.
        ///     Segments must start with a letter and be followed by letters, numbers, or underscores.
        ///     They cannot contain dots or be empty/whitespace.
        /// </summary>
        private bool IsValidTagNameSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment)) return false;

            // Regex: Starts with a letter, followed by zero or more letters, digits, or underscores.
            // This is consistent with common programming naming conventions for identifiers.
            return Regex.IsMatch(segment, @"^[a-zA-Z][a-zA-Z0-9_]*$");
        }


        public GameplayTag[] GetAllTags()
        {
            if (!IsInitialized) Initialize();
            // Exclude the internal rootTag if it's not meant to be a user-facing tag.
            // Assuming GameplayTag class has a GetTagName() or similar method.
            // If rootTag is in tagMap and named "Root", this would filter it.
            return tagMap.Values.Where(t => t.GetTagName() != "Root")
                .ToArray(); // Assuming GameplayTag has a 'Name' property
        }

        public GameplayTag[] GetChildTags(GameplayTag parent)
        {
            if (!IsInitialized) Initialize();
            if (parent == null) return Array.Empty<GameplayTag>(); // Use Array.Empty for .NET Standard 2.1+

            return parent.GetChildren().ToArray();
        }

        public void LoadTagsFromAsset(GameplayTagAsset asset)
        {
            if (!IsInitialized)
                // This might be problematic if called before Initialize sets up rootTag.
                // However, Initialize calls this, so it should be fine in that flow.
                // If called externally, ensure Initialize() ran first.
                Debug.LogWarning(
                    "LoadTagsFromAsset called before GameplayTagManager was fully initialized. This might lead to issues if rootTag is not set.");

            if (asset == null || asset.tagDefinitions == null)
            {
                Debug.LogWarning("Attempted to load tags from a null asset or asset with no definitions.");
                return;
            }

            foreach (var tagData in asset.tagDefinitions)
                if (tagData != null && !string.IsNullOrEmpty(tagData.tagName))
                    // CreateGameplayTag will handle hierarchical creation and add to tagMap.
                    // It also performs validation.
                    CreateGameplayTag(tagData.tagName);
        }
    }
}