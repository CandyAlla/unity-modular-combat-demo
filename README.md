# Battle Demo (Unity 3D Action Roguelite)

> **面试作品仓库** | Unity 2021+ LTS | URP | C#

## 1. 项目简介

本项目是一个展示 Unity 客户端架构能力与工程化实践的 **3D 俯视角战斗 Demo**。
主要演示了**模块化架构**、**数据驱动的关卡设计**、**状态机驱动的流程管理**以及**基础的性能优化方案**。

**核心亮点：**
- **清晰架构**：基于 `GameEntry` + `SceneStateSystem` 的生命周期与流程管理。
- **数据驱动**：关卡波次（Wave）与怪物刷新完全由 ScriptableObject 配置驱动。
- **模块化战斗**：Actor 模型（Player/NPC）与 Buff 系统分离，支持灵活扩展。
- **性能意识**：内置 `PoolManager` 实现对象池复用，减少运行时 GC 与实例化开销。

---

## 2. 核心架构

项目代码主要位于 `Assets/Project/Scripts`，遵循分层设计原则：

### 2.1 目录结构
```text
Scripts
├── App
│   ├── Core              # 核心框架 (Singleton, FSM)
│   ├── Actors            # 角色实体 (Player, NPC)
│   ├── Buff              # Buff 系统 (LayerMgr, BufferInstance)
│   ├── Camera            # 相机控制
│   ├── Debug             # 调试工具
│   ├── UI                # UI 框架 (UIManager, UIBase)
│   ├── SceneStateSystem.cs # 场景状态机
│   └── PoolManager.cs    # 对象池管理
├── Data
│   └── Configs           # 配置数据定义 (MainChapterConfig)
├── Scenes
│   ├── LoginScene        # 登录/Hub 逻辑
│   └── BattleScene       # 战斗核心逻辑
└── UI                    # 具体 UI 实现 (UI_LoginPanel, UI_BattleSettlement)
```

### 2.2 关键模块说明

- **场景流程 (Scene Flow)**
    - 使用 `SceneStateSystem` 统一管理场景切换（Entry -> Login/Hub -> Battle）。
    - 避免直接调用 `SceneManager.LoadScene`，确保转场前后的资源清理与初始化 (`DoBeforeLeaving`, `DoBeforeEntering`)。

- **战斗系统 (Battle Core)**
    - **Actor 设计**：`MPCharacterSoulActorBase` 作为基类，封装了属性、状态与基础行为。
    - **Buff 系统**：独立的 `BuffLayerMgr` 管理 Buff 的叠加、刷新与移除。支持 `MoveSpeedUp` 等多种效果。

- **关卡配置 (Data Driven)**
    - 关卡逻辑不写死。通过 `MainChapterConfig` 配置总时长与波次。
    - `NPCSpawnData` 定义每一波的怪物种类、数量与刷新节奏。

---

## 3. 快速开始 (Quick Start)

1. **环境准备**：
    - Unity Editor 2021.3 LTS 或更高版本。
    - 确保已安装 URP (Universal Render Pipeline) 包。

2. **运行步骤**：
    - 打开 `Assets/Scenes/Map_GameEntry.unity`。
    - 点击 Editor 播放按钮。
    - 游戏将自动初始化，跳转至 Hub (Login) 界面。
    - 点击 **"开始战斗"** 按钮进入战斗演示。

---

## 4. 性能优化 (Performance)

针对同屏大量怪物（300+）的需求，项目采取了以下优化措施：

### 4.1 对象池 (Object Pooling)
- **问题**：高频率创建/销毁子弹与怪物会导致 CPU 峰值与 GC 压力。
- **方案**：实现了 `PoolManager`。
    - **预加载**：进入战斗场景时，根据配置预先实例化指定数量的怪物与特效。
    - **复用**：怪物死亡时仅 `SetActive(false)` 并回收至池，而非销毁。
- **效果**：显著降低了战斗过程中的 GC Alloc，帧率波动更加平滑。

### 4.2 结构化数据
- 核心战斗计算尽量采用轻量级数据结构，减少引用类型造成的内存碎片。

---

## 5. 调试系统 (Debug System)

本项目包含一套完善的运行时调试工具，位于 `Map_TestScene` 或战斗场景中：

- **Runtime Debug Panel (UI_DebugPanel)**
    - **实时监控**：显示 FPS、敌人数量、玩家属性及 Buff 状态。
    - **动态生成**：支持一键生成指定或随机怪物 (Spawn Enemy)。
    - **Buff 调试**：支持给主角或全场 NPC 添加 Buff (加速/加攻/减速/眩晕)。
    - **状态重置**：一键重置主角状态 (Reset Hero)。

- **代码级验证**
    - `BuffSystemVerification.cs`: 用于单元测试 Buff 叠加逻辑。

---

## 6. 后续计划 (TODO)
- [ ] **性能数据基准测试**：在 300+ 同屏怪环境下进行 Profiler 采样并记录具体 GC/FPS 数据。
- [ ] **更多技能支持**：扩展技能配置系统，支持 AOE 与 投射物技能。
- [ ] **NavMesh 动态烘焙**：支持更加复杂的动态地形寻路。
