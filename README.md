# Unity MCP Server

让 AI 助手（Claude、VS Code Copilot 等）通过 **Model Context Protocol (MCP)** 直接操控 Unity Editor —— 创建场景对象、粒子特效、材质、灯光、Prefab，并对特效进行调参、变色、缩放、变体批量截图和报告导出。

---

## 目录

- [1. 项目简介](#1-项目简介)
- [2. 功能特性](#2-功能特性)
- [3. 系统架构](#3-系统架构)
- [4. 目录结构](#4-目录结构)
- [5. 环境要求](#5-环境要求)
- [6. 安装步骤](#6-安装步骤)
- [7. Unity 插件安装方法](#7-unity-插件安装方法)
- [8. Python MCP Server 启动方法](#8-python-mcp-server-启动方法)
- [9. MCP Client 配置示例](#9-mcp-client-配置示例)
- [10. 阶段功能说明](#10-阶段功能说明)
- [11. MCP Tools 总览表](#11-mcp-tools-总览表)
- [12. HTTP Endpoints 总览表](#12-http-endpoints-总览表)
- [13. 关键组件说明](#13-关键组件说明)
- [14. 实现关键点](#14-实现关键点)
- [15. 安全设计](#15-安全设计)
- [16. 常见使用流程](#16-常见使用流程)
- [17. 测试脚本说明](#17-测试脚本说明)
- [18. 常见问题排查](#18-常见问题排查)
- [19. 如何扩展新工具](#19-如何扩展新工具)
- [20. 后续路线](#20-后续路线)

---

## 1. 项目简介

Unity MCP Server 由两部分组成：

| 组件 | 语言 | 职责 |
|------|------|------|
| **Python MCP Server** | Python | 实现 MCP 协议，暴露工具给 AI 客户端，通过 HTTP 转发请求给 Unity |
| **Unity Editor 插件** | C# | 内嵌 HTTP 服务器，接收请求并调用 Unity API 操作场景、资源 |

AI 客户端通过 MCP 协议发现并调用工具，Python MCP Server 将请求转为 HTTP JSON 发送给 Unity Editor，Unity 插件在 Editor 主线程上执行操作并返回结果。

---

## 2. 功能特性

- **基础场景操作**：创建空对象、设置 Transform、列举场景对象
- **粒子系统**：创建/调参 Particle System、设置 Burst、Shape、ColorOverLifetime、SizeOverLifetime、Velocity、Noise、Trails
- **灯光**：创建/调参 Point Light（颜色、强度、范围）
- **材质系统**：创建/分配材质，设置颜色、自发光、Shader 属性读写
- **Prefab 系统**：保存场景物体为 Prefab、实例化 Prefab
- **高级 VFX**：魔法传送门、火焰爆炸、闪电打击、治疗光环、烟雾爆发、斩击拖尾
- **预览与截图**：聚焦物体、播放/停止特效、SceneView/GameView 截图
- **模板系统**：从模板 Prefab 生成新特效
- **资源管理**：列举 AI_Generated 资产、按前缀清理场景、查询物体信息
- **特效调优**：粒子/灯光/LineRenderer 参数修改、整体重着色、缩放、时长调整
- **变体系统**：批量克隆并排列变体、批量截图变体
- **Shader 工具**：列出材质属性、设置材质属性（Float/Color/Keyword）
- **VFX Graph**（可选）：通过反射读写 VisualEffect 暴露属性、从 .vfx 模板创建
- **报告导出**：导出特效的组件、材质、变换信息为 JSON 报告

---

## 3. 系统架构

```
┌─────────────────────────────────────────────────┐
│                  AI Agent / MCP Client           │
│  (Claude Desktop, VS Code, Cursor, 等)           │
└─────────────────────┬───────────────────────────┘
                      │  MCP 协议 (stdio)
                      ▼
┌─────────────────────────────────────────────────┐
│              Python MCP Server                    │
│  server.py + tools/*.py                          │
│  FastMCP("unity-mcp-server")                     │
│  40 个 @mcp.tool() 工具                          │
└─────────────────────┬───────────────────────────┘
                      │  HTTP POST/GET
                      │  localhost:8765
                      ▼
┌─────────────────────────────────────────────────┐
│           Unity Editor HTTP Server                │
│  UnityMcpHttpServer.cs                           │
│  HttpListener :8765                              │
│  EditorApplication.delayCall → 主线程             │
└─────────────────────┬───────────────────────────┘
                      ▼
┌─────────────────────────────────────────────────┐
│          Unity Editor Tools (C#)                  │
│  UnityMcpRouter.cs → 38 routes                   │
│  Tools/*.cs → 场景/粒子/材质/灯光/截图/报告        │
│  Utils/*.cs → 颜色/路径/响应/VFX 工具             │
└─────────────────────┬───────────────────────────┘
                      ▼
┌─────────────────────────────────────────────────┐
│        Unity 场景 / 粒子 / 材质 / 灯光 / Prefab    │
└─────────────────────────────────────────────────┘
```

---

## 4. 目录结构

```
D:\zm\YTT_TOOLs\mcp-server\        ← 项目根目录（当前目录）
│
├── server.py                       Python MCP Server 入口
├── config.py                       配置（Unity URL、超时）
├── requirements.txt                Python 依赖
├── .gitignore                      Git 忽略规则
│
├── tools/                          Python 工具模块
│   ├── __init__.py
│   ├── unity_http.py               HTTP 请求封装
│   ├── connection_tools.py         连接测试
│   ├── scene_tools.py              场景操作
│   ├── vfx_tools.py                VFX 创建
│   ├── prefab_tools.py             Prefab 保存
│   ├── material_tools.py           材质操作
│   ├── preview_tools.py            预览/播放/截图
│   ├── template_tools.py           模板生成
│   ├── asset_tools.py              资产管理
│   ├── tuning_tools.py             特效调优
│   ├── variant_tools.py            变体工具
│   ├── shader_tools.py             Shader/VFX Graph
│   └── report_tools.py             报告导出
│
├── unity-plugin/                   Unity 插件源码
│   └── Assets/Editor/UnityMCP/
│       ├── UnityMcpHttpServer.cs   HTTP 服务器（监听 :8765）
│       ├── UnityMcpModels.cs       请求/响应模型
│       ├── UnityMcpRouter.cs       路由分发
│       ├── Tools/
│       │   ├── UnityMcpConnectionTools.cs
│       │   ├── UnityMcpSceneTools.cs
│       │   ├── UnityMcpVfxTools.cs
│       │   ├── UnityMcpPrefabTools.cs
│       │   ├── UnityMcpMaterialTools.cs
│       │   ├── UnityMcpPreviewTools.cs
│       │   ├── UnityMcpTemplateTools.cs
│       │   ├── UnityMcpAssetTools.cs
│       │   ├── UnityMcpTuningTools.cs
│       │   ├── UnityMcpVariantTools.cs
│       │   ├── UnityMcpShaderTools.cs
│       │   └── UnityMcpReportTools.cs
│       └── Utils/
│           ├── UnityMcpColorUtils.cs    颜色解析
│           ├── UnityMcpPathUtils.cs     安全路径校验
│           ├── UnityMcpResponseUtils.cs  JSON 响应封装
│           └── UnityMcpVfxUtils.cs      VFX 工具函数
│
├── docs/                           阶段性文档
│   ├── stage2-materials.md
│   ├── stage3-advanced-vfx.md
│   └── stage4-preview-template-workflow.md
│
└── test-stage*.ps1                 阶段性测试脚本
```

---

## 5. 环境要求

| 依赖 | 版本要求 |
|------|----------|
| Python | >= 3.10 |
| Unity | 2021 LTS ~ 2023 LTS |
| .NET | 兼容 Unity Mono / IL2CPP |
| 操作系统 | Windows 10+（HttpListener 基于 Windows） |

### Python 依赖

```
mcp          # Model Context Protocol Python SDK
requests     # HTTP 客户端
```

### Unity 可选包

- **Universal Render Pipeline (URP)** —— 推荐，提供更好的粒子 Shader 支持
- **Visual Effect Graph** —— 可选，不安装也不影响编译，相关工具会返回友好提示

---

## 6. 安装步骤

```powershell
# 1. 克隆或复制项目
cd D:\zm\YTT_TOOLs\mcp-server

# 2. 安装 Python 依赖
python -m pip install -r requirements.txt

# 3. 确认依赖安装成功
python -c "import mcp; import requests; print('OK')"
```

---

## 7. Unity 插件安装方法

```powershell
# 将 unity-plugin/Assets/Editor/UnityMCP/ 整个目录复制到你的 Unity 项目
# 目标路径：你的Unity项目/Assets/Editor/UnityMCP/

# 示例（假设 Unity 项目在 D:\MyUnityProject）：
Copy-Item -Recurse -Path "unity-plugin/Assets/Editor/UnityMCP" `
    -Destination "D:\MyUnityProject\Assets\Editor\UnityMCP"
```

在 Unity Editor 中：
1. 等待脚本编译完成
2. 菜单栏出现 **Unity MCP > Start Server**
3. 点击 **Unity MCP > Start Server** 启动 HTTP 服务
4. 也可以随 Editor 启动自动运行（`[InitializeOnLoad]`）
5. 确认控制台输出 `[UnityMCP] Unity MCP HTTP Server started at http://localhost:8765/`

> 注意：`unity-plugin/Assets/Editor/UnityMCP/` 是**源码目录**，不是 Unity 项目的 Assets 根目录。你必须手动将其复制到真实 Unity 项目的对应位置。

---

## 8. Python MCP Server 启动方法

```powershell
cd D:\zm\YTT_TOOLs\mcp-server
python server.py
```

启动后控制台输出类似：
```
2025-06-06 10:00:00,000 - INFO - Running MCP server...
2025-06-06 10:00:00,000 - INFO - Registered 40 tools
```

Server 默认使用 **stdio 传输**，由 MCP Client 启动并管理生命周期。

---

## 9. MCP Client 配置示例

### VS Code (Cline / Continue 扩展)

```json
{
  "mcpServers": {
    "unity-mcp-server": {
      "command": "python",
      "args": ["D:\\zm\\YTT_TOOLs\\mcp-server\\server.py"],
      "cwd": "D:\\zm\\YTT_TOOLs\\mcp-server"
    }
  }
}
```

### Claude Desktop

```json
{
  "mcpServers": {
    "unity-mcp-server": {
      "command": "python",
      "args": ["D:\\zm\\YTT_TOOLs\\mcp-server\\server.py"],
      "cwd": "D:\\zm\\YTT_TOOLs\\mcp-server"
    }
  }
}
```

### Cursor

在 Cursor 设置中添加 MCP Server，Command 指向 `python`，Args 指向 `server.py` 的完整路径。

---

## 10. 阶段功能说明

### 阶段一：基础场景工具

| 工具 | 功能 |
|------|------|
| `create_empty` | 创建空 GameObject |
| `list_scene_objects` | 列举场景中所有物体 |
| `set_transform` | 设置物体的位置/旋转/缩放 |
| `create_particle_effect` | 创建基础粒子系统 |
| `create_light` | 创建点光源 |
| `save_prefab` | 将场景物体保存为 Prefab |

### 阶段二：材质系统

| 工具 | 功能 |
|------|------|
| `create_material` | 创建材质（指定颜色、Shader、自发光） |
| `assign_material` | 将材质应用到物体 |
| `create_additive_particle_material` | 创建透明叠加粒子材质 |
| `set_material_color` | 修改材质颜色 |
| `set_material_emission` | 修改材质自发光 |

### 阶段三：高级 VFX

| 工具 | 功能 |
|------|------|
| `create_magic_portal` | 魔法传送门（环状粒子 + 核心 + 火花 + LineRenderer 光圈 + 灯光） |
| `create_fire_explosion` | 火焰爆炸（火焰 + 烟雾 + 火花 + 闪光灯） |
| `create_lightning_hit` | 闪电打击（主闪电 ZigZag + 分支 + 火花 + 灯光） |
| `create_heal_aura` | 治疗光环（地面光圈 + 上升粒子 + 闪烁粒子 + 灯光） |
| `create_smoke_burst` | 烟雾爆发（主烟 + 飘散 + 地面尘环） |
| `create_slash_trail` | 斩击拖尾（弧线 LineRenderer + 火花 + 灯光） |

### 阶段四：预览与工作流

| 工具 | 功能 |
|------|------|
| `focus_scene_object` | 聚焦并选中场景物体 |
| `play_effect` | 播放特效（含子物体粒子和粒子系统） |
| `stop_effect` | 停止特效（可选清除粒子） |
| `capture_view` | 截图 SceneView 或 GameView 为 PNG |
| `create_vfx_from_template` | 从模板 Prefab 生成新特效 |
| `instantiate_prefab` | 实例化 Prefab 到场景 |
| `list_generated_assets` | 列举 AI_Generated 目录资产 |
| `clear_ai_generated_scene_objects` | 按前缀清理场景物体 |
| `get_object_info` | 查询物体详细信息 |

### 阶段五：调优与报告

| 工具 | 功能 |
|------|------|
| `update_particle_system` | 调整粒子系统参数（时长、速率、生命周期、速度、大小、颜色） |
| `update_light` | 调整灯光参数（颜色、强度、范围） |
| `update_line_renderer` | 调整 LineRenderer 参数（颜色、宽度） |
| `recolor_effect` | 整体重着色（粒子 + 灯光 + 渲染器 + LineRenderer） |
| `scale_effect` | 整体缩放（Transform + 粒子大小 + 粒子速度） |
| `adjust_effect_timing` | 调整特效时长和播放速度 |
| `create_effect_variants` | 批量克隆并排列变体 |
| `capture_effect_variants` | 批量截图多个变体 |
| `list_material_properties` | 列出材质的所有 Shader 属性 |
| `set_material_property` | 设置材质的 Shader 属性（Float/Color/Keyword） |
| `set_vfx_graph_property` | 设置 VFX Graph 暴露属性（反射，可选能力） |
| `create_vfx_graph_from_template` | 从 .vfx 模板创建 VFX Graph |
| `export_effect_report` | 导出特效 JSON 报告 |

---

## 11. MCP Tools 总览表

总计 **40 个 MCP 工具**，按模块分组：

| 模块 | 数量 | 工具列表 |
|------|------|----------|
| connection_tools | 1 | `ping_unity` |
| scene_tools | 3 | `create_empty`, `list_scene_objects`, `set_transform` |
| vfx_tools | 8 | `create_particle_effect`, `create_light`, `create_magic_portal`, `create_fire_explosion`, `create_lightning_hit`, `create_heal_aura`, `create_smoke_burst`, `create_slash_trail` |
| prefab_tools | 1 | `save_prefab` |
| material_tools | 5 | `create_material`, `assign_material`, `create_additive_particle_material`, `set_material_color`, `set_material_emission` |
| preview_tools | 4 | `focus_scene_object`, `play_effect`, `stop_effect`, `capture_view` |
| template_tools | 2 | `create_vfx_from_template`, `instantiate_prefab` |
| asset_tools | 3 | `list_generated_assets`, `clear_ai_generated_scene_objects`, `get_object_info` |
| tuning_tools | 6 | `update_particle_system`, `update_light`, `update_line_renderer`, `recolor_effect`, `scale_effect`, `adjust_effect_timing` |
| variant_tools | 2 | `create_effect_variants`, `capture_effect_variants` |
| shader_tools | 4 | `list_material_properties`, `set_material_property`, `set_vfx_graph_property`, `create_vfx_graph_from_template` |
| report_tools | 1 | `export_effect_report` |

---

## 12. HTTP Endpoints 总览表

总计 **36 个 POST + 2 个 GET = 38 个 HTTP Endpoints**，监听于 `http://localhost:8765/`。

### GET

| 路径 | 功能 |
|------|------|
| `/ping` | 连接测试，返回 `pong` |
| `/list-scene-objects` | 列举场景物体 |

### POST — 阶段一

| 路径 | 请求模型 |
|------|----------|
| `/create-empty` | `{name, x, y, z}` |
| `/set-transform` | `{objectName, x, y, z, rx, ry, rz, sx, sy, sz}` |
| `/create-particle-effect` | `{effectName, color, duration, emissionRate, startLifetime, startSpeed, startSize, radius, loop}` |
| `/create-light` | `{name, color, intensity, range, x, y, z}` |
| `/save-prefab` | `{objectName, prefabPath}` |

### POST — 阶段二

| 路径 | 请求模型 |
|------|----------|
| `/create-material` | `{materialName, color, shaderName, emissionColor, emissionIntensity}` |
| `/assign-material` | `{objectName, materialPath}` |
| `/create-additive-particle-material` | `{materialName, color, emissionIntensity}` |
| `/set-material-color` | `{materialPath, color}` |
| `/set-material-emission` | `{materialPath, emissionColor, emissionIntensity}` |

### POST — 阶段三

| 路径 | 请求模型 |
|------|----------|
| `/create-magic-portal` | `{effectName, mainColor, radius, duration, loop, saveAsPrefab}` |
| `/create-fire-explosion` | `{effectName, radius, intensity, duration, saveAsPrefab}` |
| `/create-lightning-hit` | `{effectName, mainColor, height, radius, duration, branchCount, saveAsPrefab}` |
| `/create-heal-aura` | `{effectName, mainColor, radius, duration, loop, density, saveAsPrefab}` |
| `/create-smoke-burst` | `{effectName, color, radius, duration, density, saveAsPrefab}` |
| `/create-slash-trail` | `{effectName, mainColor, length, width, duration, saveAsPrefab}` |

### POST — 阶段四

| 路径 | 请求模型 |
|------|----------|
| `/focus-scene-object` | `{objectName}` |
| `/play-effect` | `{objectName, includeChildren}` |
| `/stop-effect` | `{objectName, includeChildren, clearParticles}` |
| `/capture-view` | `{fileName, viewType, width, height}` |
| `/create-vfx-from-template` | `{templatePath, outputName, x, y, z, scale, mainColor, saveAsPrefab}` |
| `/instantiate-prefab` | `{prefabPath, objectName, x, y, z, scale}` |
| `/list-generated-assets` | `{assetType}` |
| `/clear-ai-generated-scene-objects` | `{prefix}` |
| `/get-object-info` | `{objectName, includeChildren}` |

### POST — 阶段五

| 路径 | 请求模型 |
|------|----------|
| `/update-particle-system` | `{objectName, color, duration, emissionRate, startLifetime, startSpeed, startSize, loop}` |
| `/update-light` | `{objectName, color, intensity, range}` |
| `/update-line-renderer` | `{objectName, color, width, sx, sy}` |
| `/recolor-effect` | `{objectName, color, affectParticles, affectLights, affectRenderers, affectLines}` |
| `/scale-effect` | `{objectName, scaleMultiplier, scaleTransform, scaleParticleSize, scaleParticleSpeed, affectParticles}` |
| `/adjust-effect-timing` | `{objectName, duration, durationMultiplier, speedMultiplier}` |
| `/create-effect-variants` | `{sourceObjectName, count, spacing, variantPrefix}` |
| `/capture-effect-variants` | `{objectPrefix, filePrefix, viewType}` |
| `/list-material-properties` | `{objectName, materialPath}` |
| `/set-material-property` | `{objectName, materialPath, propertyName, propertyType, value}` |
| `/set-vfx-graph-property` | `{objectName, propertyName, propertyType, value}` |
| `/create-vfx-graph-from-template` | `{templatePath, outputName}` |
| `/export-effect-report` | `{objectName, fileName}` |

---

## 13. 关键组件说明

### Python 层

- **`server.py`** —— 入口，创建 `FastMCP("unity-mcp-server")`，注册所有工具模块，运行 MCP 传输层
- **`tools/unity_http.py`** —— 封装 `get_from_unity()` 和 `post_to_unity()`，统一处理 HTTP 请求、超时和错误响应
- **`tools/*_tools.py`** —— 每个文件包含一个 `register_xxx_tools(mcp)` 函数，使用 `@mcp.tool()` 装饰器注册工具；工具函数内部构造 JSON payload 并调用 Unity HTTP 接口
- **`config.py`** —— `UNITY_BASE_URL = "http://localhost:8765"`，`HTTP_TIMEOUT = 30`

### Unity C# 层

- **`UnityMcpHttpServer.cs`** —— `HttpListener` 在后台线程监听 `:8765`，通过 `EditorApplication.delayCall` 将请求回调到 Unity 主线程执行，避免线程安全问题
- **`UnityMcpRouter.cs`** —— 静态字典映射路径 → 处理方法，`RouteGet()` / `RoutePost()` 分发
- **`UnityMcpModels.cs`** —— 统一的 `RequestModel`（所有请求字段在一个类中）和 `ResponseModel`（成功/状态/数据）
- **`Tools/*.cs`** —— 每个文件对应一类功能，接收 `RequestModel` 返回 JSON 字符串
- **`Utils/UnityMcpVfxUtils.cs`** —— VFX 构建工具函数库，包括粒子配置、LineRenderer 创建、闪电生成、材质创建等
- **`Utils/UnityMcpPathUtils.cs`** —— 路径安全校验和自动编号保存路径生成
- **`Utils/UnityMcpColorUtils.cs`** —— HTML 颜色解析 (#RGB/#RRGGBB/#RRGGBBAA)
- **`Utils/UnityMcpResponseUtils.cs`** —— `Success()` / `Error()` 快捷创建响应 JSON

---

## 14. 实现关键点

### 线程安全

- Unity API 必须在主线程调用
- `HttpListener` 在后台线程接收请求
- 通过 `EditorApplication.delayCall` 将处理逻辑排队到主线程
- 响应通过闭包跨线程传递回发送者

### Shader 兼容

- 使用 Shader 优先级回退机制：`URP > Built-in > Standard`
- 自动检测 Shader 属性名（`_BaseColor` / `_Color` / `_EmissionColor`）
- 材质方法兼容 URP / Built-in / HDRP 管线

### VFX Graph 可选性

- 通过 `Type.GetType("UnityEngine.VFX.VisualEffect, UnityEngine.VFXModule")` 反射访问
- 如果未安装 VFX Graph 包，返回友好错误消息而非编译错误
- 所有 VFX Graph 代码包裹在 try-catch 中

### 粒子系统兼容

- `ParticleSystem.Burst` 构造函数第二个参数为 `short` 类型，需显式强转
- `VelocityOverLifetimeModule` 的 `.x` / `.y` / `.z` 为 `MinMaxCurve` 类型，不能直接赋 `float`
- `ParticleSystemSortMode.DistanceToCamera` 在部分 Unity 版本不存在，改用 `Distance`

### 安全设计（详见下一节）

### 资源隔离

- 所有生成的材质、Prefab、截图、报告限制在 `Assets/AI_Generated/` 下
- 不存在 `..` 路径穿越风险

### 路径自动编号

- 保存文件时自动检测重名并追加编号（`Name_1.mat`、`Name_2.mat`）
- 使用 `AssetDatabase.LoadAssetAtPath` 检测已存在资产

---

## 15. 安全设计

| 措施 | 说明 |
|------|------|
| **路径白名单** | 所有资产路径必须以 `Assets/` 开头，禁止 `..` 穿越 |
| **路径分类校验** | `IsSafeGeneratedMaterialPath`、`IsSafeCapturePath`、`IsSafeReportPath` 等细化校验 |
| **文件名消毒** | `SanitizeFileName` 移除 `\ / : * ? " < > |` 等非法字符 |
| **参数 Clamp** | 所有数值参数都经过 `Mathf.Clamp`，防止极端值 |
| **禁止任意代码执行** | 不提供任何执行任意 C# 代码的工具 |
| **仅限 localhost** | HTTP Listener 只绑定 `http://localhost:8765/`，不接受外部网络请求 |
| **资源隔离** | 所有生成资产限制在 `Assets/AI_Generated/`，不影响项目原有资源 |
| **自动编号** | 防止文件名冲突和覆盖已有资产 |

---

## 16. 常见使用流程

### 完整工作流：创建 → 预览 → 调参 → 变体 → 截图 → 报告

```powershell
# 1. 创建特效
$body = @{ effectName = "MyPortal"; mainColor = "#33AAFF"; radius = 2.0; duration = 5.0; loop = $true; saveAsPrefab = $true } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8765/create-magic-portal" -Method Post -Body $body -ContentType "application/json"

# 2. 播放并聚焦
$body = @{ objectName = "MyPortal"; includeChildren = $true } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8765/play-effect" -Method Post -Body $body -ContentType "application/json"
$body = @{ objectName = "MyPortal" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8765/focus-scene-object" -Method Post -Body $body -ContentType "application/json"

# 3. 截图
$body = @{ fileName = "MyPortal_Shot"; viewType = "scene"; width = 1920; height = 1080 } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8765/capture-view" -Method Post -Body $body -ContentType "application/json"

# 4. 调参（变色 + 缩放）
$body = @{ objectName = "MyPortal"; color = "#FF44AA"; affectParticles = $true; affectLights = $true; affectRenderers = $true; affectLines = $true } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8765/recolor-effect" -Method Post -Body $body -ContentType "application/json"
$body = @{ objectName = "MyPortal"; scaleMultiplier = 1.5; scaleTransform = $true; scaleParticleSize = $true; scaleParticleSpeed = $true; affectParticles = $true } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8765/scale-effect" -Method Post -Body $body -ContentType "application/json"

# 5. 批量变体
$body = @{ sourceObjectName = "MyPortal"; count = 4; spacing = 3.0; variantPrefix = "MyPortal" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8765/create-effect-variants" -Method Post -Body $body -ContentType "application/json"

# 6. 批量截图变体
$body = @{ objectPrefix = "MyPortal_"; filePrefix = "Portal_Variant"; viewType = "front" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8765/capture-effect-variants" -Method Post -Body $body -ContentType "application/json"

# 7. 导出报告
$body = @{ objectName = "MyPortal"; fileName = "MyPortal_Report" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8765/export-effect-report" -Method Post -Body $body -ContentType "application/json"

# 8. 清理
$body = @{ prefix = "MyPortal" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8765/clear-ai-generated-scene-objects" -Method Post -Body $body -ContentType "application/json"
```

### 测试连接

```powershell
Invoke-RestMethod -Uri "http://localhost:8765/ping" -Method Get
# 预期: {"success":true,"message":"pong","objectName":"UnityMCP"}
```

---

## 17. 测试脚本说明

项目根目录包含 4 个 PowerShell 测试脚本，按阶段组织：

| 脚本 | 测试内容 | 工具数 |
|------|----------|--------|
| `test-stage2.ps1` | 材质系统 | 10 步 |
| `test-stage3.ps1` | 高级 VFX | 8 步 |
| `test-stage4.ps1` | 预览与工作流 | 11 步 |
| `test-stage5.ps1` | 调优/变体/Shader/报告 | 16 步 |

所有测试脚本模式一致：

```powershell
$BaseUrl = "http://localhost:8765"

function Test-Step {
    param($Number, $Description, $ScriptBlock)
    Write-Host "$Number. $Description..." -ForegroundColor Yellow
    try {
        $result = & $ScriptBlock
        if (-not $result.success) { throw "FAILED: $($result.message)" }
        Write-Host "   OK" -ForegroundColor Green
    } catch {
        Write-Host "   FAILED: $_" -ForegroundColor Red
        exit 1
    }
}

# 使用 Invoke-RestMethod（推荐，不要用 curl.exe）
Test-Step -Number 1 -Description "Ping Unity" -ScriptBlock {
    Invoke-RestMethod -Uri "$BaseUrl/ping" -Method Get
}
```

---

## 18. 常见问题排查

### 连接相关

| 问题 | 可能原因 | 解决方法 |
|------|----------|----------|
| `ping` 超时 | Unity 未启动 HTTP Server | 在 Unity 菜单栏点击 Unity MCP > Start Server |
| `ping` 连接拒绝 | 端口 8765 被占用 | 检查其他进程：`netstat -ano \| findstr :8765` |
| 请求返回空响应 | Unity 编译中 | 等待 Unity 编译完成后再试 |
| 偶发线程异常 | Unity API 线程要求 | 所有请求已通过 delayCall 分发到主线程 |

### 特效相关

| 问题 | 可能原因 | 解决方法 |
|------|----------|----------|
| 粒子不显示 | 粒子材质为 Built-in 粒子 Shader 但使用 URP 管线 | 安装 URP 包或创建 URP 兼容材质 |
| 自发光不亮 | Shader 不支持 `_EMISSION` | 使用 Standard 或 URP Lit Shader |
| LineRenderer 不显示 | 没有灯光或 SceneView 不显示 | 确保 SceneView 有环境光 |
| 截图全黑 | viewType 为 game 但 GameView 未渲染 | 使用 `"viewType": "scene"` |
| VFX Graph 工具报 Not Available | 未安装 VFX Graph 包 | 通过 Package Manager 安装，或忽略此功能（不影响其他工具） |

### PowerShell 测试问题

| 问题 | 解决方法 |
|------|----------|
| `Invoke-RestMethod` 返回空 | 确保 Unity HTTP Server 已启动；检查 `$ErrorActionPreference` |
| JSON 转义问题 | 使用 `ConvertTo-Json` 构造 body，**不要**手动拼接 JSON 字符串 |
| `curl.exe` 参数复杂 | 推荐 `Invoke-RestMethod`，不要用 `curl.exe` |
| 中文乱码 | 文件保存为 UTF-8 with BOM；PowerShell 7+ 兼容更好 |

---

## 19. 如何扩展新工具

### Python 端

```python
# tools/my_new_tools.py
from tools.unity_http import post_to_unity

def register_my_tools(mcp):
    @mcp.tool()
    def my_new_tool(param1: str = "default", param2: float = 1.0) -> dict:
        """工具描述（将作为 MCP tool description 暴露给 AI 客户端）"""
        payload = {"param1": param1, "param2": param2}
        return post_to_unity("/my-new-endpoint", payload)
```

然后在 `server.py` 中添加：

```python
from tools.my_new_tools import register_my_tools
register_my_tools(mcp)
```

### Unity C# 端

```csharp
// 1. 在 UnityMcpModels.cs 的 RequestModel 中添加新字段（如有必要）

// 2. 创建新的工具类
// Tools/UnityMcpMyNewTools.cs
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpMyNewTools
    {
        public static string MyNewAction(RequestModel req)
        {
            // 读取参数
            string param = req.someField;
            // 执行操作
            // ...
            return UnityMcpResponseUtils.Success("操作完成");
        }
    }
}

// 3. 在 UnityMcpRouter.cs 的 _postRoutes 中添加路由
{ "/my-new-endpoint", (req) => UnityMcpMyNewTools.MyNewAction(req) }
```

### 测试

在 `test-stage5.ps1` 或创建新的测试脚本，按照现有模式添加测试步骤。

---

## 20. 后续路线

- [ ] **Terrains** —— 创建和编辑 Terrain
- [ ] **Animations** —— 控制 Animation/Animator 组件
- [ ] **Audio** —— 创建和播放 AudioSource
- [ ] **UI** —— 创建和操作 uGUI 元素
- [ ] **Cinemachine** —— 控制虚拟相机
- [ ] **Timeline** —— 创建和控制 Timeline
- [ ] **ScriptableObject** —— 读取和编辑数据资产
- [ ] **NavMesh** —— 寻路相关操作
- [ ] **GLTF/FBX 导入** —— 运行时导入外部模型
- [ ] **场景序列化** —— 保存和恢复场景状态

---

> 项目地址：`D:\zm\YTT_TOOLs\mcp-server`  
> Unity 插件源码：`unity-plugin/Assets/Editor/UnityMCP/`  
> 问题反馈：请在仓库提交 Issue
