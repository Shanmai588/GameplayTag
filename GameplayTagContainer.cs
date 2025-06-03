using System;
using System.Collections.Generic;
using System.Linq;

namespace GameplayTag
{
    /// <summary>
    ///     Container for multiple gameplay tags with query operations
    /// </summary>
    [Serializable]
    public class GameplayTagContainer
    {
        [NonSerialized] private HashSet<GameplayTag> parentTags;
        private HashSet<GameplayTag> tags;

        public GameplayTagContainer()
        {
            tags = new HashSet<GameplayTag>();
            parentTags = new HashSet<GameplayTag>();
        }

        public GameplayTagContainer(params GameplayTag[] tagsArray) : this()
        {
            AddTags(tagsArray);
        }

        public void AddTag(GameplayTag tag)
        {
            if (tag == null || !tag.IsValid()) return;

            tags.Add(tag);

            // Add all parent tags
            var current = tag.GetParent();
            while (current != null)
            {
                parentTags.Add(current);
                current = current.GetParent();
            }
        }

        public void AddTags(params GameplayTag[] tagsArray)
        {
            foreach (var tag in tagsArray) AddTag(tag);
        }

        public bool RemoveTag(GameplayTag tag)
        {
            if (tag == null) return false;

            var removed = tags.Remove(tag);
            if (removed) RebuildParentTags();

            return removed;
        }

        public void RemoveTags(params GameplayTag[] tagsArray)
        {
            foreach (var tag in tagsArray) tags.Remove(tag);
            RebuildParentTags();
        }

        public void Clear()
        {
            tags.Clear();
            parentTags.Clear();
        }

        public bool HasTag(GameplayTag tag)
        {
            if (tag == null) return false;

            // Check exact match
            if (tags.Contains(tag)) return true;

            // Check if any of our tags are children of the queried tag
            foreach (var ownedTag in tags)
                if (tag.MatchesTag(ownedTag))
                    return true;

            return false;
        }

        public bool HasTagExact(GameplayTag tag)
        {
            return tag != null && tags.Contains(tag);
        }

        public bool HasAny(GameplayTagContainer other)
        {
            if (other == null || other.IsEmpty()) return false;

            foreach (var tag in other.tags)
                if (HasTag(tag))
                    return true;

            return false;
        }

        public bool HasAnyExact(GameplayTagContainer other)
        {
            if (other == null || other.IsEmpty()) return false;

            foreach (var tag in other.tags)
                if (HasTagExact(tag))
                    return true;

            return false;
        }

        public bool HasAll(GameplayTagContainer other)
        {
            if (other == null || other.IsEmpty()) return true;

            foreach (var tag in other.tags)
                if (!HasTag(tag))
                    return false;

            return true;
        }

        public bool HasAllExact(GameplayTagContainer other)
        {
            if (other == null || other.IsEmpty()) return true;

            foreach (var tag in other.tags)
                if (!HasTagExact(tag))
                    return false;

            return false;
        }

        public GameplayTag[] GetTags()
        {
            return tags.ToArray();
        }

        public int Count()
        {
            return tags.Count;
        }

        public bool IsEmpty()
        {
            return tags.Count == 0;
        }

        public GameplayTagContainer Filter(GameplayTagContainer filter)
        {
            if (filter == null || filter.IsEmpty()) return new GameplayTagContainer();

            var result = new GameplayTagContainer();
            foreach (var tag in tags)
                // Check if this tag matches any tag in the filter
            foreach (var filterTag in filter.GetTags())
                if (filterTag.MatchesTag(tag))
                {
                    result.AddTag(tag);
                    break;
                }

            return result;
        }

        private void RebuildParentTags()
        {
            parentTags.Clear();
            foreach (var tag in tags)
            {
                var current = tag.GetParent();
                while (current != null)
                {
                    parentTags.Add(current);
                    current = current.GetParent();
                }
            }
        }
    }
}