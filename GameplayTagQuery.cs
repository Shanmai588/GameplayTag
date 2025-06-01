using System;
using System.Linq;

namespace GameplayTag
{
    /// <summary>
    /// Complex query for matching gameplay tags
    /// </summary>
    [Serializable]
    public class GameplayTagQuery
    {
        public GameplayTagContainer requiredTags;
        public GameplayTagContainer blockedTags;
        public GameplayTagContainer anyTags;
        public GameplayTagContainer noneOfTags;
        public EGameplayTagQueryType queryType;

        public bool Matches(GameplayTagContainer tags)
        {
            if (tags == null) return IsEmpty();
            
            switch (queryType)
            {
                case EGameplayTagQueryType.All:
                    return tags.HasAll(requiredTags) && !tags.HasAny(blockedTags);
                    
                case EGameplayTagQueryType.Any:
                    return tags.HasAny(anyTags) && !tags.HasAny(blockedTags);
                    
                case EGameplayTagQueryType.None:
                    return !tags.HasAny(noneOfTags);
                    
                case EGameplayTagQueryType.AllExact:
                    return tags.HasAllExact(requiredTags) && !tags.HasAnyExact(blockedTags);
                    
                case EGameplayTagQueryType.AnyExact:
                    return tags.HasAnyExact(anyTags) && !tags.HasAnyExact(blockedTags);
                    
                case EGameplayTagQueryType.NoneExact:
                    return !tags.HasAnyExact(noneOfTags);
                    
                default:
                    return false;
            }
        }

        public bool IsEmpty()
        {
            return (requiredTags == null || requiredTags.IsEmpty()) &&
                   (blockedTags == null || blockedTags.IsEmpty()) &&
                   (anyTags == null || anyTags.IsEmpty()) &&
                   (noneOfTags == null || noneOfTags.IsEmpty());
        }

        public string GetDescription()
        {
            var description = $"Query Type: {queryType}";
            
            if (requiredTags != null && !requiredTags.IsEmpty())
                description += $"\nRequired: {string.Join(", ", requiredTags.GetTags().Select(t => t.ToString()))}";
                
            if (blockedTags != null && !blockedTags.IsEmpty())
                description += $"\nBlocked: {string.Join(", ", blockedTags.GetTags().Select(t => t.ToString()))}";
                
            if (anyTags != null && !anyTags.IsEmpty())
                description += $"\nAny: {string.Join(", ", anyTags.GetTags().Select(t => t.ToString()))}";
                
            if (noneOfTags != null && !noneOfTags.IsEmpty())
                description += $"\nNone Of: {string.Join(", ", noneOfTags.GetTags().Select(t => t.ToString()))}";
                
            return description;
        }
    }

}