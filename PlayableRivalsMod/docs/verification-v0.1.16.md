# v0.1.16 verification / 验证记录

Date: 2026-11-24. Windows x64, Steam Build 26845264, MelonLoader 0.7.3.

## Native runtime observations

| Test / 测试 | Observed result / 实际结果 |
| --- | --- |
| Outcast level-1 sword / 初始魔剑 | 23 damage |
| Outcast level-21 sword before unlock / 解除前 | 48 base, 30 actual damage |
| Two 20-damage hits in one use / 同次两段各 20 | 20 + 10 |
| Single-target hit followed by an area attack in one use / 单体后群攻 | Target 1: 20, target 1: 10, target 2: 0 |
| Strength buff / 力量强化 | Direct damage capped at 30 |
| Native burning and poison ticks / 原版燃烧中毒 | 80 and 2008 damage in a high-health test encounter; not capped |
| Soldier and enemy isolation / 士兵与敌人隔离 | 82 and 89 damage; not capped |
| Daggers, levels 1 and 21 / 匕首 | 11 and 24 per independent action |
| Ordinary iron sword with robbery preparation / 普通铁剑抢夺 | Weapon stolen, preparation consumed, 30 damage; target survives |
| Cannot-steal weapon, miss, healing, enchantment / 夺取边界 | Restrictions and preparation consumption remain correct |
| Ordinary victory / 普通战斗胜利 | Cap stays enabled |
| Native slime victory branch, then another battle / 目标首领胜利分支后 | Unlock persists; level-21 sword deals 48 |
| Post-battle save, native menu load, new battle / 战后存档读档再进战斗 | Unlock restored; actual damage remains 48 |
| Villainess isolation / 千金隔离 | Levels 1/21/41 deal 15/27/39; Love's area effects, 70% boundary and cooldown remain correct |
| Dropout isolation / 落选者隔离 | Levels 1/21 grant 15/25 block; armored physical damage at levels 1/21/41 is 3/18/34; magic stays disabled |

Tests used isolated copies of the game and separate test saves. The original 108 save files and the other seven installed mods were unchanged by hash. The final release DLL passed runtime checks of selection descriptions, icons, the damage-cap notice, learned-skill unlocks, sanity portrait bindings, and repeated party swaps.

多段与群攻测试使用隔离进程中的临时 ActionSet，经过原版选牌、QTE 和扣血流程。测试动作没有写入发行 DLL。首领解锁通过原版胜利分支验证，未声称从开局完整通关所有剧情与形态。

## Limits / 边界

- Unit checks cover pure rules; they do not prove native combat by themselves.
- Boss tests entered the native victory branch in an isolated scenario, rather than completing a full story playthrough.
- Persistence acceptance covers a post-battle save followed by a new encounter. Early tests that forced a mid-battle save restored variables but did not restore enemy controllers; those tests were not counted as passing, and enemy restoration was not changed in this release.
- Hidden-window UI checks verify live text, icons, and bindings, not complete visual acceptance.

## Binary identity

`AnnihilatedPlayableRivals.dll` SHA-256:

```text
D712112116D1A50AC50FCE502EB417D54BF05C5F10AC751876E7AC8F1964C974
```

The release ZIP contains exactly `Mods/AnnihilatedPlayableRivals.dll` and `README.md`. Its embedded DLL matches the locally installed and tested release binary. Source packaging relocates shared files and image directories without changing gameplay source or image contents.
