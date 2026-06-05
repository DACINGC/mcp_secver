# Stage 4: Preview, Template, Screenshot & Workflow Enhancement

## 新增能力

实现 9 个 MCP Tools，建立"创建 → 预览 → 截图 → 调参 → 保存"的闭环：

1. **focus_scene_object** — 选中并 Frame 场景对象
2. **play_effect** — 播放粒子特效
3. **stop_effect** — 停止粒子特效
4. **capture_view** — 截图 SceneView 或 GameView 保存为 PNG
5. **create_vfx_from_template** — 从模板 Prefab 创建新特效实例
6. **instantiate_prefab** — 实例化已有 Prefab
7. **list_generated_assets** — 查询 AI_Generated 下的生成资源
8. **clear_ai_generated_scene_objects** — 清理场景中 AI 生成对象
9. **get_object_info** — 获取对象详细信息

## Python MCP Tools

| Tool | Description |
|------|-------------|
| `focus_scene_object` | 选中并聚焦 GameObject |
| `play_effect` | 播放 ParticleSystem |
| `stop_effect` | 停止 ParticleSystem |
| `capture_view` | 截图 Scene/Game View 并保存 PNG |
| `create_vfx_from_template` | 从模板 Prefab 创建特效实例 |
| `instantiate_prefab` | 实例化 Prefab |
| `list_generated_assets` | 列出 AI 生成资源 |
| `clear_ai_generated_scene_objects` | 按前缀清理场景对象 |
| `get_object_info` | 获取对象详细信息 |

## Unity HTTP Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/focus-scene-object` | 聚焦对象 |
| POST | `/play-effect` | 播放特效 |
| POST | `/stop-effect` | 停止特效 |
| POST | `/capture-view` | 截图 |
| POST | `/create-vfx-from-template` | 从模板创建特效 |
| POST | `/instantiate-prefab` | 实例化 Prefab |
| POST | `/list-generated-assets` | 列出生成资源 |
| POST | `/clear-ai-generated-scene-objects` | 清理场景 |
| POST | `/get-object-info` | 获取对象信息 |

## 截图保存位置

所有截图保存到：`Assets/AI_Generated/Captures/{file_name}.png`

支持两种视图：
- **scene** — SceneView 截图（使用相机渲染到 RenderTexture）
- **game** — GameView 截图（使用 ScreenCapture，如果不可用则回退到 SceneView）

## 模板 Prefab 放置位置

模板 Prefab 可以从以下目录读取：
- `Assets/VFX/Templates/` — 设计师放置的模板
- `Assets/AI_Generated/Prefabs/` — AI 之前生成的 Prefab

## 从模板创建特效的流程

1. 准备模板 Prefab 放到 `Assets/VFX/Templates/`
2. 调用 `create_vfx_from_template(templatePath, outputName, ...)`
3. 可以传入 `mainColor` 覆盖所有粒子/Light/Renderer 颜色
4. 材质自动复制到 `Assets/AI_Generated/Materials/`，不修改模板材质
5. 可选择保存为新 Prefab 到 `Assets/AI_Generated/Prefabs/`

## AI 工作流示例

```
1. create_magic_portal → 创建门户
2. focus_scene_object → 聚焦查看
3. play_effect → 播放特效
4. capture_view → 截图保存
5. 用户查看截图后要求调整颜色/大小
6. set_transform / create_vfx_from_template 调参
7. save_prefab → 保存最终版本
```

## PowerShell 测试方式

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
./test-stage4.ps1
```

## 常见问题

### SceneView 截图失败

- 确保有一个打开的 SceneView 标签页
- 如果没有，Unity Editor → Window → General → Scene
- camera 可能为空（极少见），此时返回错误

### GameView 截图 fallback

`ScreenCapture.CaptureScreenshot` 在 Editor 下可能不能稳定保存到指定路径。如果失败，自动 fallback 到 SceneView 截图，message 中会说明。

### Prefab 加载失败

- 确认路径正确（使用 list_generated_assets 查看）
- 确认文件扩展名为 `.prefab`
- 模板路径必须为 `Assets/VFX/Templates/` 或 `Assets/AI_Generated/Prefabs/` 开头
- instantiate_prefab 允许任何 `Assets/` 开头的 `.prefab`

### 材质被修改到模板上

create_vfx_from_template 在修改材质时，会自动：
1. `new Material(renderer.sharedMaterial)` 创建新实例
2. 保存到 `Assets/AI_Generated/Materials/`
3. 赋给 renderer.sharedMaterial
4. 不会修改原模板 Prefab 的材质

### list_generated_assets 为空

- 确认执行过至少一次 save_prefab、create_material 或 capture_view
- 扫描范围为 `Assets/AI_Generated/` 及其子目录
- 只返回 `.prefab`、`.mat`、`.png` 文件

### clear_ai_generated_scene_objects 的安全限制

- prefix 不能为空，且至少 3 个字符
- 只删除当前活动场景中名称以 prefix 开头的**根对象**
- 不递归删除非匹配根对象的子对象
- 不删除任何 Asset 资源
- 默认 prefix = "AI_"
