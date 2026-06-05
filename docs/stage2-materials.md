# Stage 2: Material System

## 新增能力

- 创建材质资源（.mat）并保存到 `Assets/AI_Generated/Materials/`
- 设置材质颜色和发光（Emission）属性
- 为场景对象分配材质（支持 Renderer 和 ParticleSystemRenderer）
- 创建适合粒子系统的 Additive/透明材质
- 自动处理 Shader 回退
- HDR 发光颜色支持

## Python MCP Tools

| Tool | Description |
|------|-------------|
| `create_material` | 创建材质资源，支持颜色、发光、Shader 选择 |
| `assign_material` | 把材质分配给场景中的 GameObject |
| `create_additive_particle_material` | 创建适合粒子的透明/Additive 材质 |
| `set_material_color` | 修改已有材质的主色 |
| `set_material_emission` | 修改已有材质的发光颜色和强度 |

## Unity HTTP Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/create-material` | 创建材质资源 |
| POST | `/assign-material` | 分配材质给对象 |
| POST | `/create-additive-particle-material` | 创建粒子透明材质 |
| POST | `/set-material-color` | 设置材质颜色 |
| POST | `/set-material-emission` | 设置材质发光 |

## PowerShell 测试方式

```powershell
# 确保 Unity 已启动且 HTTP Server 已运行
.\test-stage2.ps1
```

脚本会自动：
1. Ping Unity
2. 创建空对象
3. 创建发光材质
4. 分配材质
5. 修改颜色
6. 修改发光
7. 创建粒子效果
8. 创建粒子材质
9. 分配粒子材质
10. 保存 Prefab

## 常见问题

### Shader 找不到

如果指定 Shader 不存在，会自动按以下顺序回退：
- `Universal Render Pipeline/Particles/Unlit`
- `Universal Render Pipeline/Lit`
- `Particles/Standard Unlit`
- `Standard`
- `Unlit/Color`

如果所有 Shader 都不存在（极罕见），会返回错误。

### 材质创建成功但看不到发光

可能原因：
1. Shader 不支持 `_EmissionColor` 属性
2. 场景中没有启用后处理效果
3. 发光强度太低（建议 1-5）
4. 使用 Built-in Render Pipeline 时需要在 Lighting 设置中启用 Emission

### assign_material affectedCount 为 0

可能原因：
1. `object_name` 拼写错误或对象不存在
2. 对象没有任何 Renderer 组件
3. `material_path` 不合法

排查：
- 先用 `list_scene_objects` 确认对象名
- 检查 material_path 格式
- 确认材质文件存在

### material_path 不合法

material_path 必须满足：
- 以 `Assets/` 开头
- 不包含 `..`
- 以 `.mat` 结尾

### URP/HDRP/Built-in 管线差异

- **URP**: 推荐使用 `Universal Render Pipeline/Particles/Unlit`，支持 `_BaseColor`、`_EmissionColor`
- **Built-in**: `Standard` Shader 使用 `_Color`、`_EmissionColor`、`_Mode` 属性
- **HDRP**: 本工具未专门适配 HDRP，建议将 Unity 项目切换到 URP

材质创建时使用 `HasProperty` 检测属性存在性，不会因属性缺失报错。
