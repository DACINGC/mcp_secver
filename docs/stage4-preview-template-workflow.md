# Stage 4: Preview, Template, Screenshot & Workflow

> 本篇为预览与工作流的详细参考文档。工具列表和端点已在 README 中完整列出，此处仅保留 **文档内独有的参考信息**。

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
3. 可传入 `mainColor` 覆盖所有粒子 / Light / Renderer 颜色
4. 材质自动复制到 `Assets/AI_Generated/Materials/`，**不修改模板材质**
5. 可选择保存为新 Prefab 到 `Assets/AI_Generated/Prefabs/`

### 材质保护机制

`create_vfx_from_template` 在修改材质时，会自动：
1. `new Material(renderer.sharedMaterial)` 创建新实例
2. 保存到 `Assets/AI_Generated/Materials/`
3. 赋给 `renderer.sharedMaterial`
4. **不会修改原模板 Prefab 的材质**

## AI 工作流示例

```
1. create_magic_portal → 创建传送门
2. focus_scene_object → 聚焦查看
3. play_effect → 播放特效
4. capture_view → 截图保存
5. 用户查看截图后要求调整颜色/大小
6. set_transform / create_vfx_from_template 调参
7. save_prefab → 保存最终版本
```

## 常见问题

| 问题 | 原因 | 解决 |
|------|------|------|
| SceneView 截图失败 | 无打开的 SceneView 标签页 | Window → General → Scene |
| GameView 截图 fallback | Editor 下 ScreenCapture 不稳定 | 自动回退到 SceneView，message 中会说明 |
| Prefab 加载失败 | 路径不正确 | 使用 `list_generated_assets` 查看可用资源 |
| `list_generated_assets` 为空 | 未执行过任何保存操作 | 扫描范围 `Assets/AI_Generated/`，只返回 `.prefab` `.mat` `.png` |

## 安全限制

`clear_ai_generated_scene_objects` 的限制：
- `prefix` 不能为空，且至少 3 个字符
- 只删除当前活动场景中名称以 prefix 开头的**根对象**
- 不递归删除非匹配根对象的子对象
- 不删除任何 Asset 资源
- 默认 prefix = `"AI_"`
