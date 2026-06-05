# Stage 3: Advanced VFX Tools

## 新增能力

实现 6 个语义化高级游戏特效，每个特效由多个 Unity 组件（ParticleSystem、LineRenderer、Light）组合生成：

1. **魔法传送门** — 环形粒子 + 核心粒子 + 飞散火花 + 旋转光环（LineRenderer）+ 点光源
2. **火焰爆炸** — 火焰爆发 + 烟雾 + 飞溅火花（带拖尾）+ 闪光
3. **雷电命中** — 锯齿闪电主光束 + 分支闪电 + 撞击火花 + 光晕
4. **治疗光环** — 地面光环（LineRenderer）+ 上升粒子 + 闪烁星芒 + 柔和绿光
5. **烟雾爆发** — 大烟雾团 + 飘散烟雾 + 地面尘土环
6. **刀光/斩击拖尾** — 弧形刀光（LineRenderer）+ 飞散粒子 + 闪光

## Python MCP Tools

| Tool | Description |
|------|-------------|
| `create_magic_portal` | 创建魔法传送门 |
| `create_fire_explosion` | 创建火焰爆炸 |
| `create_lightning_hit` | 创建雷电命中 |
| `create_heal_aura` | 创建治疗光环 |
| `create_smoke_burst` | 创建烟雾爆发 |
| `create_slash_trail` | 创建刀光拖尾 |

## Unity HTTP Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/create-magic-portal` | 创建传送门 |
| POST | `/create-fire-explosion` | 创建火焰爆炸 |
| POST | `/create-lightning-hit` | 创建雷电命中 |
| POST | `/create-heal-aura` | 创建治疗光环 |
| POST | `/create-smoke-burst` | 创建烟雾爆发 |
| POST | `/create-slash-trail` | 创建刀光拖尾 |

## 工具参数说明

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

## PowerShell 测试方式

```powershell
# 确保 Unity 已启动且 HTTP Server 已运行
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
./test-stage3.ps1
```

## 常见问题

### 生成了对象但看起来不明显

原因：
1. 粒子系统默认可能未播放 — 粒子的 `playOnAwake` 默认为 true，不需要手动调用 Play
2. 场景视图可能离特效太远 — 选中对象按 F 聚焦
3. 粒子大小或颜色 Alpha 太低

### 发光不明显

1. 检查场景是否使用 URP，且 Post Processing 是否启用
2. LineRenderer 使用 `CreateLineRendererMaterial` 会自发光，但在 Built-in 中需要开启 `Emission GI`
3. 建议在 URP 项目中使用

### URP/Built-in 下材质差异

LineRenderer 和粒子材质自动适配：
- 优先使用 `Universal Render Pipeline/Particles/Unlit`
- 回退到 `Standard` 或 `Unlit/Color`

### LineRenderer 看不到

1. 检查宽度是否太小（建议 0.02~0.3）
2. 材质颜色 Alpha 是否 > 0
3. 确认场景坐标下 LineRenderer 不在物体内部
4. 尝试在 Scene 视图不同角度查看

### Prefab 保存失败

1. 确保该名称的 Prefab 没有被打开关闭冲突
2. 自动检查 `Assets/AI_Generated/Prefabs/` 目录是否存在并创建
3. 同名 Prefab 会自动编号（xxx_1.prefab）

### 粒子没有自动播放

所有粒子系统 `playOnAwake` 默认为 true。如果粒子停止：

1. 选中对象，查看 Particle System 组件
2. 点击 `Open Editor` 查看预览
3. 检查 `Simulation Speed` 是否为 0
4. 非循环特效需要手动切换预览时间轴

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

## 复用阶段一/二

- 可以使用 `create_particle_effect` 创建简单粒子
- 使用 `create_material` 预生成自定义材质
- 使用 `assign_material` 替换特效子对象材质
- 使用 `save_prefab` 单独保存某些部分
