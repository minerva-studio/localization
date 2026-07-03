# Localization

一个轻量的 Unity 本地化 package，用于运行时翻译、本地化 UI 文本、对象本地化 context、语言文件资产，以及编辑器侧的 localization 管理。

## 功能特性

| 功能 | 说明 |
| --- | --- |
| 键值表本地化 | 通过 key 获取文本，支持模块化文件、多来源合并，以及 `game.ui.start` 这类层级 key path。 |
| 运行时翻译 API | `L10n` 统一处理初始化、语言加载、翻译、缺失 key、运行时 override 与默认 region 读取。 |
| 结构化参数 | `L10nParams` 区分 key option 段和动态变量，用于替代容易混淆的 legacy `params string[]` 写法。 |
| 动态变量与 context | `L10nContext` / `ILocalizableContext` 为对象绑定 key root，并为占位符提供运行时变量。 |
| 表达式解析 | 本地化文本支持 `{value}`、`{a + b}`、`{value:0.0}`，以及带参数的动态变量。 |
| 内联引用 | `$Key.Path$` / `$@Key.Path$` 可以嵌入其他本地化 key，并参与 tooltip import 行为。 |
| 颜色标记 | `§Rtext§`、`§#ff8800text§`、`§<Keyword>text§` 会转换为 TMP/UGUI 兼容的 `<color>` tag。 |
| UI 组件 | 内置 localizer 支持 TextMeshPro 与 legacy `UnityEngine.UI.Text`。 |
| 编辑器工具 | 支持 `.yml` / `.lang` 导入、key 管理、CSV 导入导出、排序、缺失 key 等编辑器工作流。 |
| 扩展点 | 项目可以提供自定义 formatter、context、颜色 resolver 和缺失 key 行为。 |

## 安装要求

- Unity `2021.3` 或更新版本
- 能够从 `Assets/Resources` 加载资源的 Unity 项目
- 本 package 需要的 Unity utility 依赖
- 如果使用 TextMeshPro localizer 组件，需要 TextMeshPro

`package.json` 中声明的包信息：

```json
{
  "name": "com.minervagamestudio.localization",
  "version": "0.4.0",
  "displayName": "Localization",
  "unity": "2021.3"
}
```

## 安装方式

在 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.minervagamestudio.localization": "https://github.com/minerva-studio/localization.git"
  }
}
```

也可以在 Unity 中通过 Package Manager 安装：

```text
Window > Package Manager > + > Add package from git URL...
```

然后粘贴：

```text
https://github.com/minerva-studio/localization.git
```

## 快速开始

### 1. 创建 Localization Manager

从 Unity 资产菜单创建 `L10nDataManager`：

```text
Create > Localization > Localization Manager
```

Manager 负责管理：

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

### 2. 把 Manager 赋给 Localization Settings

Package 会从以下路径加载项目设置：

```text
Assets/Resources/Localization/LocalizationSetting.asset
```

打开 `LocalizationSetting` asset，把 `L10nDataManager` 赋给它的 `manager` 字段。

运行时，`LocalizationSettings` 会从 `Resources` 加载这个 asset，并初始化 `L10n`。

### 3. 创建语言文件

语言文件可以是 `LanguageFile` asset，也可以由 `.lang` / `.yml` 文件导入。

推荐目录结构：

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

Region 字符串需要和 `L10nDataManager.regions` 中配置的值完全一致。

示例 entries：

```yml
Game.UI.MainMenu.Start.name: Start
Game.UI.MainMenu.Quit.name: Quit
Game.UI.MainMenu.Quit.msg: Are you sure you want to quit?
```

### 4. 加载 region

系统会从 `LocalizationSettings` 自动初始化；游戏也可以显式加载 region：

```csharp
using Minerva.Localizations;

L10n.Init();
L10n.Load("EN_US");
```

也可以使用指定 manager 初始化：

```csharp
L10n.InitAndLoad(manager, "EN_US");
```

语言加载后会触发 `L10n.OnLocalizationLoaded`。内置文本 localizer 会监听这个事件并刷新文本。

## 核心概念

### L10nDataManager

`L10nDataManager` 是 localization 数据的编辑器与运行时中枢。它是一个 `ScriptableObject`，可以从以下菜单创建：

```text
Create > Localization > Localization Manager
```

它负责管理支持的 regions、语言文件、源文件、key collection 重建、排序、导入导出行为、引用导入选项、tooltip 导入选项，以及缺失 key 行为。

### LocalizationSettings

`LocalizationSettings` 是项目级设置 asset，会从以下路径加载：

```text
Assets/Resources/Localization/LocalizationSetting.asset
```

它保存用于运行时初始化 localization 系统的 `L10nDataManager` 引用。

### L10n

`L10n` 是主要运行时门面。它负责：

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

Region 加载完成后会触发 `L10n.OnLocalizationLoaded`，让 UI localizer 或其他监听者刷新显示文本。

### LanguageFile

`LanguageFile` 是具体语言内容 asset。它包含 `region`、`tag`、localization `entries`，以及可选的 child files。

Master file 会合并自身 entries 与 child-file entries。若出现重复 key，后导入的 entry 会覆盖前面的 entry，并输出 warning。

## 运行时 API

### 基础翻译

```csharp
string text = L10n.Tr("Game.UI.MainMenu.Start.name", L10nParams.Empty);
```

不传 `L10nParams` 的 legacy 调用仍然可用：

```csharp
string text = L10n.Tr("Game.UI.MainMenu.Start.name");
```

### 通过 option 拼接 key

`L10nParams` 中的 option 段会追加到 base key 后面。

```csharp
string name = L10n.Tr("Game.Item.HealthPotion", L10nParams.Create("name"));
string desc = L10n.Tr("Game.Item.HealthPotion", L10nParams.Create("desc"));
```

这两个调用分别读取：

```text
Game.Item.HealthPotion.name
Game.Item.HealthPotion.desc
```

多个 option 会按顺序追加：

```csharp
string desc = L10n.Tr(
    "Game.Effect.Buff",
    L10nParams.Create("Fire", "desc")
);
```

它会读取：

```text
Game.Effect.Buff.Fire.desc
```

### 传入动态变量

```csharp
string text = L10n.Tr(
    "Game.UI.Timer.info",
    L10nParams.Create().With("seconds", 12.5f)
);
```

语言 entry：

```yml
Game.UI.Timer.info: "Time: {seconds:0.0}s"
```

### 直接翻译对象

如果某个类型已经注册了 localization context，可以直接传对象：

```csharp
string displayName = L10n.Tr(itemData, L10nParams.Create("name"));
string description = L10n.Tr(itemData, L10nParams.Create("desc"));
```

内部流程是：把对象解析成 `L10nContext.Of(object)`，生成 localization key，读取 raw content，然后解析 localization escape patterns。

### 使用指定 context 翻译

当 key 是显式给出的，但动态变量需要来自某个 context 时，可以使用 `TrKey`：

```csharp
var context = L10nContext.Of(itemData);
string text = L10n.TrKey("Game.UI.Inventory.Selected.info", context, L10nParams.Empty);
```

### 翻译 raw content

当 raw string 已经存在，但仍需要解析变量、引用或颜色标记时，使用 `TrRaw`：

```csharp
var context = L10nContext.Of(itemData);

string text = L10n.TrRaw(
    "Price: {price}",
    context,
    L10nParams.Empty
);
```

### 默认 region 翻译

使用 `TrDefault` 从默认 localization region 读取文本：

```csharp
string fallback = L10n.TrDefault("Game.UI.MainMenu.Start.name", L10nParams.Empty);
```

## L10nParams

`L10nParams` 用于区分 key option 段和动态变量。

### 空参数

```csharp
var parameters = L10nParams.Empty;
var another = L10nParams.Create();
```

### Option 段

```csharp
var nameOption = L10nParams.Create("name");
var nestedOption = L10nParams.Create("Fire", "desc");
```

### 变量

```csharp
var parameters = L10nParams
    .Create("desc")
    .With("level", 2)
    .With("damage", 12.5f);
```

也可以一次添加多个变量：

```csharp
var parameters = L10nParams
    .Create("desc")
    .With(
        ("level", 2),
        ("damage", 12.5f),
        ("cooldown", 3.0f)
    );
```

### 修改 option

```csharp
parameters = parameters.WithOptions("name");
parameters = parameters.AppendOptions("tooltip");
parameters = parameters.PrependOptions("Common");
```

### Legacy string 参数

Legacy string 参数会通过 `L10nParams.FromStrings(...)` 转换。

```csharp
L10n.Tr("Game.Item.HealthPotion", "name", "level=2");
```

转换规则：

- 不包含 `=` 的字符串会成为 option
- 包含 `=` 的字符串会成为变量

例如 `["Daily", "level=2", "desc"]` 会变成：

- Options: `["Daily", "desc"]`
- Variables: `{ "level": "2" }`

新代码建议显式使用 `L10nParams`：

```csharp
L10n.Tr(
    "Game.Item.HealthPotion",
    L10nParams.Create("name").With("level", 2)
);
```

## Localization Contexts

Localization context 用来描述对象如何映射到 localization key，以及如何提供动态变量。

### ILocalizableContext

自定义 context 可以实现 `ILocalizableContext`：

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

对于可复用的对象映射，可以继承 `L10nContext` 并注册：

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

在初始化阶段注册 context：

```csharp
L10nContext.Register<ItemL10nContext, ItemData>();
```

如果同一个 context 也要应用到子类：

```csharp
L10nContext.Register<ItemL10nContext, ItemData>(allowInheritance: true);
```

### 动态变量

`L10nContext` 可以从 context 对象解析动态变量：

```yml
Game.Item.HealthPotion.desc: "Restores {amount} HP."
```

如果 `itemData.amount` 存在，`{amount}` 可以从 `BaseValue` 解析。

也可以重写 `GetEscapeValue`：

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

### 全局动态变量

全局变量适合输入按键、平台名、玩家名等共享值：

```csharp
L10nContext.GlobalEscapeValue["Key_Confirm"] = static (_, _) => "Space";
```

语言 entry：

```yml
Game.UI.ConfirmHint.info: "Press {Key_Confirm} to continue."
```

### 局部动态变量

单个 context 可以注册临时 resolver：

```csharp
var context = L10nContext.Of(itemData);
context.LocalEscapeValue["bonus"] = static (_, _) => "10";
```

`LocalEscapeValue` 存储的是 `DynamicValueProvider` delegate，这个 delegate 返回 `string`。

## Localization String Syntax

Localization entry 会按一个轻量管线解析：

1. `$...$` / `$@...$` key 引用
2. `{...}` 动态变量与表达式
3. `§...§` 颜色标记
4. 嵌套结果，直到达到最大递归深度

最大递归深度由 `L10n.MAX_RECURSION` 控制。

### 动态变量

```yml
Game.UI.Player.Level.info: "Level: {level}"
```

运行时：

```csharp
L10n.Tr("Game.UI.Player.Level.info", L10nParams.Create().With("level", 5));
```

动态变量可以来自：

- 通过 `L10nParams.With(...)` 传入的变量
- 当前 context 提供的值
- 已注册的全局 escape-value provider

如果动态变量无法解析，输出会回退为变量名本身，或根据翻译路径记录诊断信息。

### 数字格式化

动态变量支持在 `:` 后添加格式：

```yml
Game.UI.Timer.info: "Time: {seconds:0.0}s"
```

格式会作用于最终值：

```yml
Game.Skill.Duration.info: "Duration: {windup + lifetime:0.0}s"
```

### 表达式

`{...}` 中可以写简单表达式：

```yml
Game.Skill.Cooldown.info: "Cooldown: {cooldown}s"
Game.Skill.Damage.info: "Damage: {damage:0}"
Game.Skill.Duration.info: "Total duration: {windup + lifetime:0.0}s"
```

表达式里的值可以来自 `L10nParams`、当前 context，或全局 dynamic-value provider。

### 动态变量参数

动态变量可以通过 `<...>` 接收一次性参数：

```yml
Game.Skill.Duration.info: "Level 3 duration: {duration<level=3>:0.0}s"
```

解析时：

- `duration` 是 escape key
- `<level=3>` 会被转换成 `L10nParams`
- context 可以通过 `parameters.GetVariableOrDefault("level", 0)` 读取它

示例 context：

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

### Key 引用

使用 `$Other.Key$` 嵌入另一个 localization key 的翻译结果：

```yml
Game.UI.Inventory.Empty.msg: "No $Game.Item.Generic.name$ found."
Game.Item.Generic.name: "item"
```

当引用结果需要根据 manager 的引用导入设置生成 tooltip 行为时，可以使用 `$@Other.Key$`。

示例：

```yml
docs.movement.name: "Movement Control"
docs.quickStart.msg: "See $docs.movement.name$ for details."
docs.tooltip.msg: "See $@docs.movement.name$ for details."
```

在启用 link 和 underline 导入时，`$@docs.movement.name$` 可以产生类似结果：

```html
<link=docs.movement.name><u>Movement Control</u></link>
```

### 颜色标记

使用 `§` 标记颜色：

```yml
Game.UI.Warning.msg: "§RWarning§"
Game.UI.Rarity.Legendary.name: "§#FFD700Legendary§"
Game.UI.Element.Fire.name: "§<Fire>Fire§"
```

支持的颜色 selector：

- `R`、`G`、`B` 这类单字母颜色代码
- `#FFD700` 这类 hex 颜色
- `<Fire>` 这类命名 resolver tag

颜色标记会被转换成 TextMeshPro 兼容的 `<color=...>` tag。

单字母色表：

| 标记 | 颜色 |
| --- | --- |
| `B` / `b` | `Color.blue` |
| `G` / `g` | `Color.green` |
| `R` / `r` | `Color.red` |
| `C` / `c` | `Color.cyan` |
| `M` / `m` | `Color.magenta` |
| `Y` / `y` | `Color.yellow` |
| `K` / `k` | `Color.black` |
| `W` / `w` | `Color.white` |

命名颜色会通过 `ColorResolvers.Resolve(name)` 解析。项目可以注册自己的 resolver：

```csharp
ColorResolvers.Register(ProjectColorPalette.Resolve);
```

引用、变量和颜色可以组合：

```yml
Game.UI.Help.msg: "§Y$@docs.movement.name$§"
Game.UI.Power.info: "Power: §R{power:0}§"
```

### 转义特殊字符

使用反斜杠转义特殊语法字符：

```yml
Game.UI.Example.msg: "Use \{braces\}, \$dollars\$, and \§color markers\§ literally."
```

### 组合示例

模板：

```yml
Game.Skill.Projectile.tip: "Projectile lasts {duration<level=1>:0.0}s. See $@docs.projectile.life.name$ - §Yimportant§."
docs.projectile.life.name: "Projectile Lifetime"
```

在启用 tooltip import 时，输出可以类似：

```html
Projectile lasts 2.3s. See <link=docs.projectile.life.name><u>Projectile Lifetime</u></link> - <color=yellow>important</color>.
```

## UI Components

### TextMeshPro

把 `TextLocalizer` 挂到带有 `TMP_Text` 的 GameObject 上。

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

`TextLocalizer` 需要 `TMP_Text` 组件，并会把翻译结果写入 `textField.text`。

### Legacy Unity UI Text

Legacy `UnityEngine.UI.Text` 使用 `TextLocalizerLegacyText`。

### 自定义文本 localizer

当目标文本组件是自定义类型时，可以继承 `TextLocalizerBase`：

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

`TextLocalizerBase.Load()` 会使用 `L10n.Tr(...)` 翻译已配置的 key；如果提供了 context，则使用 `L10n.TrKey(...)`。

## 语言文件

### Source `.yml`

`.yml` 适合作为 key 编写阶段的源文件。

```yml
Game.UI.Common.Confirm.name: Confirm
Game.UI.Common.Cancel.name: Cancel
Game.UI.Common.Back.name: Back
```

### Player-language `.lang`

`.lang` 文件保存某个 region 的具体翻译。

```yml
Game.UI.Common.Confirm.name: Confirm
Game.UI.Common.Cancel.name: Cancel
Game.UI.Common.Back.name: Back
```

Package 会把这些文件导入为 `LanguageFile` assets。

### LanguageFile assets

`LanguageFile` asset 包含：

- `tag`
- `region`
- `entries`
- `isMasterFile`
- `masterFile`
- `childFiles`
- `wordSpace`
- `listDelimiter`

Master file 会合并自身 entries 与 child files 的 entries。若出现重复 key，后导入的 entry 会覆盖前面的 entry，并输出 warning。

## 编辑器工作流

### 添加 key

在编辑器脚本中可以使用 `L10nDataManager.AddKey(...)` 或 `AddKeyToFile(...)`：

```csharp
manager.AddKeyToFile(
    "Game.UI.MainMenu.Start.name",
    fileTag: "UI",
    defaultValue: "Start"
);
```

### 移动或删除 key

```csharp
manager.MoveKey(oldKey, newKey);
manager.RemoveKey(key);
```

### 排序 entries

```csharp
manager.SortEntries();
```

### CSV 导入 / 导出

Manager 提供 CSV 导入导出的 context-menu 操作。导出的 CSV 会以 localization key 为行、region 为列。

### 重建 key 列表

Manager 可以从所有已注册的 files 与 sources 重建 key collection：

```csharp
manager.RebuildKeyList();
```

这适合在导入外部 localization 文件或修改注册源文件后使用。

## Key 命名建议

Package 不要求固定 key 结构。一个实用约定是：

```text
Game.[Category].[OptionalSubCategory].[Name].[Suffix]
```

示例：

```text
Game.UI.MainMenu.Start.name
Game.Item.HealthPotion.name
Game.Item.HealthPotion.desc
Game.UI.Timer.info
```

常见 suffix：

| Suffix | 用途 |
| --- | --- |
| `name` | 显示名、标签、按钮名、动作名 |
| `desc` | 对对象、动作、选项、效果的稳定描述 |
| `msg` | 面向玩家的正文消息 |
| `title` | 窗口、面板、章节标题 |
| `hint` | 教程提示或交互提示 |
| `info` | 当前状态、数值、读数、状态文本、统计块 |
| `tip` | Tooltip 文本 |

Suffix 应描述文本类型，而不是显示它的 UI 流程。例如确认窗口正文可以使用 `.msg`：

```text
Game.UI.MainMenu.Quit.msg
```

## 缺失键处理

Manager 通过 `MissingKeySolution` 控制缺失 key 行为：

| Value | Behavior |
| --- | --- |
| `RawDisplay` | 显示 raw key |
| `Empty` | 显示空字符串 |
| `ForceDisplay` | 显示生成值，通常基于 key 的最后一段 |
| `Fallback` | 回退到另一个语言 |

当 key 无法解析时会触发 `L10n.OnKeyMissing`。

## 排查问题

### Localization 没有初始化

检查这个 asset 是否存在：

```text
Assets/Resources/Localization/LocalizationSetting.asset
```

然后确认它的 `manager` 字段引用了有效的 `L10nDataManager`。

### 编辑器里有 key，但游戏里没有文本

检查：

- 目标 region 是否已经通过 `L10n.Load(region)` 加载
- key 是否存在于当前 `LanguageFile`
- 当前 `LanguageFile` 是否注册在 `L10nDataManager.files`
- 文件的 `region` 是否和加载的 region 字符串完全一致

### TextLocalizer 没有刷新

检查：

- 组件是否有非空 key
- `L10n` 是否已经加载 region
- key 是否存在于当前 localization 文件
- 对于 `TextLocalizer`，GameObject 是否也有 `TMP_Text` 组件
- 组件是否 enabled

### 动态变量输出成变量名

如果 `{value}` 输出为 `value`，说明变量没有找到。通过 `L10nParams.With(...)` 传入它，或从当前 context 提供它。

### Key 引用没有展开

检查被引用的 key 是否存在，以及 `$...$` 标记是否闭合。

### 颜色标记没有转换

检查颜色标记是否闭合：

```yml
Game.UI.Example.msg: "§RRed Text§"
```

对于嵌套颜色，优先使用 TextMeshPro 原生 color tag，而不是嵌套 `§` 标记。

## API 速查

| API | 用途 |
| --- | --- |
| `L10n.Init()` | 从 `LocalizationSettings` 初始化 |
| `L10n.Init(manager)` | 使用指定 `L10nDataManager` 初始化 |
| `L10n.Load(region)` | 加载 region |
| `L10n.InitAndLoad(manager, region)` | 初始化并加载 region |
| `L10n.Reload()` | 重新加载当前 localization |
| `L10n.Tr(key, params)` | 翻译 key |
| `L10n.Tr(context, params)` | 翻译对象或 context |
| `L10n.TrKey(key, context, params)` | 使用显式 key 和 context 翻译 |
| `L10n.TrRaw(raw, context, params)` | 解析 raw localization string |
| `L10n.TryTr(...)` | 翻译并返回诊断信息 |
| `L10n.TrDefault(...)` | 从默认 region 翻译 |
| `L10n.Contains(key)` | 检查当前 localization file |
| `L10n.Exist(key)` | 检查任意 localization file |
| `L10n.Write(key, value)` | 写入运行时 override，直到 reload |
| `L10n.OptionOf(partialKey)` | 查找可能的 key 补全 |
