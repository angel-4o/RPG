# CLAUDE.md

## Project Overview
Unity 2022.3.47f1 game project ("Test - Senior Dev"). Single scene (`Assets/Scenes/MainScene.unity`), URP rendering, mobile-first landscape orientation.

## Architecture

### Service Locator Pattern
- `Core/ServicesManager/Scripts/ServicesLocator.cs` — reflection-based discovery, topological dependency sort, lifecycle management
- All services implement `IService` and declare dependencies via `GetDependencies()`
- Services are singletons; initialized in dependency order, reset on shutdown

### MVC Pattern
- **Controllers** — pure C# classes (no MonoBehaviour), own all game logic (e.g. `HeroController`, `EnemiesController`)
- **Views** — MonoBehaviours that subscribe to controller events and sync visuals
- **State** — immutable structs (e.g. `HeroState`, `EnemyState`, `JoystickState`), updated by creating new instances
- Communication between layers is event-driven (`OnStateChanged`, `OnEnemySpawned`, etc.)

### Configuration
- All game parameters live in ScriptableObject singletons under `Assets/Resources/`
- Base class: `Core/ScriptableObjectSingleton/Scripts/ScriptableObjectSingleton.cs`
- Configs: `HeroConfig`, `EnemiesConfig`, `WeaponsConfig`, `WorldConfig`, `BiomeConfig`, `JoystickInputConfig`

### Async
- UniTask (`com.cysharp.unitask` v2.5.5) replaces all coroutines
- Background loops use `CancellationTokenSource` for clean shutdown — always cancel on reset/destroy

## Feature Modules
Each feature lives under `Assets/Features/<Name>/` with its own assembly definition:

| Feature | Assembly | Purpose |
|---|---|---|
| Entities | `Game.Entities` | Hero + enemy controllers, views, configs, state |
| Weapons | `Game.Weapons` | Weapon service, configs, views |
| JoystickInput | `Game.JoystickInput` | Touch/mouse joystick input service |
| UI | `Game.UI` | Joystick UI, game over overlay |
| World | `Game.World` | World prefab instantiation, Cinemachine camera |
| Biomes | `Game.Biomes` | Biome prefab instantiation |

Core framework lives under `Assets/Core/` with `Core.ServicesManager` and `Core.ScriptableObjectSingleton` assemblies.

## Naming Conventions
- `*Service` — feature coordinator, registered in ServicesLocator
- `*Controller` — business logic (no MonoBehaviour)
- `*View` — MonoBehaviour, presentation only
- `*ContainerView` — manages a collection of view instances
- `*Config` — ScriptableObject, game data
- `*State` — immutable struct, runtime data snapshot

Namespaces mirror features: `Game.GamePlay.Entities`, `Game.GamePlay.Heroes`, `Game.GamePlay.Enemies`, `Game.Weapons`, `Game.JoystickInput`, `Game.UI`, `Game.World`, `Game.Biomes`

Private fields use `_camelCase`; public properties/methods use `PascalCase`.

## Input System
Custom joystick in `JoystickInputService` — **not** Unity's new Input System package. Uses legacy `Input` API:
- Touch input (`Input.touchCount`) on mobile
- Left mouse button fallback on desktop
- Outputs a normalized `JoystickState` struct broadcast via events

## Key Packages
- `com.cysharp.unitask` v2.5.5 — async/await
- `com.unity.render-pipelines.universal` v14.0.12 — URP
- `com.unity.cinemachine` v2.10.5 — camera follow
- `com.unity.ai.navigation` v1.1.7 — NavMesh pathfinding
- `com.unity.addressables` v1.22.3 — asset loading