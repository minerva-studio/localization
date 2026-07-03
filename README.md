# Localization

A lightweight Unity localization package for runtime translation, localized UI text, object-based localization contexts, language-file assets, and editor-side localization management.

## Features

| Feature | Description |
| --- | --- |
| Key-value localization | Fetch text by key, with modular files, merged sources, and hierarchical key paths such as `game.ui.start`. |
| Runtime translation API | `L10n` handles initialization, language loading, translation, missing keys, runtime overrides, and default-region reads. |
| Structured parameters | `L10nParams` separates key option segments from dynamic variables and replaces ambiguous legacy `params string[]` usage. |
| Dynamic values and contexts | `L10nContext` / `ILocalizableContext` bind objects to key roots and provide runtime values for placeholders. |
| Expression parsing | Localization text supports `{value}`, `{a + b}`, `{value:0.0}`, and parameterized dynamic values. |
| Inline references | `$Key.Path$` / `$@Key.Path$` embed other localized strings and can participate in tooltip import behavior. |
| Color tags | `§Rtext§`, `§#ff8800text§`, and `§<Keyword>text§` convert to TMP/UGUI-compatible `<color>` tags. |
| UI components | Built-in localizers support TextMeshPro and legacy `UnityEngine.UI.Text`. |
| Editor tools | `.yml` / `.lang` import, key management, CSV import/export, sorting, and missing-key workflows are available in editor code. |
| Extension points | Projects can provide custom formatters, contexts, color resolvers, and missing-key behavior. |

## Requirements

- Unity `2021.3` or newer
- A Unity project that can load assets from `Assets/Resources`
- The Unity utility dependencies required by this package
- TextMeshPro, if using the TextMeshPro localizer component

Package manifest:

```json
{
  "name": "com.minervagamestudio.localization",
  "version": "0.4.0",
  "displayName": "Localization",
  "unity": "2021.3"
}
```

## Installation

Add the package to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.minervagamestudio.localization": "https://github.com/minerva-studio/localization.git"
  }
}
```

You can also install it from Unity:

```text
Window > Package Manager > + > Add package from git URL...
```

Then paste:

```text
https://github.com/minerva-studio/localization.git
```

## Quick Start

### 1. Create a Localization Manager

Create a `L10nDataManager` asset from Unity's asset menu:

```text
Create > Localization > Localization Manager
```

The manager owns:

- `defaultRegion`
- `regions`
- `files`
- `sources`
- `missingKeySolution`
- `referenceImportOption`
- `tooltipImportOption`
- `useUnderlineResolver`
- `wordSpace`
- `listDelimiter`

### 2. Assign the Manager to Localization Settings

The package loads project settings from:

```text
Assets/Resources/Localization/LocalizationSetting.asset
```

Open the `LocalizationSetting` asset and assign the `L10nDataManager` to its `manager` field.

At runtime, `LocalizationSettings` loads this asset from `Resources` and initializes `L10n`.

### 3. Create Language Files

A language file is a `LanguageFile` asset or an imported `.lang` / `.yml` file.

Suggested layout:

```text
Assets/
  Resources/
    Localization/
      LocalizationSetting.asset
      Base/
        Lang_UI.yml
      EN_US/
        Lang_UI_EN_US.lang
      ZH_CN/
        Lang_UI_ZH_CN.lang
```

Use the exact region strings configured in `L10nDataManager.regions`.

Example entries:

```yml
Game.UI.MainMenu.Start.name: Start
Game.UI.MainMenu.Quit.name: Quit
Game.UI.MainMenu.Quit.msg: Are you sure you want to quit?
```

### 4. Load a Region

The system auto-initializes from `LocalizationSettings`, but a game can explicitly load a region:

```csharp
using Minerva.Localizations;

L10n.Init();
L10n.Load("EN_US");
```

To initialize from a specific manager:

```csharp
L10n.InitAndLoad(manager, "EN_US");
```

When a language is loaded, `L10n.OnLocalizationLoaded` is invoked. Built-in text localizers listen to this event and refresh themselves.

## Core Concepts

### L10nDataManager

`L10nDataManager` is the editor and runtime hub for localization data. It is a `ScriptableObject` created from:

```text
Create > Localization > Localization Manager
```

It manages supported regions, language files, source files, key collection rebuilding, sorting, import/export behavior, reference import options, tooltip import options, and missing-key behavior.

### LocalizationSettings

`LocalizationSettings` is the project-level settings asset loaded from:

```text
Assets/Resources/Localization/LocalizationSetting.asset
```

It stores the active `L10nDataManager` reference used to initialize the localization system at runtime.

### L10n

`L10n` is the main runtime facade. It handles:

- `Init`
- `Load`
- `Reload`
- `Tr`
- `TrKey`
- `TrRaw`
- `TryTr`
- `TrDefault`
- `Contains`
- `Exist`
- `Write`
- `OptionOf`

After a region is loaded, `L10n.OnLocalizationLoaded` is raised so UI localizers and other listeners can refresh displayed text.

### LanguageFile

`LanguageFile` is the concrete language content asset. It contains a `region`, a `tag`, localization `entries`, and optional child files.

Master files merge their own entries and child-file entries. If duplicate keys exist, the later imported entry overrides the earlier one and a warning is logged.

## Runtime API

### Basic Translation

```csharp
string text = L10n.Tr("Game.UI.MainMenu.Start.name", L10nParams.Empty);
```

Legacy calls without `L10nParams` are still supported:

```csharp
string text = L10n.Tr("Game.UI.MainMenu.Start.name");
```

### Translation with Options

`L10nParams` option segments are appended to the base key.

```csharp
string name = L10n.Tr("Game.Item.HealthPotion", L10nParams.Create("name"));
string desc = L10n.Tr("Game.Item.HealthPotion", L10nParams.Create("desc"));
```

These calls resolve:

```text
Game.Item.HealthPotion.name
Game.Item.HealthPotion.desc
```

Multiple options are appended in order:

```csharp
string desc = L10n.Tr(
    "Game.Effect.Buff",
    L10nParams.Create("Fire", "desc")
);
```

This resolves:

```text
Game.Effect.Buff.Fire.desc
```

### Translation with Variables

```csharp
string text = L10n.Tr(
    "Game.UI.Timer.info",
    L10nParams.Create().With("seconds", 12.5f)
);
```

Language entry:

```yml
Game.UI.Timer.info: "Time: {seconds:0.0}s"
```

### Object-Based Translation

If a type has a registered localization context, it can be translated directly:

```csharp
string displayName = L10n.Tr(itemData, L10nParams.Create("name"));
string description = L10n.Tr(itemData, L10nParams.Create("desc"));
```

Internally, this resolves the object to `L10nContext.Of(object)`, builds the localization key, reads raw content, and evaluates localization escape patterns.

### Explicit Context Translation

Use `TrKey` when the key is explicit but dynamic values should come from a context:

```csharp
var context = L10nContext.Of(itemData);
string text = L10n.TrKey("Game.UI.Inventory.Selected.info", context, L10nParams.Empty);
```

### Raw Content Translation

Use `TrRaw` when the raw string is already available but still needs variables, references, or color tags resolved:

```csharp
var context = L10nContext.Of(itemData);

string text = L10n.TrRaw(
    "Price: {price}",
    context,
    L10nParams.Empty
);
```

### Default-Region Translation

Use `TrDefault` to read from the default localization region:

```csharp
string fallback = L10n.TrDefault("Game.UI.MainMenu.Start.name", L10nParams.Empty);
```

## L10nParams

`L10nParams` separates key option segments from dynamic variables.

### Empty Parameters

```csharp
var parameters = L10nParams.Empty;
var another = L10nParams.Create();
```

### Option Segments

```csharp
var nameOption = L10nParams.Create("name");
var nestedOption = L10nParams.Create("Fire", "desc");
```

### Variables

```csharp
var parameters = L10nParams
    .Create("desc")
    .With("level", 2)
    .With("damage", 12.5f);
```

Multiple variables can be added at once:

```csharp
var parameters = L10nParams
    .Create("desc")
    .With(
        ("level", 2),
        ("damage", 12.5f),
        ("cooldown", 3.0f)
    );
```

### Updating Options

```csharp
parameters = parameters.WithOptions("name");
parameters = parameters.AppendOptions("tooltip");
parameters = parameters.PrependOptions("Common");
```

### Legacy String Parameters

Legacy string parameters are converted with `L10nParams.FromStrings(...)`.

```csharp
L10n.Tr("Game.Item.HealthPotion", "name", "level=2");
```

Conversion rules:

- strings without `=` become options
- strings with `=` become variables

For example, `["Daily", "level=2", "desc"]` becomes:

- Options: `["Daily", "desc"]`
- Variables: `{ "level": "2" }`

Prefer explicit `L10nParams` in new code:

```csharp
L10n.Tr(
    "Game.Item.HealthPotion",
    L10nParams.Create("name").With("level", 2)
);
```

## Localization Contexts

Localization contexts define how objects map to localization keys and dynamic values.

### ILocalizableContext

A custom context can implement `ILocalizableContext`:

```csharp
using Minerva.Localizations;

public sealed class ItemL10nContext : ILocalizableContext
{
    private readonly ItemData item;

    public ItemL10nContext(ItemData item)
    {
        this.item = item;
    }

    public string BaseKeyString => $"Game.Item.{item.ID}";

    public object GetEscapeValue(string escapeKey, L10nParams parameters)
    {
        return escapeKey switch
        {
            "level" => item.Level,
            "rarity" => item.Rarity,
            _ => escapeKey
        };
    }
}
```

### L10nContext

For reusable object mappings, derive from `L10nContext` and register it:

```csharp
using Minerva.Localizations;

public sealed class ItemL10nContext : L10nContext
{
    private ItemData item;

    protected override void Parse(object value)
    {
        item = (ItemData)value;
        BaseKeyString = $"Game.Item.{item.ID}";
        BaseValue = item;
    }
}
```

Register the context during initialization:

```csharp
L10nContext.Register<ItemL10nContext, ItemData>();
```

Use `allowInheritance: true` when the same context should apply to child classes:

```csharp
L10nContext.Register<ItemL10nContext, ItemData>(allowInheritance: true);
```

### Dynamic Values

`L10nContext` can resolve dynamic values from the context object:

```yml
Game.Item.HealthPotion.desc: "Restores {amount} HP."
```

If `itemData.amount` exists, `{amount}` can be resolved from `BaseValue`.

You can also override `GetEscapeValue`:

```csharp
public override object GetEscapeValue(string escapeKey, L10nParams parameters)
{
    switch (escapeKey)
    {
        case "level":
            return parameters.GetVariableOrDefault("level", 0);

        case "power":
            int level = parameters.GetVariableOrDefault("level", 0);
            return item.GetPower(level);

        default:
            return base.GetEscapeValue(escapeKey, parameters);
    }
}
```

### Global Dynamic Values

Global values are useful for input labels, platform names, player names, and other shared values:

```csharp
L10nContext.GlobalEscapeValue["Key_Confirm"] = static (_, _) => "Space";
```

Language entry:

```yml
Game.UI.ConfirmHint.info: "Press {Key_Confirm} to continue."
```

### Local Dynamic Values

A single context can register temporary resolvers:

```csharp
var context = L10nContext.Of(itemData);
context.LocalEscapeValue["bonus"] = static (_, _) => "10";
```

`LocalEscapeValue` stores `DynamicValueProvider` delegates, and the delegate returns `string`.

## Localization String Syntax

Localization entries are resolved through a small pipeline:

1. `$...$` / `$@...$` key references
2. `{...}` dynamic values and expressions
3. `§...§` color tags
4. nested results, until the maximum recursion depth is reached

The maximum recursion depth is controlled by `L10n.MAX_RECURSION`.

### Dynamic Values

```yml
Game.UI.Player.Level.info: "Level: {level}"
```

Runtime:

```csharp
L10n.Tr("Game.UI.Player.Level.info", L10nParams.Create().With("level", 5));
```

Dynamic values can be resolved from:

- variables passed through `L10nParams.With(...)`
- values provided by the active context
- registered global escape-value providers

If a dynamic value cannot be resolved, the output falls back to the variable name or records diagnostics, depending on the translation path.

### Number Formatting

Dynamic values support a format segment after `:`.

```yml
Game.UI.Timer.info: "Time: {seconds:0.0}s"
```

The format is applied to the final value:

```yml
Game.Skill.Duration.info: "Duration: {windup + lifetime:0.0}s"
```

### Expressions

`{...}` can contain simple expressions:

```yml
Game.Skill.Cooldown.info: "Cooldown: {cooldown}s"
Game.Skill.Damage.info: "Damage: {damage:0}"
Game.Skill.Duration.info: "Total duration: {windup + lifetime:0.0}s"
```

Expression values can come from `L10nParams`, the active context, or global dynamic-value providers.

### Dynamic Value Parameters

Dynamic values can receive one-off parameters with `<...>`:

```yml
Game.Skill.Duration.info: "Level 3 duration: {duration<level=3>:0.0}s"
```

When this is parsed:

- `duration` is the escape key
- `<level=3>` is converted into `L10nParams`
- the context can read it with `parameters.GetVariableOrDefault("level", 0)`

Example context:

```csharp
public sealed class SkillL10nContext : L10nContext
{
    private SkillData skill;

    protected override void Parse(object value)
    {
        skill = (SkillData)value;
        BaseKeyString = $"Game.Skill.{skill.ID}";
        BaseValue = skill;
    }

    public override object GetEscapeValue(string escapeKey, L10nParams parameters)
    {
        int level = parameters.GetVariableOrDefault("level", skill.Level);

        switch (escapeKey)
        {
            case "duration":
                return skill.GetDuration(level);

            case "cooldown":
                return skill.GetCooldown(level);

            default:
                return base.GetEscapeValue(escapeKey, parameters);
        }
    }
}
```

### Key References

Use `$Other.Key$` to embed another localized string:

```yml
Game.UI.Inventory.Empty.msg: "No $Game.Item.Generic.name$ found."
Game.Item.Generic.name: "item"
```

Use `$@Other.Key$` when the reference should be imported with tooltip behavior according to the manager's reference import settings.

Example:

```yml
docs.movement.name: "Movement Control"
docs.quickStart.msg: "See $docs.movement.name$ for details."
docs.tooltip.msg: "See $@docs.movement.name$ for details."
```

With link and underline import enabled, `$@docs.movement.name$` can produce a result similar to:

```html
<link=docs.movement.name><u>Movement Control</u></link>
```

### Color Tags

Use `§` tags for color formatting:

```yml
Game.UI.Warning.msg: "§RWarning§"
Game.UI.Rarity.Legendary.name: "§#FFD700Legendary§"
Game.UI.Element.Fire.name: "§<Fire>Fire§"
```

Supported color selectors:

- single-letter color codes such as `R`, `G`, `B`
- hex colors such as `#FFD700`
- named resolver tags such as `<Fire>`

Color tags are converted to TextMeshPro-compatible `<color=...>` tags.

Single-letter colors:

| Marker | Color |
| --- | --- |
| `B` / `b` | `Color.blue` |
| `G` / `g` | `Color.green` |
| `R` / `r` | `Color.red` |
| `C` / `c` | `Color.cyan` |
| `M` / `m` | `Color.magenta` |
| `Y` / `y` | `Color.yellow` |
| `K` / `k` | `Color.black` |
| `W` / `w` | `Color.white` |

Named colors are resolved through `ColorResolvers.Resolve(name)`. A project can register a custom resolver:

```csharp
ColorResolvers.Register(ProjectColorPalette.Resolve);
```

References, variables, and colors can be combined:

```yml
Game.UI.Help.msg: "§Y$@docs.movement.name$§"
Game.UI.Power.info: "Power: §R{power:0}§"
```

### Escaping Syntax Characters

Use a backslash to escape special syntax characters:

```yml
Game.UI.Example.msg: "Use \{braces\}, \$dollars\$, and \§color markers\§ literally."
```

### Combined Example

Template:

```yml
Game.Skill.Projectile.tip: "Projectile lasts {duration<level=1>:0.0}s. See $@docs.projectile.life.name$ - §Yimportant§."
docs.projectile.life.name: "Projectile Lifetime"
```

With tooltip import enabled, the output can look like:

```html
Projectile lasts 2.3s. See <link=docs.projectile.life.name><u>Projectile Lifetime</u></link> - <color=yellow>important</color>.
```

## UI Components

### TextMeshPro

Attach `TextLocalizer` to a GameObject with `TMP_Text`.

```csharp
using Minerva.Localizations.Components;

public class Example : MonoBehaviour
{
    public TextLocalizer localizer;

    private void Start()
    {
        localizer.Load("Game.UI.MainMenu.Start.name");
    }
}
```

`TextLocalizer` requires a `TMP_Text` component and writes the translated result into `textField.text`.

### Legacy Unity UI Text

Use `TextLocalizerLegacyText` for legacy `UnityEngine.UI.Text`.

### Custom Text Localizer

Derive from `TextLocalizerBase` when the target text component is custom:

```csharp
using Minerva.Localizations.Components;

public sealed class MyTextLocalizer : TextLocalizerBase
{
    public MyTextComponent target;

    public override void SetDisplayText(string text)
    {
        target.Value = text;
    }
}
```

`TextLocalizerBase.Load()` translates the assigned key with `L10n.Tr(...)` or `L10n.TrKey(...)` when a context is provided.

## Language Files

### Source `.yml`

`.yml` files are useful as source files for key authoring.

```yml
Game.UI.Common.Confirm.name: Confirm
Game.UI.Common.Cancel.name: Cancel
Game.UI.Common.Back.name: Back
```

### Player-Language `.lang`

`.lang` files hold concrete translations for a region.

```yml
Game.UI.Common.Confirm.name: Confirm
Game.UI.Common.Cancel.name: Cancel
Game.UI.Common.Back.name: Back
```

The package imports these files into `LanguageFile` assets.

### LanguageFile Assets

`LanguageFile` assets contain:

- `tag`
- `region`
- `entries`
- `isMasterFile`
- `masterFile`
- `childFiles`
- `wordSpace`
- `listDelimiter`

Master files merge their own entries and child-file entries. If duplicate keys exist, the later imported entry overrides the earlier one and a warning is logged.

## Editor Workflow

### Add a Key

Use `L10nDataManager.AddKey(...)` or `AddKeyToFile(...)` from editor scripts:

```csharp
manager.AddKeyToFile(
    "Game.UI.MainMenu.Start.name",
    fileTag: "UI",
    defaultValue: "Start"
);
```

### Move or Remove a Key

```csharp
manager.MoveKey(oldKey, newKey);
manager.RemoveKey(key);
```

### Sort Entries

```csharp
manager.SortEntries();
```

### Export / Import CSV

The manager has context-menu actions for CSV export/import. CSV export creates a table where rows are localization keys and columns are regions.

### Rebuild Key List

The manager can rebuild its key collection from all registered files and sources:

```csharp
manager.RebuildKeyList();
```

This is useful after importing external localization files or changing the registered source files.

## Key Naming Guidelines

The package does not require a specific key shape. A practical convention is:

```text
Game.[Category].[OptionalSubCategory].[Name].[Suffix]
```

Examples:

```text
Game.UI.MainMenu.Start.name
Game.Item.HealthPotion.name
Game.Item.HealthPotion.desc
Game.UI.Timer.info
```

Common suffixes:

| Suffix | Use for |
| --- | --- |
| `name` | Display names, labels, button/action names |
| `desc` | Stable descriptions of objects, actions, options, or effects |
| `msg` | Player-facing message body text |
| `title` | Window, panel, or section titles |
| `hint` | Tutorial hints or interaction hints |
| `info` | Context-specific labels, values, readouts, status text, or stat blocks |
| `tip` | Tooltip text |

Use the suffix to describe the kind of text, not the UI flow that displays it. For example, confirmation window body text can use `.msg`:

```text
Game.UI.MainMenu.Quit.msg
```

## Missing Keys

The manager controls missing-key behavior through `MissingKeySolution`:

| Value | Behavior |
| --- | --- |
| `RawDisplay` | Display the raw key |
| `Empty` | Display an empty string |
| `ForceDisplay` | Display a generated value, usually based on the last part of the key |
| `Fallback` | Fallback to another language |

`L10n.OnKeyMissing` is invoked when a key cannot be resolved.

## Troubleshooting

### Localization Did Not Initialize

Check that this asset exists:

```text
Assets/Resources/Localization/LocalizationSetting.asset
```

Then verify that its `manager` field references a valid `L10nDataManager`.

### Key Appears in Editor but Not in Game

Check that:

- the target region is loaded with `L10n.Load(region)`
- the key exists in the active `LanguageFile`
- the active `LanguageFile` is registered in `L10nDataManager.files`
- the file's `region` matches the loaded region string exactly

### TextLocalizer Does Not Update

Check that:

- the component has a non-empty key
- `L10n` has loaded a region
- the key exists in the active localization files
- for `TextLocalizer`, the GameObject also has a `TMP_Text` component
- the component is enabled

### Dynamic Value Prints the Variable Name

If `{value}` prints as `value`, the variable was not found. Pass it through `L10nParams.With(...)` or provide it from the active context.

### Key Reference Does Not Expand

Check that the referenced key exists and that the `$...$` marker is closed.

### Color Tag Is Not Converted

Check that the tag is closed:

```yml
Game.UI.Example.msg: "§RRed Text§"
```

For nested colors, prefer TextMeshPro color tags instead of nesting `§` tags.

## API Reference

| API | Purpose |
| --- | --- |
| `L10n.Init()` | Initialize from `LocalizationSettings` |
| `L10n.Init(manager)` | Initialize from a specific `L10nDataManager` |
| `L10n.Load(region)` | Load a region |
| `L10n.InitAndLoad(manager, region)` | Initialize and load in one call |
| `L10n.Reload()` | Reload current localization |
| `L10n.Tr(key, params)` | Translate a key |
| `L10n.Tr(context, params)` | Translate an object/context |
| `L10n.TrKey(key, context, params)` | Translate with an explicit key and context |
| `L10n.TrRaw(raw, context, params)` | Resolve a raw localization string |
| `L10n.TryTr(...)` | Translate and return diagnostics |
| `L10n.TrDefault(...)` | Translate from the default region |
| `L10n.Contains(key)` | Check the current localization file |
| `L10n.Exist(key)` | Check any localization file |
| `L10n.Write(key, value)` | Runtime override until reload |
| `L10n.OptionOf(partialKey)` | Find possible key completions |
