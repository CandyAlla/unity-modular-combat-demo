# Battle Demo (Unity 3D Roguelite Combat Demo)

> Portfolio repository | Unity 2021 LTS+ | URP | C#

## 1. Overview

This project is a **top-down 3D combat demo** built for interview showcase. The focus is on clear architecture, modularization, data-driven design, and engineering practices rather than full game content.

**Highlights**
- Scene flow: `GameEntry` + `SceneStateSystem` for unified transitions
- Data-driven: level waves authored with ScriptableObjects
- Modular combat: Actor/Buff/Skill separated for extension
- Performance awareness: object pools + centralized ticking to reduce GC/spikes

## 2. Scene Flow

Flow: `Map_GameEntry` -> `Map_Login` -> `Map_BattleScene` -> Settlement -> back to `Map_Login`.

Key points:
- All scene switches go through `SceneStateSystem`; no direct `SceneManager.LoadScene` from gameplay scripts.
- `UI_LoginPanel` handles stage selection and entry to battle.

## 3. Structure (Core)

```text
Assets/Project/Scripts
├── App
│   ├── Actors           # Actor base and NPC AI
│   ├── Attributes       # Attribute system
│   ├── Buff             # Buff stacking and effects
│   ├── Camera           # Camera follow
│   ├── Core             # Event bus, base systems
│   ├── Debug            # Debug panel and verification scripts
│   ├── UI               # UI base and manager
│   ├── PoolManager.cs   # Object pooling entry
│   └── SceneStateSystem.cs
├── Combat               # Combat logic (projectiles/feedback)
├── Data                 # Config data (level/skill)
├── Player               # Player control and skill triggers
├── Scenes               # Scene managers
├── SkillSystem          # Runtime skill control
└── UI                   # Concrete UI screens
```

Config data:
```
Assets/Project/Resources/Configs
```

## 4. Quick Start

1. Open the project with Unity 2021.3 LTS or later.
2. Open scene: `Assets/Scenes/Map_GameEntry.unity`
3. Play. The game auto-enters `Map_Login`.
4. Select a stage and click "Start Battle".

## 5. Controls

- Move: W/A/S/D
- Basic attack: J
- Active skill 1: K (or UI button)
- Active skill 2: L (or UI button)

## 6. Level Config (Data-Driven)

### 6.1 Config Structure

`MainChapterConfig` (ScriptableObject)
- StageId: stage id
- Duration: duration in seconds
- Waves: List<NPCSpawnData>

`NPCSpawnData`
- Time: trigger second
- NpcId: npc id
- NpcCount: count
- SpawnPointIndex: spawn point index
- SpawnPosition: fixed spawn position (optional)

### 6.2 Examples

**Stage_01_Easy**
- Duration: 120s
- Wave timing: 3/10/18/25/35/45/55/70/85/100 seconds
- Enemies: 101 (common), 102 (elite) in mid/late waves

**Stage_02_Rush**
- Duration: 90s
- Wave timing: 2s start, dense waves at 31-40s
- Enemies: 102 (high pressure)

Assets:
```
Assets/Project/Resources/Configs/Stage_01_Easy.asset
Assets/Project/Resources/Configs/Stage_02_Rush.asset
```

## 7. Combat & Skill System

### 7.1 Actors
- `MPCharacterSoulActorBase` handles HP, damage, death events.
- `MPSoulActor`: player character.
- `MPNpcSoulActor`: NPC AI (chase/attack).

### 7.2 Buff System
- `BuffLayerMgr` handles stacking/refresh/expiry.
- Buffs modify attributes at runtime (speed/attack).
- Debug panel shows active buffs with stacks.

### 7.3 Skill System
- `SkillRuntimeController`: state machine (Casting/Active/Recovery).
- `MPSkillActorLite`: primary + two active skills.
- Configured skills:
  - PrimaryAttack (basic melee)
  - ActiveAoE (area damage)
  - ActiveBarrage (fast projectile)

Assets:
```
Assets/Project/Resources/Configs/Skill/PrimaryAttack.asset
Assets/Project/Resources/Configs/Skill/ActiveAoE.asset
Assets/Project/Resources/Configs/Skill/ActiveBarrage.asset
```

## 8. UI & Settlement

UI is managed by `UIManager`:
- `UI_LoginPanel`: stage select + enter battle
- `UI_BattlePanel`: timer/pause/skill buttons
- `UI_BattleSettlement`: result (win/lose, time, kills, spawns, damage stats)

## 9. Debug Tools

- `UI_DebugPanel`: stage time, enemy count, player stats, buffs
- `BuffSystemVerification`: validate buff stacking logic
- Test entry: `Map_Login` "Test" button -> `Map_TestScene`

## 10. Performance (Template)

### 10.1 Problem (to fill)
- On-screen enemies: 300+
- Initial FPS/GC/CPU cost: TBD

### 10.2 Optimization
- Object pooling: `PoolManager` for NPCs/bullets/floating text/HP bars
- Centralized tick: `MPRoomManager` updates actors

### 10.3 Result (to fill)
- FPS improvement: TBD
- GC allocation: TBD

## 11. TODO

- Runtime debug panel hotkey
- Better skill VFX
- Performance numbers + capture video
