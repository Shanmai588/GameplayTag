using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameplayTag
{
    /// <summary>
    ///     Represents a hierarchical gameplay tag with parent-child relationships
    /// </summary>
    [Serializable]
    public class GameplayTag : IEquatable<GameplayTag>
    {
        [SerializeField] private string tagName;
        [NonSerialized] private List<GameplayTag> children;
        [NonSerialized] private int depth;
        [NonSerialized] private GameplayTag parent;

        public GameplayTag(string name, GameplayTag parent = null)
        {
            tagName = name;
            this.parent = parent;
            children = new List<GameplayTag>();
            depth = parent != null ? parent.depth + 1 : 0;

            if (parent != null) parent.children.Add(this);
        }

        public bool Equals(GameplayTag other)
        {
            if (other == null) return false;
            return GetFullTagName() == other.GetFullTagName();
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(tagName);
        }

        public bool MatchesTag(GameplayTag other)
        {
            if (other == null) return false;

            // Check if this tag is a parent of the other tag
            var current = other;
            while (current != null)
            {
                if (current.Equals(this))
                    return true;
                current = current.parent;
            }

            return false;
        }

        public bool MatchesTagExact(GameplayTag other)
        {
            return Equals(other);
        }

        public bool IsChildOf(GameplayTag parentTag)
        {
            if (parentTag == null) return false;

            var current = parent;
            while (current != null)
            {
                if (current.Equals(parentTag))
                    return true;
                current = current.parent;
            }

            return false;
        }

        public string GetTagName()
        {
            return tagName;
        }

        public string GetFullTagName()
        {
            if (parent == null || parent.tagName == "Root")
                return tagName;

            return parent.GetFullTagName() + "." + tagName;
        }

        public GameplayTag GetParent()
        {
            return parent;
        }

        public List<GameplayTag> GetChildren()
        {
            return new List<GameplayTag>(children);
        }

        public override string ToString()
        {
            return GetFullTagName();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as GameplayTag);
        }

        public override int GetHashCode()
        {
            return GetFullTagName().GetHashCode();
        }
    }
}