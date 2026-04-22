---
name: create-new-feature
description: Scaffold a new Unity feature module (folders, asmdef, config, controller, state, view, container view, service) wired into this project's ServicesLocator + MVC architecture. Triggers when the user says "create a new feature", "scaffold a feature", "new feature module", or similar.
argument-hint: <FeatureName> [--service] [--controller] [--state] [--spawner] [--no-config]
allowed-tools:
  - Read
  - Write
  - Bash
  - Glob
  - Grep
  - AskUserQuestion
---

# create-new-feature

Scaffold a new feature module under `Assets/Features/<FeatureName>/` following this project's conventions:

- Service Locator pattern (`IService` + `ServicesLocator`)
- MVC split — Controller (pure C#), View (MonoBehaviour), State (immutable struct)
- ScriptableObject singleton for config (under `Resources/`)
- Assembly definition per feature (`Game.<FeatureName>.asmdef`)
- Optional ContainerView for features that spawn views (like `EnemiesContainerView`)

## Project invariants (do not violate)

- Service classes live at `Assets/Features/<Name>/Scripts/<Name>Service.cs` and implement `Core.ServicesManager.IService`. They are discovered via reflection — do NOT register them manually.
- Controllers are plain C# (no `MonoBehaviour`).
- Views are `MonoBehaviour` and subscribe to controller events.
- State is an immutable `struct` with a `With(...)` copy method.
- Config extends `Core.ScriptableObjectSingleton.ScriptableObjectSingleton<T>` and uses `[CreateAssetMenu(fileName = "<Name>Config", menuName = "Game/<Name>Config")]`.
- Private fields use `_camelCase`; public properties/methods use `PascalCase`.
- Async uses UniTask (`Cysharp.Threading.Tasks`), not coroutines. Long-running loops use `CancellationTokenSource` and are cancelled in `Reset()`.
- Unity `.meta` files MUST be generated for every created file and folder — 32-char lowercase hex GUIDs. Without them, Unity treats the files as new assets and breaks references on reimport.

## Inputs

Parse `$ARGUMENTS`:

- **First positional arg** → `FeatureName` in PascalCase (required). Reject names that don't match `^[A-Z][A-Za-z0-9]+$`.
- **Flags** (all optional; if none provided, ask the user):
  - `--service` — generate `<FeatureName>Service.cs` (for features that need to coordinate across other services or run init logic)
  - `--controller` — generate `<FeatureName>Controller.cs` + `<FeatureName>State.cs` (game logic + runtime state)
  - `--state` — generate state struct only (implies controller if neither set)
  - `--spawner` — generate `<FeatureName>ContainerView.cs` in addition to `<FeatureName>View.cs` (for features that spawn multiple view instances at runtime, like Enemies)
  - `--no-config` — skip the `<FeatureName>Config.cs` ScriptableObject

If `FeatureName` is missing OR no flags were provided, use `AskUserQuestion` with these questions in one batch:

1. **"Feature name?"** — free-form if not already supplied.
2. **"Include a Service?"** — Yes / No — "A Service coordinates initialization, registers with ServicesLocator, and exposes controllers to other features (e.g. EntitiesService, WeaponsService). Skip for presentation-only features (e.g. Biomes)."
3. **"Include a Controller + State?"** — Yes / No — "A Controller holds game logic (no MonoBehaviour); State is an immutable struct snapshot. Needed when the feature has runtime behavior driven by events (e.g. HeroController, EnemiesController)."
4. **"Does this feature spawn objects at runtime?"** — Yes / No — "Yes → adds a ContainerView that listens to controller events (OnSpawned/OnRemoved/OnChanged) and manages instantiated view prefabs. Like EnemiesContainerView."
5. **"Include a Config ScriptableObject?"** — Yes / No — "Config holds tunable parameters and lives at Assets/Resources/<Name>Config.asset. Default yes."

If the user answers No to Service, Controller, Config, and Spawner — stop and report: "Nothing to generate. At minimum a feature needs a View or a Config."

## Pre-flight checks

Before writing anything:

1. Confirm `Assets/Features/` exists. If not, this isn't the Unity RPG project — stop and report.
2. Confirm `Assets/Features/<FeatureName>/` does NOT already exist. If it does, stop and ask whether to abort or add missing pieces (do not overwrite existing files).
3. Resolve asmdef GUIDs for referenced assemblies by reading `.meta` files — do NOT hardcode. Build a map for:
   - `Core.ServicesManager` (needed if Service, Controller, ContainerView, or View subscribes to `ServicesLocator`)
   - `Core.ScriptableObjectSingleton` (needed if Config is generated)
   - `UniTask` (needed if Service or Controller is generated)
   - Any other `Game.*` asmdefs the user flags as dependencies
   
   Resolution command:
   ```bash
   find Assets -name "<TargetAsmdef>.asmdef.meta" -exec grep -h "^guid:" {} \; | head -1 | awk '{print $2}'
   ```
   
   Also scan the UniTask GUID from `Library/PackageCache/com.cysharp.unitask*/Runtime/UniTask.asmdef.meta` (it's not under `Assets/`).

4. Ask: **"Does this service/controller depend on other existing services?"** — if yes, show the list of existing `Game.*` asmdefs and let the user pick. Those GUIDs go into both the new asmdef `references` array AND the service's `GetDependencies()` return.

## Generate folder tree

Create only the folders the user's answers require:

```
Assets/Features/<FeatureName>/
├── <FeatureName>.meta                          (folder meta)
└── Scripts/
    ├── Scripts.meta
    ├── Game.<FeatureName>.asmdef
    ├── Game.<FeatureName>.asmdef.meta
    ├── <FeatureName>Service.cs                 (if --service)
    ├── <FeatureName>Service.cs.meta
    ├── Config/                                 (if config)
    │   ├── Config.meta
    │   ├── <FeatureName>Config.cs
    │   └── <FeatureName>Config.cs.meta
    ├── Controllers/                            (if --controller)
    │   ├── Controllers.meta
    │   ├── <FeatureName>Controller.cs
    │   └── <FeatureName>Controller.cs.meta
    ├── Models/                                 (if --state or --controller)
    │   ├── Models.meta
    │   └── RuntimeState/
    │       ├── RuntimeState.meta
    │       ├── <FeatureName>State.cs
    │       └── <FeatureName>State.cs.meta
    └── View/
        ├── View.meta
        ├── <FeatureName>View.cs
        ├── <FeatureName>View.cs.meta
        ├── <FeatureName>ContainerView.cs       (if --spawner)
        └── <FeatureName>ContainerView.cs.meta
```

### Folder `.meta` file format

```yaml
fileFormatVersion: 2
guid: <NEW_GUID>
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

Note: existing folder metas in this project omit `folderAsset: yes` and the importer block — matching them (minimal form) is acceptable:

```yaml
fileFormatVersion: 2
guid: <NEW_GUID>
timeCreated: <UNIX_SECONDS>
```

Use the minimal form to match this codebase.

### Script `.cs.meta` file format

```yaml
fileFormatVersion: 2
guid: <NEW_GUID>
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

### Asmdef `.asmdef.meta` file format

```yaml
fileFormatVersion: 2
guid: <NEW_GUID>
timeCreated: <UNIX_SECONDS>
```

### GUID generation

Generate 32-char lowercase hex GUIDs via Bash:

```bash
uuidgen | tr -d '-' | tr 'A-F' 'a-f'
```

Generate one GUID per file/folder. Store them so you can also put the asmdef's GUID into references later if other features need to reference this one.

## File templates

### `Game.<FeatureName>.asmdef`

```json
{
    "name": "Game.<FeatureName>",
    "rootNamespace": "Game.<FeatureName>",
    "references": [
        "<GUID_REF_1>",
        "<GUID_REF_2>"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

Each reference is `"GUID:<hex>"` format (see existing asmdefs). Always include:
- `Core.ServicesManager` if Service/Controller/ContainerView/View is generated
- `Core.ScriptableObjectSingleton` if Config is generated
- `UniTask` if Service/Controller is generated
- Any user-selected feature dependencies

### `<FeatureName>Service.cs`

If no dependencies:
```csharp
using System;
using Cysharp.Threading.Tasks;
using Core.ServicesManager;

namespace Game.<FeatureName>
{
    public class <FeatureName>Service : IService
    {
        public Type[] GetDependencies() => Array.Empty<Type>();

        // Expose controllers to other services here.
        // public <FeatureName>Controller <FeatureName>Controller { get; private set; }

        public UniTask<bool> Initialize()
        {
            // <FeatureName>Controller = new <FeatureName>Controller();
            // return <FeatureName>Controller.Initialize();
            return UniTask.FromResult(true);
        }

        public UniTask Reset() => default;
    }
}
```

If dependencies are declared, mirror `EntitiesService` — list them in `GetDependencies()`, fetch them in `Initialize`, inject into the controller.

### `<FeatureName>Controller.cs`

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.<FeatureName>
{
    public class <FeatureName>Controller
    {
        public event Action<<FeatureName>State> OnStateChanged;

        public <FeatureName>State CurrentState { get; private set; }

        private CancellationTokenSource _cancellationTokenSource;

        public UniTask<bool> Initialize()
        {
            CurrentState = new <FeatureName>State();
            _cancellationTokenSource = new CancellationTokenSource();
            return UniTask.FromResult(true);
        }

        public UniTask Reset()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            return UniTask.CompletedTask;
        }
    }
}
```

If `--spawner`, add to controller:
- `Dictionary<int, <FeatureName>State> _items`
- `int _nextId`
- Events: `OnSpawned`, `OnRemoved`, `OnPositionChanged` (or similar)
- `Spawn()` / `Remove(int id)` methods
- A `SpawnLoop` `UniTaskVoid` if spawning is time-driven

Mirror `EnemiesController` as the reference implementation.

### `<FeatureName>State.cs`

```csharp
using UnityEngine;

namespace Game.<FeatureName>
{
    public struct <FeatureName>State
    {
        public Vector3 Position { get; }

        public <FeatureName>State(Vector3 position)
        {
            Position = position;
        }

        public <FeatureName>State With(Vector3? position = null)
        {
            return new <FeatureName>State(position ?? Position);
        }
    }
}
```

If `--spawner`, include an `Id` field and expand the signature to match `EnemyState`.

### `<FeatureName>Config.cs`

```csharp
using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.<FeatureName>
{
    [CreateAssetMenu(fileName = "<FeatureName>Config", menuName = "Game/<FeatureName>Config")]
    public class <FeatureName>Config : ScriptableObjectSingleton<<FeatureName>Config>
    {
        [SerializeField]
        [Tooltip("TODO: describe this field")]
        private float exampleValue = 1f;

        public float ExampleValue => exampleValue;
    }
}
```

### `<FeatureName>View.cs`

```csharp
using Core.ServicesManager;
using UnityEngine;

namespace Game.<FeatureName>
{
    public class <FeatureName>View : MonoBehaviour
    {
        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            // Fetch controllers/services and subscribe to events here.
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
        }
    }
}
```

### `<FeatureName>ContainerView.cs` (if `--spawner`)

```csharp
using System.Collections.Generic;
using Core.ServicesManager;
using UnityEngine;

namespace Game.<FeatureName>
{
    public class <FeatureName>ContainerView : MonoBehaviour
    {
        [SerializeField] private <FeatureName>View prefab;

        private <FeatureName>Controller _controller;
        private Dictionary<int, <FeatureName>View> _views;

        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            // If the feature has a Service, fetch its controller here, e.g.:
            // _controller = ServicesLocator.Instance.GetService<<FeatureName>Service>().<FeatureName>Controller;
            _views = new Dictionary<int, <FeatureName>View>();

            // _controller.OnSpawned += OnSpawned;
            // _controller.OnRemoved += OnRemoved;
        }

        // private void OnSpawned(<FeatureName>State state)
        // {
        //     <FeatureName>View view = Instantiate(prefab, transform);
        //     view.transform.position = state.Position;
        //     _views[state.Id] = view;
        // }

        // private void OnRemoved(int id)
        // {
        //     if (_views.Remove(id, out <FeatureName>View view)) Destroy(view.gameObject);
        // }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
            // if (_controller != null)
            // {
            //     _controller.OnSpawned -= OnSpawned;
            //     _controller.OnRemoved -= OnRemoved;
            // }
        }
    }
}
```

Mirror `EnemiesContainerView` for a full reference.

## Write order

1. Generate all GUIDs first (one pass, via Bash loop).
2. Create folders (via `mkdir -p`).
3. Write folder `.meta` files.
4. Write the asmdef + its `.meta` file.
5. Write every `.cs` file and paired `.meta` file.
6. Never use `Edit` on existing files in this skill; only `Write` new ones.

## Post-generation summary

After writing, report to the user:

- Tree of created files with relative paths.
- Any TODOs they must do manually in the Unity Editor:
  - Create the `<FeatureName>Config` asset at `Assets/Resources/<FeatureName>Config.asset` (right-click → Create → Game → <FeatureName>Config).
  - Drag the generated `<FeatureName>ContainerView.cs` onto a GameObject in `MainScene.unity` if it's a spawner.
  - Assign the View prefab field on the ContainerView.
  - If this feature has a prefab, create it under `Assets/Features/<FeatureName>/View/` and assign it in the Config.
- The service (if created) is auto-registered by `ServicesLocator` via reflection — no manual wiring required.
- Unity Editor will refresh and compile on next focus. If compile fails, run `check_compile_errors` via the coplay MCP tool (if available) to debug.

## Do NOT

- Do NOT modify `ServicesLocator.cs` to register the service manually — reflection handles it.
- Do NOT put `.asset` files anywhere except `Assets/Resources/` for singleton configs.
- Do NOT create a Controller as a `MonoBehaviour` — Controllers are plain C# classes.
- Do NOT subscribe to events in `Awake()` in a View — use `Start()` + `OnAllServicesInitialized` so the Service graph is ready.
- Do NOT use `async void` — use `UniTaskVoid` + `.Forget()` (see `EnemiesController.SpawnLoop`).
- Do NOT commit changes automatically. Leave the new files unstaged so the user can review in the Unity Editor first.