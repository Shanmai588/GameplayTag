using System;
using UnityEngine;

namespace GameplayTag
{
   
        /// <summary>
        /// Component for GameObjects to hold gameplay tags
        /// </summary>
        public class GameplayTagComponent : MonoBehaviour, IGameplayTagInterface
        {
            [SerializeField] private GameplayTagContainer tagContainer;
            [SerializeField] private string[] initialTagNames; // Store as strings for serialization

            public event Action<GameplayTag> OnTagAdded;
            public event Action<GameplayTag> OnTagRemoved;

            private void Awake()
            {
                if (tagContainer == null)
                {
                    tagContainer = new GameplayTagContainer();
                }

                if (initialTagNames != null)
                {
                    // Convert string tags to GameplayTag objects
                    foreach (var tagName in initialTagNames)
                    {
                        var tag = GameplayTagManager.Instance.RequestGameplayTag(tagName);
                        if (tag != null)
                        {
                            tagContainer.AddTag(tag);
                        }
                    }
                }
            }

            public void AddTag(GameplayTag tag)
            {
                if (tag == null || !tag.IsValid()) return;

                tagContainer.AddTag(tag);
                OnTagAdded?.Invoke(tag);
            }

            public void RemoveTag(GameplayTag tag)
            {
                if (tag == null) return;

                if (tagContainer.RemoveTag(tag))
                {
                    OnTagRemoved?.Invoke(tag);
                }
            }

            public bool HasTag(GameplayTag tag)
            {
                return tagContainer.HasTag(tag);
            }

            public GameplayTagContainer GetTags()
            {
                return tagContainer;
            }

            // IGameplayTagInterface implementation
            public GameplayTagContainer GetOwnedGameplayTags()
            {
                return tagContainer;
            }

            public bool HasMatchingGameplayTag(GameplayTag tag)
            {
                return tagContainer.HasTag(tag);
            }

            public bool HasAllMatchingGameplayTags(GameplayTagContainer tags)
            {
                return tagContainer.HasAll(tags);
            }

            public bool HasAnyMatchingGameplayTags(GameplayTagContainer tags)
            {
                return tagContainer.HasAny(tags);
            }
        }

    
}