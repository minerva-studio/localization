# LOM - Localization

《米埃里亚图书馆》的本地化系统

This system is capable of running in other Unity Projects, but it requires the [Unity Utilities](https://github.com/Minerva-Studio/Unity-Utilities) library.

Discord: [Join](https://discord.com/invite/pPbHMcSB7W)

---

> 一个轻量、可扩展、可在运行时/编辑器使用的 Unity 本地化系统

---

## 功能特性

- **键值表本地化**：按 key 获取文本，支持模块化、多来源合并, 提供层级键（`a.b.c`）的管理与查询
- **表达式/变量求值**: 可选的求值能力，便于在运行时进行简单计算与动态插值。
- **动态变量与上下文**：`L10nContext`、反射读取对象属性/字段、延迟计算值
- **轻量表达式**：在模板中进行简单逻辑（条件、默认值、链式属性等）
- **语言回退链**：如 `zh-CN → en`
- **可拓展**：自定义 `Formatter`、自定义 `L10nContext`、可挂接缺失键处理

---

## 目录

- [LOM - Localization](#lom---localization)
  - [功能特性](#功能特性)
  - [目录](#目录)
  - [安装 \& 要求](#安装--要求)
    - [方式 A：通过 UPM（推荐）](#方式-a通过-upm推荐)
    - [方式 B：以源码/子模块引入](#方式-b以源码子模块引入)
  - [快速开始](#快速开始)
  - [核心概念](#核心概念)
    - [L10nDataManager（数据中枢）](#l10ndatamanager数据中枢)
    - [L10n（门面 API）](#l10n门面-api)
    - [LanguageFile（语言文件模型）](#languagefile语言文件模型)
    - [典型接口（以源码为准）：](#典型接口以源码为准)
  - [内联标记](#内联标记)
    - [键引用（Inline Reference）](#键引用inline-reference)
      - [作用](#作用)
      - [语法](#语法)
      - [转义](#转义)
    - [着色（等价于 `<color>` ）](#着色等价于-color-)
      - [单字母色表](#单字母色表)
      - [示例](#示例)
      - [嵌套与组合](#嵌套与组合)
      - [转义](#转义-1)
    - [变量、表达式系统（Expressions）](#变量表达式系统expressions)
      - [特性](#特性)
      - [概览](#概览)
      - [语法与求值规则](#语法与求值规则)
      - [解析顺序](#解析顺序)
      - [错误处理](#错误处理)
      - [示例一览](#示例一览)
      - [模板语法](#模板语法)
    - [解析顺序与深度](#解析顺序与深度)
    - [示例](#示例-1)

---

## 安装 & 要求

- **Unity 版本**: Unity 2021.3 LTS
- **安装方式**：
  - 作为 Unity 包引入（UPM Git URL / 本地包）
  - 或将目录整体拷贝到你的工程

### 方式 A：通过 UPM（推荐）

1. 打开 **Package Manager**：`Window > Package Manager`
2. 点击左上角 **+** → **Add package from git URL...**
3. 粘贴：https://github.com/minerva-studio/localization.git
4. 按提示完成安装。

### 方式 B：以源码/子模块引入

- 将本仓库的 `Runtime/`（以及需要的 `Editor/`）拷贝或以 **git submodule** 的方式加入你的工程。

> **兼容性**：建议 Unity 2023.1 或更高版本。

---

## 快速开始

---

## 核心概念

### L10nDataManager（数据中枢）

- 管理当前语言、回退链、来源注册与合并、缓存与缺失键处理
- 工作流程：拉取对应语言的 LanguageFile，合并为可查询的键值表，提供给 L10n 查询

### L10n（门面 API）

- 面向业务层的简洁入口
- 内部：负责把模板交给 Parser 与 Formatter

### LanguageFile（语言文件模型）

- 建议约定：
  - key 使用分段命名：menu.file.open、npc.blacksmith.greet
  - 一文件一语言、可按模块拆分：Lang_UI.lang, Lang_NPC.lang

### 典型接口（以源码为准）：

- L10nContext（上下文/动态变量）
- 变量作用域：可注入基础类型、对象、字典、委托/函数
- 支持反射获取对象的公共属性/字段：{player.name}、{item.Price}
- 支持动态值（懒计算）：WithFunc("now", () => DateTime.Now)

---

## 内联标记

---

### 键引用（Inline Reference）

#### 作用

把 \$Key.path\$ 替换为当前语言下键 Key.path 的已渲染文本（会再次走同一套渲染流程与回退链）。
\$@Key.path\$ 是“加强版”引用：在替换为其值的同时，加上链接与下划线样式。可以结合自己实现的内联提示。

行为开关（由 L10nDataManager 控制），参考[ReferenceImportOption.cs](./Runtime/Utilities/ReferenceImportOption.cs)

- \$...\$：默认为 None
- \$@...\$：默认为 ReferenceImportOption.WithLinkTag | ReferenceImportOption.WithUnderline

#### 语法

- 基本：$key.path$、$@key.path$
- key.path 为另一条本地化键，按当前语言→回退链查找并渲染。
- 允许嵌套（见解析顺序与深度限制）。

#### 转义

- 使用反斜杠转义：\\\$ 输出字面 \$
  例："\$not-a-reference$" → $not-a-reference$

```
"See $docs.movement$ for details."
"See $@docs.movement$ for details."       // 带 <link> + 下划线
```

若 docs.movement = "Movement Control"，并且 $@...$ 打开链接与下划线，则输出：

```
"See <u><link=docs.movement>Movement Control</link></u> for details."
```

> `<link>` 的 id 为键名本身

---

### 着色（等价于 `<color>` ）

§...§ 用于给一段文本着色，等价于 TMP/UGUI 的 \<color=...\> ... \</color\>。
两种写法：

- 固定色名（单字母）：§Y ... §
- Hex 颜色：§#rrggbb ... § 或 §#rrggbbaa ... §

#### 单字母色表

下列字母（大小写等价）映射到 Unity Color：

| 标记    | 颜色            |
| ------- | --------------- |
| `B`/`b` | `Color.blue`    |
| `G`/`g` | `Color.green`   |
| `R`/`r` | `Color.red`     |
| `C`/`c` | `Color.cyan`    |
| `M`/`m` | `Color.magenta` |
| `Y`/`y` | `Color.yellow`  |
| `K`/`k` | `Color.black`   |
| `W`/`w` | `Color.white`   |

#### 示例

```
"§yWarning§: Low HP!"      →  "<color=yellow>Warning</color>: Low HP!"
"§#ff8800Critical§"        →  "<color=#ff8800>Critical</color>"
```

> 注意：§...§ 只负责样式，不会改变 $...$ 或 {...} 的求值；因此把它放在引用/表达式外层可让样式作用于最终文本。

#### 嵌套与组合

- 可以与 $...$ / {...} 混用：
  §y$@docs.movement$§ → 给“带链接下划线的引用文本”整体着色。
- 允许多层 §，但请保证成对闭合；解析时按从左到右匹配最近的闭合 §。

#### 转义

- 使用 \§ 输出字面 §。
  例："Use \§Y to start" → Use §Y to start

---

### 变量、表达式系统（Expressions）

#### 特性

- **层级键空间**：使用点号分隔（默认为 `.`），支持前缀存在性检测、子树提取、分段视图、首层键遍历等。
- **表达式解析**：在本地化文本中嵌入轻量表达式与变量（`+ - * / ^`、括号、变量）。

#### 概览

- 花括号 { ... } 包裹的是可求值表达式，结果将被转成字符串插入到模板中。
- 表达式内部可以引用变量、做加法运算，并在尾部通过 : 单个 formatter 对整个表达式结果进行格式化。
- 变量值来源：模板渲染时的 Localization Context， GetEscapeValue 提供。
- 变量参数：在变量名后用尖括号 <...> 传入键值对参数，这些参数会作为 GetEscapeValue 的 param 传入，供上下文侧决定返回值。

示例:

```
"projectile will alive for {prelaunchTime + lifetime<level=1>:sec}."
```

- prelaunchTime、lifetime<level=1> 都是变量，由 Context 提供数值。
- + 表示加法（当前实现语义里，我们只对加法做 AST 解析与求值）
- :sec 是对整个表达式的最终数值做一次格式化（例如转成“秒”的字符串）。

#### 语法与求值规则

1) 插值表达式的最小/完整形态

- 最小形态：{varName} —— 单变量替换
- 带参数的变量：{varName<k=v>}
- 表达式（加法）：{varA + varB}、{varA + varB<k=v>}
- 带格式化：{表达式:formatterName}

> 说明：冒号 : 之后只有一个 formatter，作用于整个表达式结果。不要再使用管道语法（|），也不要在花括号外再接 formatter。

2) 变量与参数传递

- 变量写法：identifier
- 变量参数：<key=value[,param...]>
- 这些参数将以原样字符串传入 GetEscapeValue(string escapeKey, params string[] param) 的 param，你可在 Context 内使用自定义解析方法（例如 GetOpt(param)）提取。

示例 Context：

```C#
public class SkillA : Skill
{
    public float prelaunchTime;
    public float lifetime;
}

public class SkillL10nContext : L10nContext
{
    public Skill skill;

    public Skill GetSkill(int level) { /* ... */ }

    public override object GetEscapeValue(string escapeKey, params string[] param)
    {
        int level = 0;
        foreach (var arg in GetOpt(param))
        {
            if (arg.Item1.SequentialEquals("level"))
                level = int.Parse(arg.Item2);
        }

        // 通过参数决定本次变量读取的“版本”（例如 level）
        var skill = GetSkill(level) ?? this.skill;

        // 将 escapeKey 分发到正确的数据源
        switch (escapeKey)
        {
            case "prelaunchTime": return skill.prelaunchTime;
            case "lifetime":      return skill.lifetime;
            // 其它变量...
        }

        // 兜底：交给基类（反射读取已注册对象的字段/属性）
        return base.GetEscapeValue(escapeKey, param);
    }
}
```

要点：

- 变量名（如 prelaunchTime、lifetime）就是 escapeKey。
- 变量参数（如 <level=1>）以 param 传入，不参与表达式求值，只影响 Context 如何取值。
- 你可以用 switch/表驱动/反射等方式返回变量值。

3) 运算与类型

- 运算：当前表达式解析器支持加法 + 并构建 AST；左右两侧是变量或数值字面量（数值字面量如 1.5、10）。
- 类型：会优先按数值类型（float/double/decimal 之一）计算；若出现类型不兼容，按以下顺序尝试：
  1. 将参与运算的操作数转为 float 进行加法
  2. 若仍失败，抛解析/求值错误（详见“错误处理”）

4) 格式化（Formatter）

- 位置：紧跟表达式末尾的 :formatterName
- 次数：仅一次
- 输入：格式化器接收整个表达式的最终结果（通常是数值，可以是任何结果）
- 输出：字符串，用于最终替换

示例：{prelaunchTime + lifetime<level=1>:sec}

- 先计算 prelaunchTime + lifetime(level=1)，得到一个数值（例如 2.2）
- 再交给 sec formatter，例如输出 "2.2 sec" 或本地化后的 "2.2 秒"

> 如果某个 formatter 需要额外参数（例如 :number(precision=2)），参考自定义formatter的实现。

#### 解析顺序

1. 解析花括号内字符串为 AST：
   - 提取尾部 :formatter（若有）
   - 解析加法表达式与变量节点；对每个变量解析其 <k=v[,p]> 参数列表（保留字符串形态）
2. 变量求值：
   - 对每个变量节点调用 context.GetEscapeValue(escapeKey, params)
   - 将返回值强制（或尝试）转换为数值，供运算使用
3. 表达式计算（数值加法）
4. 格式化：若存在 :formatter，把结果交给对应格式化器，得到字符串
5. 替换回模板

#### 错误处理

- 未知变量：GetEscapeValue 返回 null 或抛异常 → 记日志并以占位形式输出（原字符串）。
- 解析错误（非法字符/不完整表达式）或类型错误（无法转为数值参与加法）：

> 具体策略由 L10nDataManager 或 Parser/Evaluator 层统一控制。

#### 示例一览

```
# 单变量替换（无格式化）
"CD time: {cooldown}."

# 变量带参数
"Life time(Lv.3): {lifetime<level=3>}."

# 表达式（加法）
"Total time: {prelaunchTime + lifetime}."

# 表达式 + 变量参数 + 格式化
"projectile will alive for {prelaunchTime + lifetime<level=1>:sec}."
```

#### 模板语法

|    写法    |       含义       |                 说明                  |
| :--------: | :--------------: | :-----------------------------------: |
|   {var}    |    单变量替换    | var 由 Context 的 GetEscapeValue 提供 |
| {var<k=v>} |     变量参数     |   参数数组会原样传给 GetEscapeValue   |
|  {a + b}   |    加法表达式    | 先从 Context 取 a、b 并转数值后相加\| |
| {expr:fmt} | 表达式结果格式化 |         作用于整个表达式结果          |

---

### 解析顺序与深度

- 解析顺序：

1. 键引用：$...$ / $@...$
- 先匹配并展开 $key$（按 L10n.ReferenceImportOption）与 $@key$（强制 WithLinkTag | WithUnderline）。
- 对被引用内容再做一遍局部管线：$ → {} → §（见第 1.1 步）。
- 生成 <link=...> 与 <u>...</u> 时：
  - UseUnderlineResolver == WhileLinking：在这里对下划线与 <color> 分段进行就地拆分。
- 超过递归深度（L10n.MAX_RECURSION）直接停止并标记为缺失。

2. 动态表达式：{...}
解析表达式（变量支持行内参数 <k=v>，变量由 context.GetEscapeValue 提供；当前以加法等为主），再应用尾随 :formatter（若有）。
若表达式结果是字符串，再额外跑一次键引用解析（允许 {...} 结果里出现 $key$）。

3. 着色：§...§
§Y...§ 等单字母映射到固定颜色；§#RRGGBB(...)...§ 映射为 <color=...>...</color>。

对动态表达式/键引用循环上述步骤
收尾

1. 反斜杠归一：
 收尾把 \\ 还原为 \（用于前面阶段的转义书写）。

1. 全局下划线拆分（可选）：
若 UseUnderlineResolver == Always，在最末尾对整段文本执行一次下划线与 <color> 的分段拆分（保证 <u> 不跨色块）。


> 循环与深度限制：循环引用（A → B → A）的最大上限为20，超过该上限后，最后一级的内容将不会被处理

### 示例

模板

```
"projectile will alive for {prelaunchTime + lifetime<level=1>:sec}. See $@docs.projectile.life$ — §yimportant§."
```

上下文（摘录）

```
// 表达式变量：来自 SkillL10nContext
prelaunchTime = 0.3f;
lifetime= 2.0f; // when level=1

// 引用键
docs.projectile.life = "Projectile Lifetime";
```

渲染结果（示例，使用 TMP 标签）

```
"projectile will alive for 2.3 sec. See <u><link=docs.projectile.life>Projectile Lifetime</link></u> — <color=yellow>important</color>."
```
