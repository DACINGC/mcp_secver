# Stage 2: Material System

> 本篇为材质系统的详细参考文档。工具列表和端点已在 README 中完整列出，此处仅保留 **文档内独有的参考信息**。

## Shader 回退机制

如果指定 Shader 不存在，会自动按以下顺序回退：
1. `Universal Render Pipeline/Particles/Unlit`
2. `Universal Render Pipeline/Lit`
3. `Particles/Standard Unlit`
4. `Standard`
5. `Unlit/Color`

所有回退均不存在的极端情况下返回错误。

## 材质路径规则

`material_path` 必须满足：
- 以 `Assets/` 开头
- 不包含 `..`（禁止路径穿越）
- 以 `.mat` 结尾

## 管线差异

| 管线 | 推荐 Shader | 颜色属性 | 自发光属性 |
|------|------------|----------|-----------|
| **URP** | `Universal Render Pipeline/Particles/Unlit` | `_BaseColor` | `_EmissionColor` |
| **Built-in** | `Standard` | `_Color` | `_EmissionColor` + `_Mode` |
| **HDRP** | （未专门适配） | — | — |

材质创建时使用 `HasProperty` 检测属性存在性，不会因属性缺失报错。

## 常见问题排错

| 问题 | 原因 | 解决 |
|------|------|------|
| 创建成功但看不到发光 | Shader 不支持 `_EmissionColor` / 场景无后处理 / 强度太低 | 建议强度 1~5，检查 Lighting 设置中的 Emission |
| `assign_material` 的 `affectedCount` 为 0 | 对象名拼写错误 / 无 Renderer / 路径不合法 | 先用 `list_scene_objects` 确认对象名 |
