# Stage 3: Advanced VFX Tools

> 本篇为高级 VFX 特效的详细参考文档。工具列表和端点已在 README 中完整列出，此处仅保留 **文档内独有的参考信息**。

## 工具参数详情

### create_magic_portal

| 参数 | 类型 | 默认值 | 限制 | 说明 |
|------|------|--------|------|------|
| effect_name | str | — | — | 特效根对象名 |
| main_color | str | "#33AAFF" | — | 主色 HTML 颜色 |
| radius | float | 2.0 | 0.2~10 | 传送门半径 |
| duration | float | 5.0 | 0.5~30 | 持续秒数 |
| loop | bool | true | — | 是否循环 |
| save_as_prefab | bool | false | — | 是否保存为 Prefab |

### create_fire_explosion

| 参数 | 类型 | 默认值 | 限制 | 说明 |
|------|------|--------|------|------|
| effect_name | str | — | — | 特效根对象名 |
| radius | float | 2.0 | 0.2~20 | 爆炸半径 |
| intensity | float | 1.0 | 0.1~5 | 强度倍数 |
| duration | float | 1.2 | 0.2~10 | 持续秒数 |
| save_as_prefab | bool | false | — | 是否保存为 Prefab |

### create_lightning_hit

| 参数 | 类型 | 默认值 | 限制 | 说明 |
|------|------|--------|------|------|
| effect_name | str | — | — | 特效根对象名 |
| main_color | str | "#AA33FF" | — | 闪电颜色 |
| height | float | 4.0 | 0.5~20 | 闪电高度 |
| radius | float | 1.0 | 0.1~10 | 扩散半径 |
| duration | float | 0.8 | 0.1~5 | 持续秒数 |
| branch_count | int | 5 | 1~20 | 分支数量 |
| save_as_prefab | bool | false | — | 是否保存为 Prefab |

### create_heal_aura

| 参数 | 类型 | 默认值 | 限制 | 说明 |
|------|------|--------|------|------|
| effect_name | str | — | — | 特效根对象名 |
| main_color | str | "#55FF88" | — | 主色 |
| radius | float | 2.0 | 0.2~10 | 光环半径 |
| duration | float | 4.0 | 0.5~30 | 持续秒数 |
| loop | bool | true | — | 是否循环 |
| save_as_prefab | bool | false | — | 是否保存为 Prefab |

### create_smoke_burst

| 参数 | 类型 | 默认值 | 限制 | 说明 |
|------|------|--------|------|------|
| effect_name | str | — | — | 特效根对象名 |
| color | str | "#777777" | — | 烟雾颜色 |
| radius | float | 2.0 | 0.2~20 | 扩散半径 |
| duration | float | 2.5 | 0.5~20 | 持续秒数 |
| density | float | 1.0 | 0.1~5 | 粒子密度 |
| save_as_prefab | bool | false | — | 是否保存为 Prefab |

### create_slash_trail

| 参数 | 类型 | 默认值 | 限制 | 说明 |
|------|------|--------|------|------|
| effect_name | str | — | — | 特效根对象名 |
| main_color | str | "#66CCFF" | — | 刀光颜色 |
| length | float | 3.0 | 0.5~20 | 刀光长度 |
| width | float | 0.3 | 0.02~3 | 刀光宽度 |
| duration | float | 0.5 | 0.1~5 | 持续秒数 |
| save_as_prefab | bool | false | — | 是否保存为 Prefab |

## 生成的 Unity 对象结构

### MagicPortal

```
AI_Test_Magic_Portal
├─ Portal_Ring_Particles (ParticleSystem, Circle, Ring color, Alpha fade)
├─ Portal_Core_Particles (ParticleSystem, Sphere, Core glow)
├─ Portal_Spark_Particles (ParticleSystem, Circle edge, Fast sparks)
├─ Portal_Rotating_Ring (LineRenderer, Circle loop)
└─ Portal_Light (Point Light, main_color, range = radius*3)
```

### FireExplosion

```
AI_Test_Fire_Explosion
├─ Fire_Burst (ParticleSystem, Burst, Orange gradient, Size burst)
├─ Smoke_Burst (ParticleSystem, Burst, Gray smoke, Rise + Noise)
├─ Sparks (ParticleSystem, Burst, Yellow trails)
└─ Flash_Light (Point Light, Orange, intense)
```

### LightningHit

```
AI_Test_Lightning_Hit
├─ Lightning_Main_Bolt (LineRenderer, Zigzag from top to ground)
├─ Lightning_Branch_0..N (LineRenderer, Random branches)
├─ Impact_Sparks (ParticleSystem, Burst, Purple)
└─ Lightning_Light (Point Light, Purple, ground level)
```

### HealAura

```
AI_Test_Heal_Aura
├─ Aura_Ring (LineRenderer, Circle on ground)
├─ Rising_Particles (ParticleSystem, Circle, Velocity Y up)
├─ Healing_Sparkles (ParticleSystem, Sphere, Small sparkles)
└─ Aura_Light (Point Light, Green, soft)
```

### SmokeBurst

```
AI_Test_Smoke_Burst
├─ Smoke_Main (ParticleSystem, Burst, Large gray, Noise)
├─ Smoke_Drift (ParticleSystem, Burst, Slow drift upward)
└─ Dust_Ring (ParticleSystem, Circle on ground)
```

### SlashTrail

```
AI_Test_Slash_Trail
├─ Slash_Arc (LineRenderer, Semi-circle arc)
├─ Slash_Sparks (ParticleSystem, Burst, Along arc)
└─ Slash_Light (Point Light, Brief flash)
```

## 调参建议

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| radius | 控制特效整体尺寸 | 1~5 |
| intensity | 控制爆炸力度（粒子速度 + 光强） | 0.5~3 |
| duration | 控制特效播放时长 | 非循环 0.5~2，循环 5~10 |
| density | 控制粒子数量密度 | 0.5~3 |
| branch_count | 控制闪电分支数 | 3~10 |
| emission rate | 粒子发射率（内部设定） | 20~120 |
| start size | 粒子大小 | 0.05~1.0 |
| light intensity | 光源强度 | 2~12 |
