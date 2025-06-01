using System.Linq;
using UnityEngine;

namespace GameplayTag
{
    /// <summary>
    /// ScriptableObject asset for defining gameplay tags
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayTagAsset", menuName = "Gameplay/Tag Asset")]
    public class GameplayTagAsset : ScriptableObject
    {
        public GameplayTagData[] tagDefinitions;

        public string[] GetTagNames()
        {
            if (tagDefinitions == null) return new string[0];
            
            return tagDefinitions
                .Where(td => td != null && !string.IsNullOrEmpty(td.tagName))
                .Select(td => td.tagName)
                .ToArray();
        }

        public void ValidateAsset()
        {
            if (tagDefinitions == null) return;
            
            foreach (var tagData in tagDefinitions)
            {
                if (tagData != null && !GameplayTagManager.Instance.IsValidTagName(tagData.tagName))
                {
                    Debug.LogWarning($"Invalid tag name: {tagData.tagName} in asset {name}");
                }
            }
        }
    }

}