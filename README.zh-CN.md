# Battle Demo（Unity 3D Roguelite 战斗 Demo）

[English Version](README.md)

## 1. 项目简介

本项目是一个用于面试展示的 **3D 俯视角战斗 Demo**，重点在于清晰架构、模块化拆分、数据驱动与工程化能力展示，而非完整商业化内容。

**核心亮点**
- 场景流程：`GameEntry` + `SceneStateSystem` 统一管理转场
- 数据驱动：关卡波次由 ScriptableObject 配置驱动
- 战斗模块化：Actor/Buff/Skill 解耦，便于扩展
- 性能意识：对象池 + 集中 Tick 降低 GC 与抖动

## 2. 场景流程

流程：`Map_GameEntry` -> `Map_Login` -> `Map_BattleScene` -> 结算 -> 返回 `Map_Login`。

关键点：
- 场景切换统一由 `SceneStateSystem` 管理，避免业务脚本直接调用 `SceneManager.LoadScene`。
- `UI_LoginPanel` 负责选择关卡与进入战斗。

## 3. 目录结构（核心部分）

```text
Assets/Project/Scripts
├── App
│   ├── Actors           # Actor 基类与 NPC AI
│   ├── Attributes       # 属性系统
│   ├── Buff             # Buff 叠加与效果计算
│   ├── Camera           # 相机跟随
│   ├── Core             # 事件总线等基础模块
│   ├── Debug            # 调试面板与验证脚本
│   ├── UI               # UI 基类与管理器
│   ├── PoolManager.cs   # 对象池入口
│   └── SceneStateSystem.cs
├── Combat               # 战斗相关（投射物/反馈）
├── Data                 # 配置数据结构（关卡/技能）
├── Player               # 玩家控制与技能触发
├── Scenes               # 场景管理器
├── SkillSystem          # 技能运行时控制
└── UI                   # 具体 UI 界面
```

配置数据路径：
```
Assets/Project/Resources/Configs
```

## 4. 快速开始

1. 使用 Unity 2021.3 LTS 或更高版本打开项目。
2. 打开场景：`Assets/Scenes/Map_GameEntry.unity`
3. 点击 Play，自动进入 `Map_Login`。
4. 选择关卡并点击“开始战斗”进入战斗演示。

## 5. 操作方式

- 移动：W/A/S/D
- 普通攻击：J
- 主动技能 1：K（或 UI 按钮）
- 主动技能 2：L（或 UI 按钮）

## 6. 关卡配置（数据驱动）

### 6.1 配置结构

`MainChapterConfig`（ScriptableObject）
- StageId：关卡 ID
- Duration：关卡时长（秒）
- Waves：List<NPCSpawnData>

`NPCSpawnData`
- Time：触发秒
- NpcId：怪物 ID
- NpcCount：数量
- SpawnPointIndex：刷怪点索引
- SpawnPosition：固定刷怪点（可选）

### 6.2 配置示例

**Stage_01_Easy**
- Duration：120s
- 波次节奏：3/10/18/25/35/45/55/70/85/100 秒
- 主要怪物：101（普通），中后期插入 102（精英）

**Stage_02_Rush**
- Duration：90s
- 波次节奏：2 秒起手，31-40 秒密集刷怪
- 怪物类型：102（高强度）

配置资产位置：
```
Assets/Project/Resources/Configs/Stage_01_Easy.asset
Assets/Project/Resources/Configs/Stage_02_Rush.asset
```

## 7. 战斗与技能系统

### 7.1 Actor 基础
- `MPCharacterSoulActorBase` 统一处理 HP、受击、死亡事件。
- `MPSoulActor`：玩家角色
- `MPNpcSoulActor`：怪物 AI（追击/攻击）

### 7.2 Buff 系统
- `BuffLayerMgr` 管理 Buff 叠加、刷新与移除
- 运行时 Buff 影响属性（移速、攻击等）
- 调试面板可查看 Buff 列表与层数

### 7.3 技能系统
- `SkillRuntimeController`：技能状态机（Casting/Active/Recovery）
- `MPSkillActorLite`：管理主技能与两个主动技能
- 已配置技能：
  - PrimaryAttack（普攻，近战）
  - ActiveAoE（范围伤害）
  - ActiveBarrage（高速弹道技能）

配置资产位置：
```
Assets/Project/Resources/Configs/Skill/PrimaryAttack.asset
Assets/Project/Resources/Configs/Skill/ActiveAoE.asset
Assets/Project/Resources/Configs/Skill/ActiveBarrage.asset
```

## 8. UI 与结算

UI 由 `UIManager` 统一管理：
- `UI_LoginPanel`：关卡选择 + 进入战斗
- `UI_BattlePanel`：计时/暂停/技能按钮
- `UI_BattleSettlement`：结算（胜负、时长、击杀、刷怪数、伤害统计）

## 9. 调试工具

- `UI_DebugPanel`：显示关卡时间、敌人数量、玩家属性、Buff 列表
- `BuffSystemVerification`：用于验证 Buff 叠加逻辑
- 测试入口：`Map_Login` 的 Test 按钮进入 `Map_TestScene`

## 10. 性能优化（模板）

### 10.1 问题描述（待补充）
- 同屏怪物数量：300+
- 初始帧率/GC/CPU 开销：待补充

### 10.2 优化手段
- 对象池：`PoolManager` 统一管理 NPC/子弹/飘字/血条
- 集中 Tick：由 `MPRoomManager` 统一驱动 Actor 更新

### 10.3 效果对比（待补充）
- 帧率提升：待补充
- GC 分配：待补充

## 11. 后续计划

- 运行时 Debug 面板快捷键
- 技能表现与特效增强
- 完善性能分析数据与录屏
