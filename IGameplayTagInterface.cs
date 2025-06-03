namespace GameplayTag
{
    /// <summary>
    ///     Interface for objects that can have gameplay tags
    /// </summary>
    public interface IGameplayTagInterface
    {
        GameplayTagContainer GetOwnedGameplayTags();
        bool HasMatchingGameplayTag(GameplayTag tag);
        bool HasAllMatchingGameplayTags(GameplayTagContainer tags);
        bool HasAnyMatchingGameplayTags(GameplayTagContainer tags);
    }
}