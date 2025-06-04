# Gameplay Tags for Unity

A lightweight, **data-driven gameplay tag system** inspired by Unreal Engine’s tags.  
It lets you label objects, abilities, effects, and events with hierarchical tags such as  
`Character.Player.Mage` or `Effect.Fire.Burn`, query them at runtime, and replicate them
efficiently over the network.

---

## ✨ Key Features
| Feature | Description |
|---------|-------------|
| **Hierarchical tags** | Unlimited depth with fast parent/child look-ups. |
| **Containers & queries** | Perform *Has Any / Has All* (exact or recursive) checks on tag sets. |
| **Singleton manager** | Central registry validates, loads, and canonicalises tags at startup. |
| **Scriptable definitions** | Author tag catalogs in `GameplayTagAsset` ScriptableObjects. |
| **Editor tools** | Custom GameplayTag Assets Creator. |
| **Events & hooks** | Listen for tag added/removed events on any `GameplayTagComponent`. |

> Requires **Unity 2021.3 LTS** or newer.
