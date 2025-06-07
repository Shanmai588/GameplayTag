using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace GameplayTag
{
    [TestFixture]
    public class GameplayTagTests
    {
        [SetUp]
        public void Setup()
        {
            // Reset the singleton instance before each test
            var instanceField = typeof(GameplayTagManager).GetField("instance",
                BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);

            // Initialize the manager
            GameplayTagManager.Instance.Initialize();
        }

        [Test]
        public void GameplayTag_Constructor_CreatesValidTag()
        {
            // Arrange & Act
            var tag = new GameplayTag("TestTag");

            // Assert
            Assert.IsTrue(tag.IsValid());
            Assert.AreEqual("TestTag", tag.GetTagName());
            Assert.AreEqual("TestTag", tag.GetFullTagName());
            Assert.IsNull(tag.GetParent());
            Assert.IsEmpty(tag.GetChildren());
        }

        [Test]
        public void GameplayTag_HierarchicalCreation_CreatesCorrectStructure()
        {
            // Arrange & Act
            var parentTag = new GameplayTag("Parent");
            var childTag = new GameplayTag("Child", parentTag);
            var grandchildTag = new GameplayTag("Grandchild", childTag);

            // Assert
            Assert.AreEqual("Parent", parentTag.GetFullTagName());
            Assert.AreEqual("Parent.Child", childTag.GetFullTagName());
            Assert.AreEqual("Parent.Child.Grandchild", grandchildTag.GetFullTagName());

            Assert.Contains(childTag, parentTag.GetChildren());
            Assert.Contains(grandchildTag, childTag.GetChildren());
            Assert.AreEqual(parentTag, childTag.GetParent());
            Assert.AreEqual(childTag, grandchildTag.GetParent());
        }

        [Test]
        public void GameplayTag_MatchesTag_WithHierarchy()
        {
            // Arrange
            var abilityTag = new GameplayTag("Ability");
            var fireTag = new GameplayTag("Fire", abilityTag);
            var projectileTag = new GameplayTag("Projectile", fireTag);

            // Act & Assert
            Assert.IsTrue(abilityTag.MatchesTag(projectileTag)); // Parent matches child
            Assert.IsTrue(fireTag.MatchesTag(projectileTag)); // Parent matches child
            Assert.IsFalse(projectileTag.MatchesTag(abilityTag)); // Child doesn't match parent
            Assert.IsTrue(projectileTag.MatchesTag(projectileTag)); // Self match
        }

        [Test]
        public void GameplayTag_MatchesTagExact_OnlyExactMatches()
        {
            // Arrange
            var tag1 = new GameplayTag("Ability");
            var tag2 = new GameplayTag("Ability");
            var childTag = new GameplayTag("Fire", tag1);

            // Act & Assert
            Assert.IsTrue(tag1.MatchesTagExact(tag1)); // Self match
            Assert.IsFalse(tag1.MatchesTagExact(childTag)); // No parent-child match
            Assert.IsFalse(childTag.MatchesTagExact(tag1)); // No child-parent match
        }

        [Test]
        public void GameplayTag_IsChildOf_CorrectlyIdentifiesRelationships()
        {
            // Arrange
            var rootTag = new GameplayTag("Root");
            var abilityTag = new GameplayTag("Ability", rootTag);
            var fireTag = new GameplayTag("Fire", abilityTag);
            var waterTag = new GameplayTag("Water", abilityTag);

            // Act & Assert
            Assert.IsTrue(fireTag.IsChildOf(abilityTag));
            Assert.IsTrue(fireTag.IsChildOf(rootTag));
            Assert.IsFalse(fireTag.IsChildOf(waterTag));
            Assert.IsFalse(abilityTag.IsChildOf(fireTag));
        }

        [Test]
        public void GameplayTag_Equals_HandlesNullAndSameInstance()
        {
            // Arrange
            var tag = new GameplayTag("Test");
            GameplayTag nullTag = null;

            // Act & Assert
            Assert.IsTrue(tag.Equals(tag)); // Same instance
            Assert.IsFalse(tag.Equals(nullTag)); // Null comparison
            Assert.IsFalse(tag.Equals(null)); // Null object
            Assert.IsFalse(tag.Equals("NotATag")); // Different type
        }

        [Test]
        public void GameplayTag_GetHashCode_ConsistentForEqualTags()
        {
            // Arrange
            var tag1 = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire");
            var tag2 = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire");
            var tag3 = GameplayTagManager.Instance.RequestGameplayTag("Ability.Water");

            // Act & Assert
            Assert.AreEqual(tag1.GetHashCode(), tag2.GetHashCode());
            Assert.AreNotEqual(tag1.GetHashCode(), tag3.GetHashCode());
        }

        [Test]
        public void GameplayTagContainer_AddTag_AddsTagAndParents()
        {
            // Arrange
            var container = new GameplayTagContainer();
            var abilityTag = GameplayTagManager.Instance.RequestGameplayTag("Ability");
            var fireTag = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire");
            var projectileTag = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire.Projectile");

            // Act
            container.AddTag(projectileTag);

            // Assert
            Assert.IsTrue(container.HasTagExact(projectileTag));
            Assert.IsTrue(container.HasTag(fireTag)); // Should have parent
            Assert.IsTrue(container.HasTag(abilityTag)); // Should have grandparent
            Assert.AreEqual(1, container.Count()); // Only one tag explicitly added
        }

        [Test]
        public void GameplayTagContainer_RemoveTag_RemovesOnlySpecifiedTag()
        {
            // Arrange
            var container = new GameplayTagContainer();
            var fireTag = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire");
            var projectileTag = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire.Projectile");
            container.AddTags(fireTag, projectileTag);

            // Act
            container.RemoveTag(projectileTag);

            // Assert
            Assert.IsFalse(container.HasTagExact(projectileTag));
            Assert.IsTrue(container.HasTagExact(fireTag));
        }

        [Test]
        public void GameplayTagContainer_HasTag_WithHierarchy()
        {
            // Arrange
            var container = new GameplayTagContainer();
            var projectileTag = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire.Projectile");
            container.AddTag(projectileTag);

            // Act & Assert
            Assert.IsTrue(container.HasTag(GameplayTagManager.Instance.RequestGameplayTag("Ability")));
            Assert.IsTrue(container.HasTag(GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire")));
            Assert.IsTrue(container.HasTag(projectileTag));
            Assert.IsFalse(container.HasTag(GameplayTagManager.Instance.RequestGameplayTag("Status")));
        }

        [Test]
        public void GameplayTagContainer_HasAll_ComplexScenarios()
        {
            // Arrange
            var container = new GameplayTagContainer();
            container.AddTag(GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire"));
            container.AddTag(GameplayTagManager.Instance.RequestGameplayTag("Status.Burning"));
            container.AddTag(GameplayTagManager.Instance.RequestGameplayTag("Character.Player"));

            var queryContainer1 = new GameplayTagContainer(
                GameplayTagManager.Instance.RequestGameplayTag("Ability"),
                GameplayTagManager.Instance.RequestGameplayTag("Status")
            );

            var queryContainer2 = new GameplayTagContainer(
                GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire.Projectile"),
                GameplayTagManager.Instance.RequestGameplayTag("Status.Burning")
            );

            // Act & Assert
            Assert.IsTrue(container.HasAll(queryContainer1)); // Has parent tags
            Assert.IsFalse(container.HasAll(queryContainer2)); // Doesn't have more specific child
        }

        [Test]
        public void GameplayTagContainer_HasAny_VariousScenarios()
        {
            // Arrange
            var container = new GameplayTagContainer();
            container.AddTag(GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire"));
            container.AddTag(GameplayTagManager.Instance.RequestGameplayTag("Status.Burning"));

            var queryContainer1 = new GameplayTagContainer(
                GameplayTagManager.Instance.RequestGameplayTag("Ability.Water"),
                GameplayTagManager.Instance.RequestGameplayTag("Status.Burning")
            );

            var queryContainer2 = new GameplayTagContainer(
                GameplayTagManager.Instance.RequestGameplayTag("Character.Enemy"),
                GameplayTagManager.Instance.RequestGameplayTag("Item.Weapon")
            );

            // Act & Assert
            Assert.IsTrue(container.HasAny(queryContainer1)); // Has one matching tag
            Assert.IsFalse(container.HasAny(queryContainer2)); // Has no matching tags
        }

        [Test]
        public void GameplayTagContainer_Filter_ReturnsCorrectSubset()
        {
            // Arrange
            var container = new GameplayTagContainer();
            var fireTag = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire");
            var waterTag = GameplayTagManager.Instance.RequestGameplayTag("Ability.Water");
            var burningTag = GameplayTagManager.Instance.RequestGameplayTag("Status.Burning");
            container.AddTags(fireTag, waterTag, burningTag);

            var filter = new GameplayTagContainer(
                GameplayTagManager.Instance.RequestGameplayTag("Ability")
            );

            // Act
            var filtered = container.Filter(filter);

            // Assert
            Assert.AreEqual(2, filtered.Count());
            Assert.IsTrue(filtered.HasTagExact(fireTag));
            Assert.IsTrue(filtered.HasTagExact(waterTag));
            Assert.IsFalse(filtered.HasTagExact(burningTag));
        }

        [Test]
        public void GameplayTagContainer_Clear_RemovesAllTags()
        {
            // Arrange
            var container = new GameplayTagContainer();
            container.AddTags(
                GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire"),
                GameplayTagManager.Instance.RequestGameplayTag("Status.Burning")
            );

            // Act
            container.Clear();

            // Assert
            Assert.IsTrue(container.IsEmpty());
            Assert.AreEqual(0, container.Count());
        }

        [Test]
        public void GameplayTagManager_Singleton_ReturnsSameInstance()
        {
            // Act
            var instance1 = GameplayTagManager.Instance;
            var instance2 = GameplayTagManager.Instance;

            // Assert
            Assert.AreSame(instance1, instance2);
        }

        [Test]
        public void GameplayTagManager_RequestGameplayTag_CreatesHierarchy()
        {
            // Act
            var tag = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire.Projectile");

            // Assert
            Assert.IsNotNull(tag);
            Assert.AreEqual("Ability.Fire.Projectile", tag.GetFullTagName());
            Assert.AreEqual("Projectile", tag.GetTagName());

            // Check hierarchy was created
            var fireTag = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire");
            var abilityTag = GameplayTagManager.Instance.RequestGameplayTag("Ability");

            Assert.IsNotNull(fireTag);
            Assert.IsNotNull(abilityTag);
            Assert.AreEqual(fireTag, tag.GetParent());
            Assert.AreEqual(abilityTag, fireTag.GetParent());
        }

        [Test]
        public void GameplayTagManager_RequestGameplayTag_ReturnsSameInstance()
        {
            // Act
            var tag1 = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire");
            var tag2 = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire");

            // Assert
            Assert.AreSame(tag1, tag2);
        }

        [Test]
        public void GameplayTagManager_IsValidTagName_ValidatesCorrectly()
        {
            // Act & Assert
            Assert.IsTrue(GameplayTagManager.Instance.IsValidTagName("Ability"));
            Assert.IsTrue(GameplayTagManager.Instance.IsValidTagName("Ability.Fire", true));
            Assert.IsTrue(GameplayTagManager.Instance.IsValidTagName("Ability_Fire"));
            Assert.IsTrue(GameplayTagManager.Instance.IsValidTagName("Ability123"));

            Assert.IsFalse(GameplayTagManager.Instance.IsValidTagName(""));
            Assert.IsFalse(GameplayTagManager.Instance.IsValidTagName(null));
            Assert.IsFalse(GameplayTagManager.Instance.IsValidTagName("123Ability")); // Starts with number
            Assert.IsFalse(GameplayTagManager.Instance.IsValidTagName("Ability-Fire")); // Invalid character
            Assert.IsFalse(GameplayTagManager.Instance.IsValidTagName("Ability Fire")); // Space
        }

        [Test]
        public void GameplayTagManager_GetAllTags_ReturnsAllNonRootTags()
        {
            // Arrange
            GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire");
            GameplayTagManager.Instance.RequestGameplayTag("Ability.Water");
            GameplayTagManager.Instance.RequestGameplayTag("Status.Burning");

            // Act
            var allTags = GameplayTagManager.Instance.GetAllTags();

            // Assert
            Assert.GreaterOrEqual(allTags.Length, 5); // At least: Ability, Fire, Water, Status, Burning
            Assert.IsFalse(allTags.Any(t => t.GetTagName() == "Root"));
        }

        [Test]
        public void GameplayTagManager_OnTagRegistered_FiresForNewTags()
        {
            // Arrange
            var registeredTags = new List<GameplayTag>();
            GameplayTagManager.Instance.OnTagRegistered += tag => registeredTags.Add(tag);

            // Act
            GameplayTagManager.Instance.RequestGameplayTag("Test.Event.Tag");

            // Assert
            Assert.IsTrue(registeredTags.Any(t => t.GetFullTagName() == "Test"));
            Assert.IsTrue(registeredTags.Any(t => t.GetFullTagName() == "Test.Event"));
            Assert.IsTrue(registeredTags.Any(t => t.GetFullTagName() == "Test.Event.Tag"));
        }

        [Test]
        public void GameplayTagQuery_All_RequiresAllTagsAndBlocksBlockedTags()
        {
            // Arrange
            var container = new GameplayTagContainer();
            container.AddTags(
                GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire"),
                GameplayTagManager.Instance.RequestGameplayTag("Character.Player")
            );

            var query = new GameplayTagQuery
            {
                requiredTags = new GameplayTagContainer(
                    GameplayTagManager.Instance.RequestGameplayTag("Ability"),
                    GameplayTagManager.Instance.RequestGameplayTag("Character")
                ),
                blockedTags = new GameplayTagContainer(
                    GameplayTagManager.Instance.RequestGameplayTag("Status.Stunned")
                ),
                queryType = EGameplayTagQueryType.All
            };

            // Act & Assert
            Assert.IsTrue(query.Matches(container));

            // Add blocked tag
            container.AddTag(GameplayTagManager.Instance.RequestGameplayTag("Status.Stunned"));
            Assert.IsFalse(query.Matches(container));
        }

        [Test]
        public void GameplayTagQuery_Any_RequiresAnyTag()
        {
            // Arrange
            var container = new GameplayTagContainer();
            container.AddTag(GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire"));

            var query = new GameplayTagQuery
            {
                anyTags = new GameplayTagContainer(
                    GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire"),
                    GameplayTagManager.Instance.RequestGameplayTag("Ability.Water"),
                    GameplayTagManager.Instance.RequestGameplayTag("Ability.Earth")
                ),
                queryType = EGameplayTagQueryType.Any
            };

            // Act & Assert
            Assert.IsTrue(query.Matches(container));
        }

        [Test]
        public void GameplayTagQuery_None_BlocksAllSpecifiedTags()
        {
            // Arrange
            var container = new GameplayTagContainer();
            container.AddTag(GameplayTagManager.Instance.RequestGameplayTag("Character.Player"));

            var query = new GameplayTagQuery
            {
                noneOfTags = new GameplayTagContainer(
                    GameplayTagManager.Instance.RequestGameplayTag("Status.Stunned"),
                    GameplayTagManager.Instance.RequestGameplayTag("Status.Silenced")
                ),
                queryType = EGameplayTagQueryType.None
            };

            // Act & Assert
            Assert.IsTrue(query.Matches(container));

            // Add one of the blocked tags
            container.AddTag(GameplayTagManager.Instance.RequestGameplayTag("Status.Stunned"));
            Assert.IsFalse(query.Matches(container));
        }

        [Test]
        public void GameplayTagQuery_ExactVariants_OnlyMatchExactTags()
        {
            // Arrange
            var container = new GameplayTagContainer();
            var fireTag = GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire");
            container.AddTag(fireTag);

            var queryExact = new GameplayTagQuery
            {
                anyTags = new GameplayTagContainer(
                    GameplayTagManager.Instance.RequestGameplayTag("Ability") // Parent tag
                ),
                queryType = EGameplayTagQueryType.AnyExact
            };

            var queryNormal = new GameplayTagQuery
            {
                anyTags = new GameplayTagContainer(
                    GameplayTagManager.Instance.RequestGameplayTag("Ability") // Parent tag
                ),
                queryType = EGameplayTagQueryType.Any
            };

            // Act & Assert
            Assert.IsFalse(queryExact.Matches(container)); // Exact match fails
            Assert.IsTrue(queryNormal.Matches(container)); // Hierarchical match succeeds
        }

        [Test]
        public void GameplayTagQuery_IsEmpty_DetectsEmptyQueries()
        {
            // Arrange
            var emptyQuery = new GameplayTagQuery();
            var nonEmptyQuery = new GameplayTagQuery
            {
                requiredTags = new GameplayTagContainer(
                    GameplayTagManager.Instance.RequestGameplayTag("Test")
                )
            };

            // Act & Assert
            Assert.IsTrue(emptyQuery.IsEmpty());
            Assert.IsFalse(nonEmptyQuery.IsEmpty());
        }

        [Test]
        public void GameplayTagQuery_GetDescription_ProvidesReadableDescription()
        {
            // Arrange
            var query = new GameplayTagQuery
            {
                requiredTags = new GameplayTagContainer(
                    GameplayTagManager.Instance.RequestGameplayTag("Ability.Fire")
                ),
                blockedTags = new GameplayTagContainer(
                    GameplayTagManager.Instance.RequestGameplayTag("Status.Stunned")
                ),
                queryType = EGameplayTagQueryType.All
            };

            // Act
            var description = query.GetDescription();

            // Assert
            Assert.IsTrue(description.Contains("Query Type: All"));
            Assert.IsTrue(description.Contains("Required:"));
            Assert.IsTrue(description.Contains("Ability.Fire"));
            Assert.IsTrue(description.Contains("Blocked:"));
            Assert.IsTrue(description.Contains("Status.Stunned"));
        }

        [Test]
        public void GameplayTagComponent_AddTag_FiresEvent()
        {
            // Arrange
            var gameObject = new GameObject();
            var component = gameObject.AddComponent<GameplayTagComponent>();
            GameplayTag addedTag = null;
            component.OnTagAdded += tag => addedTag = tag;

            var testTag = GameplayTagManager.Instance.RequestGameplayTag("Test.Tag");

            // Act
            component.AddTag(testTag);

            // Assert
            Assert.AreEqual(testTag, addedTag);
            Assert.IsTrue(component.HasTag(testTag));

            // Cleanup
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void GameplayTagComponent_RemoveTag_FiresEvent()
        {
            // Arrange
            var gameObject = new GameObject();
            var component = gameObject.AddComponent<GameplayTagComponent>();
            GameplayTag removedTag = null;
            component.OnTagRemoved += tag => removedTag = tag;

            var testTag = GameplayTagManager.Instance.RequestGameplayTag("Test.Tag");
            component.AddTag(testTag);

            // Act
            component.RemoveTag(testTag);

            // Assert
            Assert.AreEqual(testTag, removedTag);
            Assert.IsFalse(component.HasTag(testTag));

            // Cleanup
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void GameplayTagComponent_ImplementsInterface_Correctly()
        {
            // Arrange
            var gameObject = new GameObject();
            var component = gameObject.AddComponent<GameplayTagComponent>();
            var testTag = GameplayTagManager.Instance.RequestGameplayTag("Test.Interface");
            component.AddTag(testTag);

            // Act
            var tagInterface = component as IGameplayTagOwner;

            // Assert
            Assert.IsNotNull(tagInterface);
            Assert.IsTrue(tagInterface.HasMatchingGameplayTag(testTag));
            Assert.AreEqual(component.GetTags(), tagInterface.GetOwnedGameplayTags());

            // Cleanup
            GameObject.DestroyImmediate(gameObject);
        }

        [Test]
        public void GameplayTagAsset_GetTagNames_ReturnsValidTags()
        {
            // Arrange
            var asset = ScriptableObject.CreateInstance<GameplayTagAsset>();
            asset.tagDefinitions = new[]
            {
                new GameplayTagData { tagName = "Valid.Tag1" },
                new GameplayTagData { tagName = "Valid.Tag2" },
                new GameplayTagData { tagName = "" }, // Invalid
                null // Null entry
            };

            // Act
            var tagNames = asset.GetTagNames();

            // Assert
            Assert.AreEqual(2, tagNames.Length);
            Assert.Contains("Valid.Tag1", tagNames);
            Assert.Contains("Valid.Tag2", tagNames);

            // Cleanup
            ScriptableObject.DestroyImmediate(asset);
        }

        [Test]
        public void GameplayTag_HandlesEmptyAndNullNames()
        {
            // Arrange & Act
            var emptyTag = new GameplayTag("");
            var nullNameTag = new GameplayTag(null);

            // Assert
            Assert.IsFalse(emptyTag.IsValid());
            Assert.IsFalse(nullNameTag.IsValid());
        }

        [Test]
        public void GameplayTagContainer_HandlesNullOperations()
        {
            // Arrange
            var container = new GameplayTagContainer();
            GameplayTag nullTag = null;
            GameplayTagContainer nullContainer = null;

            // Act & Assert - Should not throw exceptions
            container.AddTag(nullTag);
            container.RemoveTag(nullTag);
            Assert.IsFalse(container.HasTag(nullTag));
            Assert.IsFalse(container.HasTagExact(nullTag));
            Assert.IsTrue(container.HasAll(nullContainer)); // Empty check returns true
            Assert.IsFalse(container.HasAny(nullContainer)); // No tags to match
        }

        [Test]
        public void GameplayTagManager_RequestInvalidTag_ReturnsNull()
        {
            // Act
            var invalidTag1 = GameplayTagManager.Instance.RequestGameplayTag("");
            var invalidTag2 = GameplayTagManager.Instance.RequestGameplayTag(null);
            var invalidTag3 = GameplayTagManager.Instance.RequestGameplayTag("123Invalid");

            // Assert
            Assert.IsNull(invalidTag1);
            Assert.IsNull(invalidTag2);
            Assert.IsNull(invalidTag3);
        }

        [Test]
        public void GameplayTagQuery_HandlesNullContainer()
        {
            // Arrange
            var query = new GameplayTagQuery
            {
                requiredTags = new GameplayTagContainer(
                    GameplayTagManager.Instance.RequestGameplayTag("Test")
                ),
                queryType = EGameplayTagQueryType.All
            };

            // Act & Assert
            Assert.IsFalse(query.Matches(null)); // Null container should not match
        }

        [Test]
        public void GameplayTag_CircularReference_Prevention()
        {
            // This test ensures the system doesn't create circular references
            // In the current implementation, this shouldn't be possible due to
            // hierarchical creation, but it's good to verify

            // Arrange
            var tag1 = GameplayTagManager.Instance.RequestGameplayTag("Circle.A");
            var tag2 = GameplayTagManager.Instance.RequestGameplayTag("Circle.A.B");
            var tag3 = GameplayTagManager.Instance.RequestGameplayTag("Circle.A.B.C");

            // Act & Assert
            // Verify no circular references
            Assert.AreNotEqual(tag1, tag3.GetParent());
            Assert.AreNotEqual(tag3, tag1.GetParent());

            // Verify correct hierarchy
            Assert.AreEqual("Circle.A", tag1.GetFullTagName());
            Assert.AreEqual("Circle.A.B", tag2.GetFullTagName());
            Assert.AreEqual("Circle.A.B.C", tag3.GetFullTagName());
        }
    }
}