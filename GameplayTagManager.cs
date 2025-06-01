using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameplayTag
{
    /// <summary>
    /// Singleton manager for all gameplay tags in the project
    /// </summary>
    public class GameplayTagManager
    {
        private static GameplayTagManager instance;
        private Dictionary<string, GameplayTag> tagMap;
        private GameplayTag rootTag;
        private bool isInitialized;
        
        public bool IsInitialized { get { return isInitialized; } }

        public static GameplayTagManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameplayTagManager();
                }
                return instance;
            }
        }

        public event Action<GameplayTag> OnTagRegistered;

        private GameplayTagManager()
        {
            tagMap = new Dictionary<string, GameplayTag>();
            isInitialized = false;
        }

        public void Initialize()
        {
            if (isInitialized) return;
            
            rootTag = new GameplayTag("Root");
            tagMap["Root"] = rootTag;
            isInitialized = true;
            
            // Load all GameplayTagAssets from Resources
            var assets = Resources.LoadAll<GameplayTagAsset>("GameplayTags");
            foreach (var asset in assets)
            {
                LoadTagsFromAsset(asset);
            }
        }

        public GameplayTag RequestGameplayTag(string tagName)
        {
            if (!isInitialized) Initialize();
            
            if (string.IsNullOrEmpty(tagName)) return null;
            
            if (tagMap.TryGetValue(tagName, out GameplayTag existingTag))
            {
                return existingTag;
            }
            
            return CreateGameplayTag(tagName);
        }

        public GameplayTag CreateGameplayTag(string tagName)
        {
            if (!IsValidTagName(tagName)) return null;
            
            string[] parts = tagName.Split('.');
            GameplayTag parent = rootTag;
            string currentPath = "";
            
            for (int i = 0; i < parts.Length; i++)
            {
                currentPath = i == 0 ? parts[i] : currentPath + "." + parts[i];
                
                if (!tagMap.TryGetValue(currentPath, out GameplayTag currentTag))
                {
                    currentTag = new GameplayTag(parts[i], parent);
                    tagMap[currentPath] = currentTag;
                    OnTagRegistered?.Invoke(currentTag);
                }
                
                parent = currentTag;
            }
            
            return tagMap[tagName];
        }

        public bool IsValidTagName(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return false;
            
            // Check for valid characters and format
            return System.Text.RegularExpressions.Regex.IsMatch(tagName, @"^[a-zA-Z][a-zA-Z0-9_.]*$");
        }

        public GameplayTag[] GetAllTags()
        {
            return tagMap.Values.Where(t => t.GetTagName() != "Root").ToArray();
        }

        public GameplayTag[] GetChildTags(GameplayTag parent)
        {
            if (parent == null) return new GameplayTag[0];
            return parent.GetChildren().ToArray();
        }

        public void LoadTagsFromAsset(GameplayTagAsset asset)
        {
            if (asset == null || asset.tagDefinitions == null) return;
            
            foreach (var tagData in asset.tagDefinitions)
            {
                if (!string.IsNullOrEmpty(tagData.tagName))
                {
                    CreateGameplayTag(tagData.tagName);
                }
            }
        }
    }
}