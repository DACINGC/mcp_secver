# Unity MCP Server

让 AI 助手（Claude、VS Code Copilot 等）通过 **Model Context Protocol (MCP)** 直接操控 Unity Editor  创建场景对象、粒子特效、材质、灯光、Prefab，并对特效进行调参、变色、缩放、变体批量截图和报告导出。

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
- [9. Web Dashboard 可视化管理界面](#9-web-dashboard-可视化管理界面)
- [9.5 文本命令系统](#95-文本命令系统)
- [9.5 文本命令系统](#95-文本命令系统)
- [10. Unity Editor 服务器仪表盘](#10-unity-editor-服务器仪表盘)
- [11. MCP Client 配置示例](#11-mcp-client-配置示例)
- [12. 阶段功能说明](#12-阶段功能说明)
- [13. MCP Tools 总览表](#13-mcp-tools-总览表)
- [14. HTTP Endpoints 总览表](#14-http-endpoints-总览表)
- [15. 关键组件说明](#15-关键组件说明)
- [16. 实现关键点](#16-实现关键点)
- [17. 安全设计](#17-安全设计)
- [18. 常见使用流程](#18-常见使用流程)
- [19. 测试脚本说明](#19-测试脚本说明)
- [20. 常见问题排查](#20-常见问题排查)
- [21. 如何扩展新工具](#21-如何扩展新工具)
- [22. Agent 操作指南：创建特效的标准流程](#22-agent-操作指南创建特效的标准流程)
- [23. 后续路线](#23-后续路线)

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

                  AI Agent / MCP Client           
  (Claude Desktop, VS Code, Cursor, 等)           

                        MCP 协议 (stdio)
                      

              Python MCP Server                    
  server.py + tools/*.py                          
  FastMCP("unity-mcp-server")                     
  40 个 @mcp.tool() 工具                          

                        HTTP POST/GET
                        localhost:8765
                      

           Unity Editor HTTP Server                
  UnityMcpHttpServer.cs                           
  HttpListener :8765                              
  EditorApplication.delayCall  主线程             

                      

          Unity Editor Tools (C#)                  
  UnityMcpRouter.cs  38 routes                   
  Tools/*.cs  场景/粒子/材质/灯光/截图/报告        
  Utils/*.cs  颜色/路径/响应/VFX 工具             

                      

        Unity 场景 / 粒子 / 材质 / 灯光 / Prefab    

```

---

## 4. 目录结构

```
D:\zm\YTT_TOOLs\mcp-server\         项目根目录（当前目录）

 server.py                       Python MCP Server 入口
 dashboard.py                    Web Dashboard 后端（进程管理 + Unity API 代理 + 文本命令解析引擎）
 config.py                       配置（Unity URL、超时）
 config.py                       配置（Unity URL、超时）
 requirements.txt                Python 依赖
 .gitignore                      Git 忽略规则

 tools/                          Python 工具模块
    __init__.py
    unity_http.py               HTTP 请求封装
    connection_tools.py         连接测试
    scene_tools.py              场景操作
    vfx_tools.py                VFX 创建
    prefab_tools.py             Prefab 保存
    material_tools.py           材质操作
    preview_tools.py            预览/播放/截图
    template_tools.py           模板生成
    asset_tools.py              资产管理
    tuning_tools.py             特效调优
    variant_tools.py            变体工具
    shader_tools.py             Shader/VFX Graph
    report_tools.py             报告导出

 templates/                      Web Dashboard 前端页面
    dashboard.html              Dashboard HTML（深色主题 UI）

 unity-plugin/                   Unity 插件源码
    Assets/Editor/UnityMCP/
        UnityMcpDashboard.cs    服务器仪表盘（EditorWindow，F12 打开）
        UnityMcpHttpServer.cs   HTTP 服务器（监听 :8765）
        UnityMcpModels.cs       请求/响应模型
        UnityMcpRouter.cs       路由分发
        Tools/
           UnityMcpConnectionTools.cs
           UnityMcpSceneTools.cs
           UnityMcpVfxTools.cs
           UnityMcpPrefabTools.cs
           UnityMcpMaterialTools.cs
           UnityMcpPreviewTools.cs
           UnityMcpTemplateTools.cs
           UnityMcpAssetTools.cs
           UnityMcpTuningTools.cs
           UnityMcpVariantTools.cs
           UnityMcpShaderTools.cs
           UnityMcpReportTools.cs
        Utils/
            UnityMcpColorUtils.cs    颜色解析
            UnityMcpPathUtils.cs     安全路径校验
            UnityMcpResponseUtils.cs  JSON 响应封装
            UnityMcpVfxUtils.cs      VFX 工具函数

 docs/                           阶段性文档
    stage2-materials.md
    stage3-advanced-vfx.md
    stage4-preview-template-workflow.md

 test-stage*.ps1                 阶段性测试脚本
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
mcp              # Model Context Protocol Python SDK
requests         # HTTP 客户端
flask            # Web Dashboard 后端（可选）
flask-cors       # Flask 跨域支持（可选）
```

### Unity 可选包

- **Universal Render Pipeline (URP)**  推荐，提供更好的粒子 Shader 支持
- **Visual Effect Graph**  可选，不安装也不影响编译，相关工具会返回友好提示

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
4. 按 **F12** 或点击 **Unity MCP > Dashboard** 打开服务器仪表盘
5. 也可以随 Editor 启动自动运行（`[InitializeOnLoad]`）
6. 确认控制台输出 `[UnityMCP] Unity MCP HTTP Server started at http://localhost:8765/`

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

## 9. Web Dashboard 可视化管理界面

项目提供了基于 Flask 的 Web Dashboard，用于可视化管理 MCP Server 和直接操控 Unity。

### 启动方式

```powershell
cd D:\zm\YTT_TOOLs\mcp-server
pip install flask flask-cors
python dashboard.py
```

启动后打开 **http://localhost:5100** 即可看到 Dashboard 界面。

### 功能介绍

Dashboard 提供四大功能模块：

**服务器管理（顶部栏）**
- 实时显示 MCP Server 运行状态、PID、运行时间
- 点击按钮启动/停止/重启 MCP Server 进程
- 实时滚动日志查看器，支持自动滚动、清除、复制

**⌨️ 命令控制台（右侧面板顶部）**
- 文本命令输入框，支持自然语言语法（如 `create sphere named MySphere at 0 1 0 radius 0.5`）
- 按 `Enter` 或点击 `↵` 按钮执行命令，自动调用对应的 Unity HTTP 端点
- **📖 帮助按钮** — 弹出模态窗口，分类别展示所有可用命令的语法、示例和说明
- **🗑 清除按钮** — 清空命令执行历史
- 执行历史实时显示 ✅/❌ 状态，支持查看返回的 JSON 详情
- 创建命令执行后自动聚焦场景物体

**🎨 Unity 创建工具（右侧面板）**
-  **基础物体**：创建空 GameObject、点光源
-  **特效**：一键创建 6 种 VFX（火焰爆炸、魔法传送门、闪电打击、治疗光环、烟雾爆发、斩击拖尾），自动播放并聚焦
-  **播放控制**：播放/停止特效、聚焦场景物体、列举场景、按前缀清理
- 自定义物体名称和颜色

**连接与配置**
-  Ping Unity 测试连接状态
- 查看 Unity URL、超时时间、Python 版本等配置信息

### 架构

```
浏览器 (http://localhost:5100)
    │
    │  REST API (JSON)
    │
dashboard.py (Flask)
    │
    ├── 进程管理 → MCP Server (server.py)
    │
    ├── Unity API 代理 → localhost:8765 → Unity Editor
    │
    └── 命令解析引擎 (parse_command)
            │
            ├── 基础物体 / 粒子特效 / 材质
            ├── 物体操作 / 调优 / 变体
            └── 全部映射到 Unity HTTP Endpoints
```

---

## 10. Unity Editor 服务器仪表盘

项目为 Unity Editor 内置了一个 **EditorWindow 仪表盘**，可直接在 Unity 中查看和管理 MCP HTTP 服务器状态，无需依赖外部 Web 服务。

### 打开方式

| 方式 | 操作 |
|------|------|
| **快捷键** | 在 Unity Editor 中按 **F12** |
| **菜单栏** | **Unity MCP → Dashboard** |

### 功能界面

仪表盘分为四个功能卡片：

**① 服务器状态卡片**
- 绿色/灰色 状态指示灯实时显示服务器是否运行中
- 显示端口号（8765）和完整监听地址，支持一键复制
- 显示服务模式（HTTP / JSON）

**② 控制卡片**
- **▶ 启动** — 启动 HTTP 服务器（服务器已运行时自动禁用）
- **■ 停止** — 停止 HTTP 服务器
- **↻ 重启** — 停止后自动重新启动

**③ 连接检测卡片**
- **Ping 测试** 按钮 — 异步发送 GET 请求到 `http://localhost:8765/ping`
- 显示检测结果：成功时绿色 + 延迟毫秒数，失败时红色 + 错误信息
- 服务器未启动时显示提示信息

**④ 活动日志卡片**
- 自动捕获所有以 `[UnityMCP]` 为前缀的 Unity 日志，带时间戳
- 错误日志红色高亮
- 最多保留 100 条，支持一键清空

### 实现文件

| 文件 | 说明 |
|------|------|
| `UnityMcpDashboard.cs` | EditorWindow 实现（新增） |
| `UnityMcpHttpServer.cs` | 新增 `IsRunning` 公开属性供仪表盘读取状态 |

### 与 Web Dashboard 的关系

- **Unity Editor 仪表盘**：直接在 Unity Editor 中运行，适合快速查看服务器状态、启停控制、Ping 连接测试
- **Web Dashboard**（`dashboard.py` + `templates/dashboard.html`）：Flask 后端，提供进程管理、Unity 创建工具、命令控制台、日志查看等功能
- 两者可以同时使用，互不冲突

---

## 9.5 文本命令系统

Dashboard 内嵌了一套完整的**文本命令解析引擎**，让你可以直接在命令输入框中用自然语言语法操控 Unity，无需手动构造 JSON 或记忆具体端点。

### 命令语法

命令采用 `动作 类型 [参数...]` 的格式，支持以下关键词参数：

| 关键词 | 说明 | 示例 |
|--------|------|------|
| `named <name>` | 指定物体/特效名称 | `named MySphere` |
| `at x y z` | 位置坐标 | `at 0 1 0` |
| `color #RRGGBB` | 十六进制颜色 | `color #FF4400` |
| `radius n` | 半径 | `radius 2.5` |
| `duration n` | 持续时间（秒） | `duration 3.0` |
| `intensity n` | 强度 | `intensity 1.2` |
| `loop true/false` | 是否循环 | `loop true` |
| `rate n` | 粒子发射速率 | `rate 100` |
| `speed n` | 粒子速度 | `speed 5` |
| `size n` | 粒子大小 | `size 0.3` |

### 可用命令类别

| 类别 | 命令 | 示例 |
|------|------|------|
| **基础物体** | `create empty / light / cube / sphere / cylinder / plane named <name> ...` | `create sphere named MySphere at 0 1 0 radius 0.5` |
| **粒子特效** | `create particle / fire / portal / lightning / heal / smoke / slash named <name> ...` | `create fire named Blast radius 2.5 duration 1.5` |
| **材质** | `create material named <name> ...` / `assign material <path> to <object>` | `create material named MyMat color #FF5733` |
| **物体操作** | `focus / play / stop / info / list objects / clear / move ...` | `move MyObject to 3 1 0 rx 0 ry 45` |
| **特效调优** | `recolor / scale / timing / update particle / update light ...` | `recolor MyEffect to #FF3366` |
| **变体 & 其他** | `variants / save prefab / capture / report ...` | `variants MyEffect count 3 spacing 3` |

### 执行流程

```
输入命令 → POST /api/unity-command → parse_command() 解析
    → 匹配端点 + 构造参数 → POST/GET Unity HTTP
    → 返回结果 → 展示在结果面板
    → 自动聚焦创建的新物体 (create 命令)
```

### 帮助弹窗

点击命令控制台顶部的 **📖 帮助** 按钮，弹出带分类表格的模态窗口，列出所有可用命令的语法、示例和说明，数据来源于 `AVAILABLE_COMMANDS` 字典并通过 `/api/available-commands` 接口提供。

---

## 11. MCP Client 配置示例

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

## 12. 阶段功能说明

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

## 13. MCP Tools 总览表

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

## 14. HTTP Endpoints 总览表

总计 **36 个 POST + 2 个 GET = 38 个 HTTP Endpoints**，监听于 `http://localhost:8765/`。

### GET

| 路径 | 功能 |
|------|------|
| `/ping` | 连接测试，返回 `pong` |
| `/list-scene-objects` | 列举场景物体 |

### POST  阶段一

| 路径 | 请求模型 |
|------|----------|
| `/create-empty` | `{name, x, y, z}` |
| `/set-transform` | `{objectName, x, y, z, rx, ry, rz, sx, sy, sz}` |
| `/create-particle-effect` | `{effectName, color, duration, emissionRate, startLifetime, startSpeed, startSize, radius, loop}` |
| `/create-light` | `{name, color, intensity, range, x, y, z}` |
| `/save-prefab` | `{objectName, prefabPath}` |

### POST  阶段二

| 路径 | 请求模型 |
|------|----------|
| `/create-material` | `{materialName, color, shaderName, emissionColor, emissionIntensity}` |
| `/assign-material` | `{objectName, materialPath}` |
| `/create-additive-particle-material` | `{materialName, color, emissionIntensity}` |
| `/set-material-color` | `{materialPath, color}` |
| `/set-material-emission` | `{materialPath, emissionColor, emissionIntensity}` |

### POST  阶段三

| 路径 | 请求模型 |
|------|----------|
| `/create-magic-portal` | `{effectName, mainColor, radius, duration, loop, saveAsPrefab}` |
| `/create-fire-explosion` | `{effectName, radius, intensity, duration, saveAsPrefab}` |
| `/create-lightning-hit` | `{effectName, mainColor, height, radius, duration, branchCount, saveAsPrefab}` |
| `/create-heal-aura` | `{effectName, mainColor, radius, duration, loop, density, saveAsPrefab}` |
| `/create-smoke-burst` | `{effectName, color, radius, duration, density, saveAsPrefab}` |
| `/create-slash-trail` | `{effectName, mainColor, length, width, duration, saveAsPrefab}` |

### POST  阶段四

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

### POST  阶段五

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

## 15. 关键组件说明

### Python 层

- **`server.py`**  入口，创建 `FastMCP("unity-mcp-server")`，注册所有工具模块，运行 MCP 传输层
- **`dashboard.py`**  Web Dashboard 后端，提供进程管理、Unity API 代理和日志查看
- **`templates/dashboard.html`**  Dashboard 前端页面，深色主题 UI，包含服务器管理、Unity 创建工具和连接测试
- **`tools/unity_http.py`**  封装 `get_from_unity()` 和 `post_to_unity()`，统一处理 HTTP 请求、超时和错误响应
- **`tools/*_tools.py`**  每个文件包含一个 `register_xxx_tools(mcp)` 函数，使用 `@mcp.tool()` 装饰器注册工具；工具函数内部构造 JSON payload 并调用 Unity HTTP 接口
- **`config.py`**  `UNITY_BASE_URL = "http://localhost:8765"`，`HTTP_TIMEOUT = 30`

### Unity C# 层

- **`UnityMcpDashboard.cs`**  EditorWindow 仪表盘（F12 打开），实时显示服务器运行状态、端口、监听地址；提供启停控制按钮、异步 Ping 连接检测、活动日志面板
- **`UnityMcpHttpServer.cs`**  `HttpListener` 在后台线程监听 `:8765`，通过 `EditorApplication.delayCall` 将请求回调到 Unity 主线程执行，避免线程安全问题；暴露 `IsRunning` 属性供仪表盘读取状态
- **`UnityMcpRouter.cs`**  静态字典映射路径  处理方法，`RouteGet()` / `RoutePost()` 分发
- **`UnityMcpModels.cs`**  统一的 `RequestModel`（所有请求字段在一个类中）和 `ResponseModel`（成功/状态/数据）
- **`Tools/*.cs`**  每个文件对应一类功能，接收 `RequestModel` 返回 JSON 字符串
- **`Utils/UnityMcpVfxUtils.cs`**  VFX 构建工具函数库，包括粒子配置、LineRenderer 创建、闪电生成、材质创建等
- **`Utils/UnityMcpPathUtils.cs`**  路径安全校验和自动编号保存路径生成
- **`Utils/UnityMcpColorUtils.cs`**  HTML 颜色解析 (#RGB/#RRGGBB/#RRGGBBAA)
- **`Utils/UnityMcpResponseUtils.cs`**  `Success()` / `Error()` 快捷创建响应 JSON

---

## 16. 实现关键点

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

## 17. 安全设计

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

## 18. 常见使用流程

### 完整工作流：创建  预览  调参  变体  截图  报告

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

## 19. 测试脚本说明

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

## 20. 常见问题排查

### 连接相关

| 问题 | 可能原因 | 解决方法 |
|------|----------|----------|
| `ping` 超时 | Unity 未启动 HTTP Server | 在 Unity 菜单栏点击 Unity MCP > Start Server，或按 F12 在仪表盘中点击"启动" |
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

## 21. 如何扩展新工具

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

## 22. Agent 操作指南：创建特效的标准流程

当 AI Agent（Claude、VS Code Copilot 等）需要通过 Unity MCP Server 创建特效时，应按以下标准化流程操作：

### 流程概览

```
连接测试 -> 创建特效 -> 播放 -> 聚焦 -> (截图/调参/变体/报告) -> 清理
```

### 第一步：测试连接

在执行任何 Unity 操作前，必须先调用 `ping_unity` 确认连接状态。

```
工具: ping_unity
说明: 检查 Unity HTTP 服务是否可达
预期: {"success": true, "message": "pong", ...}
```

**连接失败时的处理**：
- 提示用户确认 Unity Editor 已启动
- 提示用户点击菜单 **Unity MCP > Start Server** 启动 HTTP 服务
- 确认端口 8765 未被占用

### 第二步：创建特效

根据用户需求选择对应的 MCP 工具创建特效。所有 VFX 创建工具均接受 `effectName` 参数。

#### 基础粒子效果

```
工具: create_particle_effect
参数: {"effect_name": "MyEffect", "color": "#FF4400", "duration": 2.0,
       "emission_rate": 80, "start_lifetime": 1.5, "start_speed": 2.0,
       "start_size": 0.2, "radius": 1.0, "loop": true}
HTTP 映射: POST /create-particle-effect
```

#### 高级 VFX 特效

| 特效 | MCP 工具 | 关键参数 |
|------|----------|----------|
| 火焰爆炸 | `create_fire_explosion` | `effect_name, radius, intensity, duration` |
| 魔法传送门 | `create_magic_portal` | `effect_name, main_color, radius, loop` |
| 闪电打击 | `create_lightning_hit` | `effect_name, main_color, height, branch_count` |
| 治疗光环 | `create_heal_aura` | `effect_name, main_color, radius, loop` |
| 烟雾爆发 | `create_smoke_burst` | `effect_name, color, radius, density` |
| 斩击拖尾 | `create_slash_trail` | `effect_name, main_color, length, width` |

#### 基础物体和灯光

```
create_empty(name="MyObject")       -> POST /create-empty
create_light(name="MyLight", ...)   -> POST /create-light
create_material(name="MyMat", ...)  -> POST /create-material
```

### 第三步：播放和聚焦

创建特效后，应自动执行播放和聚焦操作。

```
1. play_effect(object_name="MyEffect", include_children=true)
   -> 播放特效及其所有子物体的粒子系统
   -> HTTP POST /play-effect

2. focus_scene_object(object_name="MyEffect")
   -> 在 SceneView 中聚焦并选中该物体
   -> HTTP POST /focus-scene-object
```

### 第四步：特效调优（可选）

根据用户需求使用阶段五工具调整特效：

```
recolor_effect(object_name, color, ...)    -> 整体重着色
scale_effect(object_name, scale, ...)      -> 整体缩放
update_particle_system(object_name, ...)   -> 调整粒子参数
adjust_effect_timing(object_name, ...)     -> 调整时长/速度
```

### 第五步：截图（可选）

```
capture_view(file_name, view_type="scene", width=1920, height=1080)
-> HTTP POST /capture-view
```

### 第六步：清理场景（可选）

```
clear_ai_generated_scene_objects(prefix="MyEffect")
-> HTTP POST /clear-ai-generated-scene-objects
```

### 完整示例：创建火焰爆炸特效

以下是一个完整的 Agent 调用序列：

```
# 1. 测试连接
ping_unity()

# 2. 创建火焰爆炸特效
create_fire_explosion(
    effect_name="MyFireExplosion",
    radius=2.5,
    intensity=1.2,
    duration=1.5,
    save_as_prefab=False
)

# 3. 播放并聚焦
play_effect(object_name="MyFireExplosion", include_children=True)
focus_scene_object(object_name="MyFireExplosion")

# 4. (可选) 截图
capture_view(file_name="FireShot", view_type="scene", width=1920, height=1080)
```

### 使用 Dashboard 的操作方式

如果用户已启动 Dashboard（`python dashboard.py`，访问 `http://localhost:5100`），也可以通过浏览器界面一键操作：

1. 在物体名称输入框中输入特效名（如 `MyFireExplosion`）
2. 选择颜色
3. 点击对应特效按钮

4. Dashboard 自动依次调用：创建 -> 播放 -> 聚焦

---

## 23. 后续路线

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
