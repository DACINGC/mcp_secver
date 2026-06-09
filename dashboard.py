"""
Unity MCP Server Dashboard
提供可视化界面管理 MCP Server 的启动、停止、状态监控和日志查看，
并支持直接在界面中向 Unity 创建物体和特效。
"""
import os
import sys
import json
import time
import signal
import subprocess
import threading
from datetime import datetime, timedelta
from pathlib import Path

import flask
from flask import Flask, jsonify, request
from flask_cors import CORS

# --- 配置 ---
SERVER_SCRIPT = os.path.join(os.path.dirname(__file__), "server.py")
CONFIG_FILE = os.path.join(os.path.dirname(__file__), "config.py")

DASHBOARD_HOST = "0.0.0.0"
DASHBOARD_PORT = 5100

TEMPLATE_DIR = os.path.join(os.path.dirname(__file__), "templates")

# --- 应用初始化 ---
app = Flask(
    __name__,
    static_folder=TEMPLATE_DIR,
    static_url_path="",
    template_folder=TEMPLATE_DIR,
)
CORS(app)


# --- MCP Server 进程管理 ---
class McpProcessManager:
    """管理 MCP Server 子进程的生命周期和日志捕获"""

    def __init__(self, script_path):
        self.script_path = script_path
        self.process: subprocess.Popen | None = None
        self.pid: int | None = None
        self.start_time: datetime | None = None
        self._logs: list[dict] = []
        self._lock = threading.Lock()
        self._stop_log_capture = threading.Event()
        self._log_thread: threading.Thread | None = None
        self._max_logs = 5000

    def _append_log(self, stream: str, msg: str):
        with self._lock:
            ts = datetime.now().strftime("%H:%M:%S.%f")[:12]
            self._logs.append({"ts": ts, "stream": stream, "msg": msg})
            if len(self._logs) > self._max_logs:
                self._logs = self._logs[-self._max_logs:]

    def _capture_output(self, pipe, stream_name):
        try:
            for line in iter(pipe.readline, ""):
                if self._stop_log_capture.is_set():
                    break
                line = line.rstrip("\n\r")
                if line:
                    self._append_log(stream_name, line)
        except (ValueError, OSError):
            pass
        finally:
            pipe.close()

    def start(self) -> dict:
        if self.is_running():
            return {"success": False, "message": "MCP Server is already running"}
        if not os.path.exists(self.script_path):
            return {"success": False, "message": f"Script not found: {self.script_path}"}
        try:
            self._stop_log_capture.clear()
            self._logs = []
            self.process = subprocess.Popen(
                [sys.executable, self.script_path],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                cwd=os.path.dirname(self.script_path),
                text=True,
                bufsize=1,
                creationflags=subprocess.CREATE_NO_WINDOW if sys.platform == "win32" else 0,
            )
            self.pid = self.process.pid
            self.start_time = datetime.now()
            t_out = threading.Thread(
                target=self._capture_output, args=(self.process.stdout, "STDOUT"), daemon=True
            )
            t_err = threading.Thread(
                target=self._capture_output, args=(self.process.stderr, "STDERR"), daemon=True
            )
            t_out.start()
            t_err.start()
            self._log_thread = t_out
            self._append_log("SYSTEM", f"MCP Server started (PID: {self.pid})")
            return {"success": True, "message": f"MCP Server started (PID: {self.pid})", "pid": self.pid}
        except Exception as e:
            self._cleanup()
            return {"success": False, "message": f"Start failed: {str(e)}"}

    def stop(self) -> dict:
        if not self.is_running():
            return {"success": False, "message": "MCP Server is not running"}
        pid = self.pid
        try:
            if sys.platform == "win32":
                self.process.terminate()
            else:
                os.kill(self.process.pid, signal.SIGTERM)
            try:
                self.process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                if sys.platform == "win32":
                    self.process.kill()
                else:
                    os.kill(self.process.pid, signal.SIGKILL)
                self.process.wait(timeout=3)
            self._append_log("SYSTEM", f"MCP Server stopped (PID: {pid})")
            return {"success": True, "message": f"MCP Server stopped (PID: {pid})"}
        except Exception as e:
            return {"success": False, "message": f"Stop failed: {str(e)}"}
        finally:
            self._cleanup()

    def restart(self) -> dict:
        self.stop()
        time.sleep(0.5)
        return self.start()

    def is_running(self) -> bool:
        if self.process is None:
            return False
        ret = self.process.poll()
        return ret is None

    def get_status(self) -> dict:
        running = self.is_running()
        uptime = ""
        if running and self.start_time:
            delta = datetime.now() - self.start_time
            hours, remainder = divmod(int(delta.total_seconds()), 3600)
            mins, secs = divmod(remainder, 60)
            if hours > 0:
                uptime = f"{hours}h {mins}m"
            elif mins > 0:
                uptime = f"{mins}m {secs}s"
            else:
                uptime = f"{secs}s"
        return {
            "status": "running" if running else "stopped",
            "pid": self.pid if running else None,
            "uptime": uptime,
            "log_count": len(self._logs),
            "start_time": self.start_time.isoformat() if self.start_time else None,
        }

    def get_logs(self, after: int = 0) -> list:
        with self._lock:
            if after >= len(self._logs):
                return []
            return self._logs[after:]

    def _cleanup(self):
        if self.process and self.process.stdout:
            self.process.stdout.close()
        if self.process and self.process.stderr:
            self.process.stderr.close()
        self._stop_log_capture.set()
        self.process = None
        self.pid = None
        self.start_time = None

    def __del__(self):
        self._stop_log_capture.set()
        if self.is_running():
            self.stop()


manager = McpProcessManager(SERVER_SCRIPT)


# --- Unity HTTP 代理函数 ---
def _unity_post(endpoint: str, payload: dict) -> dict:
    """向 Unity HTTP 服务发送 POST 请求"""
    import requests
    from config import UNITY_BASE_URL, HTTP_TIMEOUT
    url = f"{UNITY_BASE_URL}{endpoint}"
    resp = requests.post(url, json=payload, timeout=HTTP_TIMEOUT)
    resp.raise_for_status()
    return resp.json()


def _unity_get(endpoint: str) -> dict:
    """向 Unity HTTP 服务发送 GET 请求"""
    import requests
    from config import UNITY_BASE_URL, HTTP_TIMEOUT
    url = f"{UNITY_BASE_URL}{endpoint}"
    resp = requests.get(url, timeout=HTTP_TIMEOUT)
    resp.raise_for_status()
    return resp.json()


# --- API 路由 ---

@app.route("/")
def index():
    return flask.send_from_directory(TEMPLATE_DIR, "dashboard.html")


@app.route("/api/status")
def api_status():
    return jsonify(manager.get_status())


@app.route("/api/logs")
def api_logs():
    after = request.args.get("after", 0, type=int)
    logs = manager.get_logs(after)
    total = len(manager._logs) if hasattr(manager, "_logs") else 0
    return jsonify({"logs": logs, "total": total, "after": after})


@app.route("/api/start", methods=["POST"])
def api_start():
    return jsonify(manager.start())


@app.route("/api/stop", methods=["POST"])
def api_stop():
    return jsonify(manager.stop())


@app.route("/api/restart", methods=["POST"])
def api_restart():
    return jsonify(manager.restart())


@app.route("/api/ping", methods=["POST"])
def api_ping():
    """测试 Unity 连接"""
    try:
        import requests
        from config import UNITY_BASE_URL, HTTP_TIMEOUT
        resp = requests.get(f"{UNITY_BASE_URL}/ping", timeout=HTTP_TIMEOUT)
        resp.raise_for_status()
        data = resp.json()
        return jsonify({"success": True, "message": "Unity connected", "data": data})
    except ImportError:
        return jsonify({"success": False, "error": "Cannot import requests or config"})
    except requests.ConnectionError:
        return jsonify({"success": False, "error": "Cannot connect to Unity. Make sure Unity Editor is running and MCP Server is started (Unity MCP > Start Server)."})
    except requests.Timeout:
        return jsonify({"success": False, "error": "Connection timed out"})
    except Exception as e:
        return jsonify({"success": False, "error": str(e)})


@app.route("/api/config")
def api_config():
    cfg = {}
    try:
        with open(CONFIG_FILE, encoding="utf-8") as f:
            exec(compile(f.read(), CONFIG_FILE, "exec"), cfg)
    except Exception:
        pass
    py_ver = f"{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}"
    return jsonify({
        "unity_base_url": cfg.get("UNITY_BASE_URL", "http://localhost:8765"),
        "http_timeout": cfg.get("HTTP_TIMEOUT", 30),
        "server_script": SERVER_SCRIPT,
        "dashboard_port": DASHBOARD_PORT,
        "python_version": py_ver,
        "working_directory": os.path.dirname(__file__),
    })


@app.route("/api/unity-call", methods=["POST"])
def api_unity_call():
    """通用代理：向 Unity HTTP 服务发送 POST 请求"""
    data = request.get_json(silent=True) or {}
    endpoint = data.get("endpoint", "")
    payload = data.get("payload", {})

    if not endpoint.startswith("/"):
        endpoint = "/" + endpoint

    try:
        import requests
        from config import UNITY_BASE_URL, HTTP_TIMEOUT
        url = f"{UNITY_BASE_URL}{endpoint}"
        resp = requests.post(url, json=payload, timeout=HTTP_TIMEOUT)
        resp.raise_for_status()
        result = resp.json()
        return jsonify({
            "success": result.get("success", True),
            "message": result.get("message", "Success"),
            "data": result,
        })
    except requests.ConnectionError:
        return jsonify({
            "success": False,
            "error": "Cannot connect to Unity HTTP service. Make sure Unity is running and MCP Server is started.",
        })
    except requests.Timeout:
        return jsonify({
            "success": False,
            "error": "Request timed out. Unity may be compiling or busy.",
        })
    except Exception as e:
        return jsonify({
            "success": False,
            "error": str(e),
        })


@app.route("/api/unity-get", methods=["POST"])
def api_unity_get():
    """通用代理：向 Unity HTTP 服务发送 GET 请求"""
    data = request.get_json(silent=True) or {}
    endpoint = data.get("endpoint", "")

    if not endpoint.startswith("/"):
        endpoint = "/" + endpoint

    try:
        import requests
        from config import UNITY_BASE_URL, HTTP_TIMEOUT
        url = f"{UNITY_BASE_URL}{endpoint}"
        resp = requests.get(url, timeout=HTTP_TIMEOUT)
        resp.raise_for_status()
        result = resp.json()
        return jsonify({
            "success": result.get("success", True),
            "message": result.get("message", "Success"),
            "data": result,
        })
    except Exception as e:
        return jsonify({"success": False, "error": str(e)})


# =====================================================================
#  命令解析系统 —— 在可视化界面中输入相关命令，直接创建物体/特效
# =====================================================================

AVAILABLE_COMMANDS = {
    "基础物体": {
        "commands": [
            {
                "syntax": 'create empty named <name> [at x y z]',
                "example": 'create empty named MyEmptyObject at 0 0 0',
                "description": "在场景中创建空物体",
                "endpoint": "/create-empty",
                "method": "POST",
            },
            {
                "syntax": 'create camera named <name> [at x y z] [rx ry rz]',
                "example": 'create camera named MainCamera at 0 1 -10',
                "description": "创建摄像机",
                "endpoint": "/create-camera",
                "method": "POST",
            },
            {
                "syntax": 'create light named <name> [color #RRGGBB] [intensity n] [range n] [at x y z]',
                "example": 'create light named MyLight color #FF4400 intensity 3 range 5 at 0 2 0',
                "description": "创建点光源",
                "endpoint": "/create-light",
                "method": "POST",
            },
            {
                "syntax": 'create cube named <name> [color #RRGGBB] [at x y z] [size n]',
                "example": 'create cube named MyCube color #33AAFF at 0 1 0 size 1',
                "description": "创建立方体",
                "endpoint": "/create-primitive",
                "method": "POST",
            },
            {
                "syntax": 'create sphere named <name> [color #RRGGBB] [at x y z] [radius n]',
                "example": 'create sphere named MySphere color #44BB44 at 2 1 0 radius 0.5',
                "description": "创建球体",
                "endpoint": "/create-primitive",
                "method": "POST",
            },
            {
                "syntax": 'create cylinder named <name> [color #RRGGBB] [at x y z]',
                "example": 'create cylinder named MyCylinder color #FFAA00 at -1 0.5 0',
                "description": "创建圆柱体",
                "endpoint": "/create-primitive",
                "method": "POST",
            },
            {
                "syntax": 'create plane named <name> [color #RRGGBB] [at x y z]',
                "example": 'create plane named Ground color #888888 at 0 0 0',
                "description": "创建平面",
                "endpoint": "/create-primitive",
                "method": "POST",
            },
        ]
    },
    "粒子特效 (VFX)": {
        "commands": [
            {
                "syntax": 'create particle named <name> [color #RRGGBB] [duration n] [rate n] [speed n] [size n] [radius n] [loop true/false]',
                "example": 'create particle named MyParticles color #FF3366 duration 3 rate 100 speed 5 size 0.3 radius 2 loop true',
                "description": "创建自定义粒子系统",
                "endpoint": "/create-particle-effect",
                "method": "POST",
            },
            {
                "syntax": 'create fire named <name> [radius n] [intensity n] [duration n]',
                "example": 'create fire named FireBlast radius 2.5 intensity 1.2 duration 1.5',
                "description": "创建火焰爆炸特效",
                "endpoint": "/create-fire-explosion",
                "method": "POST",
            },
            {
                "syntax": 'create portal named <name> [color #RRGGBB] [radius n] [duration n] [loop true/false]',
                "example": 'create portal named MagicPortal color #33AAFF radius 2 duration 5 loop true',
                "description": "创建魔法传送门特效",
                "endpoint": "/create-magic-portal",
                "method": "POST",
            },
            {
                "syntax": 'create lightning named <name> [color #RRGGBB] [height n] [radius n] [duration n] [branches n]',
                "example": 'create lightning named LightningStrike color #AA33FF height 4 radius 1 duration 0.8 branches 5',
                "description": "创建闪电打击特效",
                "endpoint": "/create-lightning-hit",
                "method": "POST",
            },
            {
                "syntax": 'create heal named <name> [color #RRGGBB] [radius n] [duration n] [loop true/false]',
                "example": 'create heal named HealAura color #55FF88 radius 2 duration 4 loop true',
                "description": "创建治疗光环特效",
                "endpoint": "/create-heal-aura",
                "method": "POST",
            },
            {
                "syntax": 'create smoke named <name> [color #RRGGBB] [radius n] [duration n] [density n]',
                "example": 'create smoke named SmokeBurst color #888888 radius 2 duration 2.5 density 1',
                "description": "创建烟雾爆发特效",
                "endpoint": "/create-smoke-burst",
                "method": "POST",
            },
            {
                "syntax": 'create slash named <name> [color #RRGGBB] [length n] [width n] [duration n]',
                "example": 'create slash named SlashTrail color #66CCFF length 3 width 0.3 duration 0.5',
                "description": "创建斩击拖尾特效",
                "endpoint": "/create-slash-trail",
                "method": "POST",
            },
        ]
    },
    "材质 (Material)": {
        "commands": [
            {
                "syntax": 'create material named <name> [color #RRGGBB] [shader <name>] [emission #RRGGBB] [emissionIntensity n]',
                "example": 'create material named MyMat color #FF5733 shader "Universal Render Pipeline/Lit" emission #FF4400 emissionIntensity 2',
                "description": "创建材质资源",
                "endpoint": "/create-material",
                "method": "POST",
            },
            {
                "syntax": 'assign material <path> to <object_name>',
                "example": 'assign material Assets/AI_Generated/Materials/MyMat.mat to MyObject',
                "description": "为物体指定材质",
                "endpoint": "/assign-material",
                "method": "POST",
            },
        ]
    },
    "场景 & 地形 (Scene & Terrain)": {
        "commands": [
            {
                "syntax": 'create test suite named <name>',
                "example": 'create test suite named AI_TestSuite',
                "description": "创建测试容器根对象，所有 AI 生成将放入此容器以便安全重置",
                "endpoint": "/create-test-suite",
                "method": "POST",
            },
            {
                "syntax": 'create scene named <name> [color #RRGGBB] [size n] [walls true/false] [lights true/false] [parent <name>]',
                "example": 'create scene named DemoScene color #4CAF50 size 30 walls true lights true parent AI_TestSuite',
                "description": "创建完整示例场景（地面 + 光照 + 围墙）",
                "endpoint": "/create-sample-scene",
                "method": "POST",
            },
            {
                "syntax": 'create terrain named <name> [width n] [length n] [height n] [resolution n]',
                "example": 'create terrain named MyTerrain width 500 length 500 height 50 resolution 513',
                "description": "创建地形对象",
                "endpoint": "/create-terrain",
                "method": "POST",
            },
            {
                "syntax": 'sculpt <name> shape <shape> [strength n]',
                "example": 'sculpt MyTerrain shape mountain strength 0.8',
                "description": "雕刻地形高度图 (flat/smooth/mountain/valley/random)",
                "endpoint": "/sculpt-terrain",
                "method": "POST",
            },
            {
                "syntax": 'paint <name> layer <type>',
                "example": 'paint MyTerrain layer grass',
                "description": "为地形贴图 (grass/sand/rock/snow)",
                "endpoint": "/paint-terrain",
                "method": "POST",
            },
            {
                "syntax": 'environment [fog true/false] [fogColor #RRGGBB] [fogMode mode] [fogDensity n] [ambient #RRGGBB] [ambientIntensity n]',
                "example": 'environment fog true fogColor #666688 fogMode exponential fogDensity 0.02 ambient #FFEECC ambientIntensity 1.2',
                "description": "设置环境雾效和光照",
                "endpoint": "/set-environment",
                "method": "POST",
            },
            {
                "syntax": 'layout <name> pattern <pattern> [count n] [spacing n] [radius n]',
                "example": 'layout MyCube pattern grid count 16 spacing 2',
                "description": "批量布局物体 (grid/circle/random/line)",
                "endpoint": "/layout-objects",
                "method": "POST",
            },
            {
                "syntax": 'reset scene [keepLights true/false] [keepTerrain true/false]',
                "example": 'reset scene keepLights true keepTerrain true',
                "description": "清空场景（可保留灯光和地形）",
                "endpoint": "/reset-scene",
                "method": "POST",
            },
        ]
    },
    "物体操作": {
        "commands": [
            {
                "syntax": 'move <name> to x y z [rx ry rz] [sx sy sz]',
                "example": 'move MyObject to 3 1 2 rx 0 ry 45 rz 0 sx 2 sy 2 sz 2',
                "description": "设置物体位置/旋转/缩放",
                "endpoint": "/set-transform",
                "method": "POST",
            },
            {
                "syntax": 'focus <name>',
                "example": 'focus MyEffect',
                "description": "在 Scene 视图中聚焦物体",
                "endpoint": "/focus-scene-object",
                "method": "POST",
            },
            {
                "syntax": 'play <name>',
                "example": 'play MyEffect',
                "description": "播放物体上的粒子特效",
                "endpoint": "/play-effect",
                "method": "POST",
            },
            {
                "syntax": 'stop <name>',
                "example": 'stop MyEffect',
                "description": "停止物体上的粒子特效",
                "endpoint": "/stop-effect",
                "method": "POST",
            },
            {
                "syntax": 'info <name>',
                "example": 'info MyEffect',
                "description": "获取物体详细信息",
                "endpoint": "/get-object-info",
                "method": "POST",
            },
            {
                "syntax": 'list objects',
                "example": 'list objects',
                "description": "列出场景中所有物体",
                "endpoint": "/list-scene-objects",
                "method": "GET",
            },
            {
                "syntax": 'clear [prefix]',
                "example": 'clear MyE',
                "description": "清理场景中指定前缀的物体（默认前缀 MyE, 最少3字符）",
                "endpoint": "/clear-ai-generated-scene-objects",
                "method": "POST",
            },
        ]
    },
    "特效调优 (Tuning)": {
        "commands": [
            {
                "syntax": 'recolor <name> to <color>',
                "example": 'recolor MyEffect to #FF3366',
                "description": "重新着色整个特效（粒子、灯光、拖尾等）",
                "endpoint": "/recolor-effect",
                "method": "POST",
            },
            {
                "syntax": 'scale <name> by <multiplier>',
                "example": 'scale MyEffect by 2',
                "description": "缩放整个特效（变换 + 粒子大小）",
                "endpoint": "/scale-effect",
                "method": "POST",
            },
            {
                "syntax": 'timing <name> duration <mult> speed <mult>',
                "example": 'timing MyEffect duration 1.5 speed 2',
                "description": "调整特效持续时间和播放速度",
                "endpoint": "/adjust-effect-timing",
                "method": "POST",
            },
            {
                "syntax": 'update particle <name> [color #RRGGBB] [rate n] [lifetime n] [speed n] [size n] [duration n] [loop true/false]',
                "example": 'update particle MyEffect color #FF0000 rate 200 size 0.5 loop true',
                "description": "修改粒子系统参数",
                "endpoint": "/update-particle-system",
                "method": "POST",
            },
            {
                "syntax": 'update light <name> [color #RRGGBB] [intensity n] [range n]',
                "example": 'update light MyLight color #FFAA00 intensity 5 range 10',
                "description": "修改灯光参数",
                "endpoint": "/update-light",
                "method": "POST",
            },
        ]
    },
    "变体 & 其他": {
        "commands": [
            {
                "syntax": 'variants <source> [prefix <name>] [colors c1,c2,c3] [count n] [spacing n]',
                "example": 'variants MyEffect prefix ColorVar colors #FF0000,#00FF00,#0000FF count 3 spacing 3',
                "description": "创建多个颜色变体",
                "endpoint": "/create-effect-variants",
                "method": "POST",
            },
            {
                "syntax": 'save prefab <object_name> [path]',
                "example": 'save prefab MyEffect Assets/AI_Generated/Prefabs/MyEffect.prefab',
                "description": "将物体保存为 Prefab",
                "endpoint": "/save-prefab",
                "method": "POST",
            },
            {
                "syntax": 'capture [name <filename>] [type scene/game] [w n] [h n]',
                "example": 'capture name MyShot type scene w 1920 h 1080',
                "description": "截取 Scene 或 Game 视图",
                "endpoint": "/capture-view",
                "method": "POST",
            },
            {
                "syntax": 'report <object_name> [filename]',
                "example": 'report MyEffect MyReport',
                "description": "导出物体详细报告为 JSON",
                "endpoint": "/export-effect-report",
                "method": "POST",
            },
        ]
    },
}


def parse_command(text: str) -> dict:
    """解析文本命令并返回对应的 Unity 端点调用参数"""
    raw = text.strip()
    if not raw:
        return {"success": False, "error": "命令不能为空"}

    parts = raw.split()
    cmd_lower = raw.lower()

    # ========== 基础物体 ==========
    if cmd_lower.startswith("create empty"):
        return _parse_create_empty(parts, raw)
    if cmd_lower.startswith("create camera"):
        return _parse_create_camera(parts, raw)
    if cmd_lower.startswith("create light"):
        return _parse_create_light(parts, raw)
    if cmd_lower.startswith("create cube"):
        return _parse_create_primitive(parts, raw, "Cube")
    if cmd_lower.startswith("create sphere"):
        return _parse_create_primitive(parts, raw, "Sphere")
    if cmd_lower.startswith("create cylinder"):
        return _parse_create_primitive(parts, raw, "Cylinder")
    if cmd_lower.startswith("create plane"):
        return _parse_create_primitive(parts, raw, "Plane")

    # ========== 粒子特效 ==========
    if cmd_lower.startswith("create particle"):
        return _parse_create_particle(parts, raw)
    if cmd_lower.startswith("create fire"):
        return _parse_create_fire(parts, raw)
    if cmd_lower.startswith("create portal"):
        return _parse_create_portal(parts, raw)
    if cmd_lower.startswith("create lightning"):
        return _parse_create_lightning(parts, raw)
    if cmd_lower.startswith("create heal"):
        return _parse_create_heal(parts, raw)
    if cmd_lower.startswith("create smoke"):
        return _parse_create_smoke(parts, raw)
    if cmd_lower.startswith("create slash"):
        return _parse_create_slash(parts, raw)

    # ========== 场景 & 地形 (EXTEND_SCENE) ==========
    if cmd_lower.startswith("create test suite"):
        return _parse_create_test_suite(parts, raw)
    if cmd_lower.startswith("create scene"):
        return _parse_create_sample_scene(parts, raw)
    if cmd_lower.startswith("create terrain"):
        return _parse_create_terrain(parts, raw)
    if cmd_lower.startswith("sculpt "):
        return _parse_sculpt_terrain(parts, raw)
    if cmd_lower.startswith("paint "):
        return _parse_paint_terrain(parts, raw)
    if cmd_lower.startswith("environment"):
        return _parse_set_environment(parts, raw)
    if cmd_lower.startswith("layout "):
        return _parse_layout_objects(parts, raw)
    if cmd_lower.startswith("reset scene"):
        return _parse_reset_scene(parts, raw)

    # ========== 材质 ==========
    if cmd_lower.startswith("create material"):
        return _parse_create_material(parts, raw)
    if cmd_lower.startswith("assign material"):
        return _parse_assign_material(parts, raw)

    # ========== 物体操作 ==========
    if cmd_lower.startswith("move "):
        return _parse_set_transform(parts, raw)

    if cmd_lower.startswith("focus "):
        obj_name = raw[6:].strip()
        if not obj_name:
            return {"success": False, "error": "请指定物体名称，例如: focus MyEffect"}
        return {"success": True, "endpoint": "/focus-scene-object", "method": "POST", "payload": {"objectName": obj_name}}

    if cmd_lower.startswith("play "):
        obj_name = raw[5:].strip()
        if not obj_name:
            return {"success": False, "error": "请指定物体名称，例如: play MyEffect"}
        return {"success": True, "endpoint": "/play-effect", "method": "POST", "payload": {"objectName": obj_name, "includeChildren": True}}

    if cmd_lower.startswith("stop "):
        obj_name = raw[5:].strip()
        if not obj_name:
            return {"success": False, "error": "请指定物体名称，例如: stop MyEffect"}
        return {"success": True, "endpoint": "/stop-effect", "method": "POST", "payload": {"objectName": obj_name, "includeChildren": True, "clearParticles": True}}

    if cmd_lower.startswith("info "):
        obj_name = raw[5:].strip()
        if not obj_name:
            return {"success": False, "error": "请指定物体名称，例如: info MyEffect"}
        return {"success": True, "endpoint": "/get-object-info", "method": "POST", "payload": {"objectName": obj_name, "includeChildren": True}}

    if cmd_lower in ("list objects", "list object"):
        return {"success": True, "endpoint": "/list-scene-objects", "method": "GET", "payload": {}}

    if cmd_lower.startswith("clear "):
        prefix = parts[1] if len(parts) > 1 else "MyE"
        if len(prefix) < 3:
            return {"success": False, "error": "前缀至少需要3个字符"}
        return {"success": True, "endpoint": "/clear-ai-generated-scene-objects", "method": "POST", "payload": {"prefix": prefix}}
    if cmd_lower == "clear":
        return {"success": True, "endpoint": "/clear-ai-generated-scene-objects", "method": "POST", "payload": {"prefix": "MyE"}}

    # ========== 调优 ==========
    if cmd_lower.startswith("recolor "):
        return _parse_recolor(parts, raw)
    if cmd_lower.startswith("scale "):
        return _parse_scale(parts, raw)
    if cmd_lower.startswith("timing "):
        return _parse_timing(parts, raw)
    if cmd_lower.startswith("update particle"):
        return _parse_update_particle(parts, raw)
    if cmd_lower.startswith("update light"):
        return _parse_update_light(parts, raw)

    # ========== 变体 & 其他 ==========
    if cmd_lower.startswith("variants") or cmd_lower.startswith("variant"):
        return _parse_variants(parts, raw)
    if cmd_lower.startswith("save prefab"):
        return _parse_save_prefab(parts, raw)
    if cmd_lower.startswith("capture"):
        return _parse_capture(parts, raw)
    if cmd_lower.startswith("report "):
        return _parse_report(parts, raw)

    return {"success": False, "error": f"无法识别的命令: {text}"}


# ----- 解析辅助函数 -----

def _get_kwarg(parts, key, default=None, convert=str):
    for i, p in enumerate(parts):
        if p.lower() == key and i + 1 < len(parts):
            try:
                return convert(parts[i + 1])
            except (ValueError, TypeError):
                return default
    return default


def _get_named(parts, raw, default_name="NewObject"):
    idx = _find_key(parts, "named")
    if idx is not None and idx + 1 < len(parts):
        name = parts[idx + 1]
        if name.startswith('"') or name.startswith("'"):
            end_char = name[0]
            full_name = name
            j = idx + 2
            while j < len(parts) and not full_name.endswith(end_char):
                full_name += " " + parts[j]
                j += 1
            return full_name.strip(end_char)
        return name
    return default_name


def _find_key(parts, key):
    for i, p in enumerate(parts):
        if p.lower() == key.lower():
            return i
    return None


def _parse_position(parts, raw):
    at_idx = _find_key(parts, "at")
    if at_idx is not None and at_idx + 3 < len(parts):
        try:
            x = float(parts[at_idx + 1])
            y = float(parts[at_idx + 2])
            z = float(parts[at_idx + 3])
            return {"x": x, "y": y, "z": z}
        except (ValueError, IndexError):
            pass
    return {"x": 0.0, "y": 0.0, "z": 0.0}


# ----- 创建命令解析 -----

def _parse_create_empty(parts, raw):
    name = _get_named(parts, raw, "EmptyObject")
    pos = _parse_position(parts, raw)
    return {"success": True, "endpoint": "/create-empty", "method": "POST", "payload": {"name": name, **pos}}


def _parse_create_camera(parts, raw):
    name = _get_named(parts, raw, "MainCamera")
    pos = _parse_position(parts, raw)
    parent = _get_kwarg(parts, "parent", "")
    return {"success": True, "endpoint": "/create-camera", "method": "POST",
            "payload": {"name": name, "parent": parent, **pos}}


def _parse_create_light(parts, raw):
    name = _get_named(parts, raw, "PointLight")
    pos = _parse_position(parts, raw)
    color = _get_kwarg(parts, "color", "#FFFFFF")
    intensity = _get_kwarg(parts, "intensity", 3.0, float)
    range_val = _get_kwarg(parts, "range", 5.0, float)
    return {"success": True, "endpoint": "/create-light", "method": "POST",
            "payload": {"name": name, "color": color, "intensity": intensity, "range": range_val, **pos}}


def _parse_create_primitive(parts, raw, primitive_type):
    name = _get_named(parts, raw, primitive_type)
    pos = _parse_position(parts, raw)
    color = _get_kwarg(parts, "color", "")
    size = _get_kwarg(parts, "size", 1.0, float)
    radius = _get_kwarg(parts, "radius", 0.5, float)
    parent = _get_kwarg(parts, "parent", "")
    return {"success": True, "endpoint": "/create-primitive", "method": "POST",
            "payload": {"primitiveType": primitive_type, "name": name, "color": color, "size": size, "radius": radius, "parent": parent, **pos}}


def _parse_create_particle(parts, raw):
    name = _get_named(parts, raw, "ParticleEffect")
    color = _get_kwarg(parts, "color", "#33AAFF")
    duration = _get_kwarg(parts, "duration", 2.0, float)
    rate = _get_kwarg(parts, "rate", 80.0, float)
    speed = _get_kwarg(parts, "speed", 2.0, float)
    size = _get_kwarg(parts, "size", 0.2, float)
    radius = _get_kwarg(parts, "radius", 1.0, float)
    loop_str = _get_kwarg(parts, "loop", "true").lower()
    loop = loop_str not in ("false", "0", "no")
    return {"success": True, "endpoint": "/create-particle-effect", "method": "POST",
            "payload": {"effectName": name, "color": color, "duration": duration, "emissionRate": rate,
                         "startLifetime": duration * 0.75, "startSpeed": speed, "startSize": size, "radius": radius, "loop": loop}}


def _parse_create_fire(parts, raw):
    name = _get_named(parts, raw, "FireExplosion")
    radius = _get_kwarg(parts, "radius", 2.5, float)
    intensity = _get_kwarg(parts, "intensity", 1.2, float)
    duration = _get_kwarg(parts, "duration", 1.5, float)
    return {"success": True, "endpoint": "/create-fire-explosion", "method": "POST",
            "payload": {"effectName": name, "radius": radius, "intensity": intensity, "duration": duration, "saveAsPrefab": False}}


def _parse_create_portal(parts, raw):
    name = _get_named(parts, raw, "MagicPortal")
    color = _get_kwarg(parts, "color", "#33AAFF")
    radius = _get_kwarg(parts, "radius", 2.0, float)
    duration = _get_kwarg(parts, "duration", 5.0, float)
    loop_str = _get_kwarg(parts, "loop", "true").lower()
    loop = loop_str not in ("false", "0", "no")
    return {"success": True, "endpoint": "/create-magic-portal", "method": "POST",
            "payload": {"effectName": name, "mainColor": color, "radius": radius, "duration": duration, "loop": loop, "saveAsPrefab": False}}


def _parse_create_lightning(parts, raw):
    name = _get_named(parts, raw, "LightningHit")
    color = _get_kwarg(parts, "color", "#AA33FF")
    height = _get_kwarg(parts, "height", 4.0, float)
    radius = _get_kwarg(parts, "radius", 1.0, float)
    duration = _get_kwarg(parts, "duration", 0.8, float)
    branches = _get_kwarg(parts, "branches", 5, int)
    return {"success": True, "endpoint": "/create-lightning-hit", "method": "POST",
            "payload": {"effectName": name, "mainColor": color, "height": height, "radius": radius,
                         "duration": duration, "branchCount": branches, "saveAsPrefab": False}}


def _parse_create_heal(parts, raw):
    name = _get_named(parts, raw, "HealAura")
    color = _get_kwarg(parts, "color", "#55FF88")
    radius = _get_kwarg(parts, "radius", 2.0, float)
    duration = _get_kwarg(parts, "duration", 4.0, float)
    loop_str = _get_kwarg(parts, "loop", "true").lower()
    loop = loop_str not in ("false", "0", "no")
    return {"success": True, "endpoint": "/create-heal-aura", "method": "POST",
            "payload": {"effectName": name, "mainColor": color, "radius": radius, "duration": duration, "loop": loop, "saveAsPrefab": False}}


def _parse_create_smoke(parts, raw):
    name = _get_named(parts, raw, "SmokeBurst")
    color = _get_kwarg(parts, "color", "#888888")
    radius = _get_kwarg(parts, "radius", 2.0, float)
    duration = _get_kwarg(parts, "duration", 2.5, float)
    density = _get_kwarg(parts, "density", 1.0, float)
    return {"success": True, "endpoint": "/create-smoke-burst", "method": "POST",
            "payload": {"effectName": name, "color": color, "radius": radius, "duration": duration, "density": density, "saveAsPrefab": False}}


def _parse_create_slash(parts, raw):
    name = _get_named(parts, raw, "SlashTrail")
    color = _get_kwarg(parts, "color", "#66CCFF")
    length = _get_kwarg(parts, "length", 3.0, float)
    width = _get_kwarg(parts, "width", 0.3, float)
    duration = _get_kwarg(parts, "duration", 0.5, float)
    return {"success": True, "endpoint": "/create-slash-trail", "method": "POST",
            "payload": {"effectName": name, "mainColor": color, "length": length, "width": width, "duration": duration, "saveAsPrefab": False}}


def _parse_create_test_suite(parts, raw):
    name = _get_named(parts, raw, "AI_TestSuite")
    return {"success": True, "endpoint": "/create-test-suite", "method": "POST",
            "payload": {"name": name}}


def _parse_create_sample_scene(parts, raw):
    name = _get_named(parts, raw, "SampleScene")
    color = _get_kwarg(parts, "color", "#4CAF50")
    size = _get_kwarg(parts, "size", 20.0, float)
    walls = _get_kwarg(parts, "walls", "false").lower() in ("true", "1", "yes")
    lights = _get_kwarg(parts, "lights", "true").lower() in ("true", "1", "yes")
    parent = _get_kwarg(parts, "parent", "")
    return {"success": True, "endpoint": "/create-sample-scene", "method": "POST",
            "payload": {"name": name, "groundColor": color, "groundSize": size,
                         "includeWalls": walls, "includeLights": lights, "parent": parent}}


def _parse_create_terrain(parts, raw):
    name = _get_named(parts, raw, "Terrain")
    width = _get_kwarg(parts, "width", 500, int)
    length = _get_kwarg(parts, "length", 500, int)
    height = _get_kwarg(parts, "height", 50.0, float)
    resolution = _get_kwarg(parts, "resolution", 513, int)
    return {"success": True, "endpoint": "/create-terrain", "method": "POST",
            "payload": {"name": name, "width": width, "length": length,
                         "height": height, "resolution": resolution}}


def _parse_sculpt_terrain(parts, raw):
    idx = 1
    name = parts[idx] if len(parts) > idx else ""
    if not name:
        return {"success": False, "error": "格式: sculpt <name> shape <shape> [strength n]"}
    shape = _get_kwarg(parts, "shape", "smooth")
    strength = _get_kwarg(parts, "strength", 0.5, float)
    return {"success": True, "endpoint": "/sculpt-terrain", "method": "POST",
            "payload": {"objectName": name, "shape": shape, "strength": strength}}


def _parse_paint_terrain(parts, raw):
    idx = 1
    name = parts[idx] if len(parts) > idx else ""
    if not name:
        return {"success": False, "error": "格式: paint <name> layer <type>"}
    layer = _get_kwarg(parts, "layer", "grass")
    return {"success": True, "endpoint": "/paint-terrain", "method": "POST",
            "payload": {"objectName": name, "layerType": layer}}


def _parse_set_environment(parts, raw):
    fog = _get_kwarg(parts, "fog", "false").lower() in ("true", "1", "yes")
    fog_color = _get_kwarg(parts, "fogColor", "#808080")
    fog_mode = _get_kwarg(parts, "fogMode", "exponential")
    fog_density = _get_kwarg(parts, "fogDensity", 0.01, float)
    ambient = _get_kwarg(parts, "ambient", "#FFFFFF")
    ambient_intensity = _get_kwarg(parts, "ambientIntensity", 1.0, float)
    return {"success": True, "endpoint": "/set-environment", "method": "POST",
            "payload": {"fogEnabled": fog, "fogColor": fog_color, "fogMode": fog_mode,
                         "fogDensity": fog_density, "ambientColor": ambient,
                         "ambientIntensity": ambient_intensity}}


def _parse_layout_objects(parts, raw):
    idx = 1
    name = parts[idx] if len(parts) > idx else ""
    if not name:
        return {"success": False, "error": "格式: layout <name> pattern <pattern> [count n] [spacing n] [radius n]"}
    pattern = _get_kwarg(parts, "pattern", "grid")
    count = _get_kwarg(parts, "count", 10, int)
    spacing = _get_kwarg(parts, "spacing", 2.0, float)
    radius = _get_kwarg(parts, "radius", 5.0, float)
    return {"success": True, "endpoint": "/layout-objects", "method": "POST",
            "payload": {"objectName": name, "pattern": pattern,
                         "count": count, "spacing": spacing, "radius": radius}}


def _parse_reset_scene(parts, raw):
    keep_lights = _get_kwarg(parts, "keepLights", "false").lower() in ("true", "1", "yes")
    keep_terrain = _get_kwarg(parts, "keepTerrain", "false").lower() in ("true", "1", "yes")
    return {"success": True, "endpoint": "/reset-scene", "method": "POST",
            "payload": {"keepLights": keep_lights, "keepTerrain": keep_terrain}}


def _parse_create_material(parts, raw):
    name = _get_named(parts, raw, "NewMaterial")
    color = _get_kwarg(parts, "color", "#FFFFFF")
    shader = _get_kwarg(parts, "shader", "Universal Render Pipeline/Particles/Unlit")
    emission = _get_kwarg(parts, "emission", "#000000")
    emission_intensity = _get_kwarg(parts, "emissionIntensity", 0.0, float)
    return {"success": True, "endpoint": "/create-material", "method": "POST",
            "payload": {"materialName": name, "color": color, "shaderName": shader, "emissionColor": emission, "emissionIntensity": emission_intensity}}


def _parse_assign_material(parts, raw):
    try:
        mat_idx = _find_key(parts, "material")
        to_idx = _find_key(parts, "to")
        if mat_idx is None or to_idx is None or to_idx <= mat_idx:
            return {"success": False, "error": "格式: assign material <path> to <object_name>"}
        mat_path = " ".join(parts[mat_idx + 1:to_idx])
        obj_name = " ".join(parts[to_idx + 1:])
        if not mat_path or not obj_name:
            return {"success": False, "error": "材质路径和物体名称不能为空"}
        return {"success": True, "endpoint": "/assign-material", "method": "POST",
                "payload": {"objectName": obj_name, "materialPath": mat_path}}
    except Exception as e:
        return {"success": False, "error": f"解析失败: {e}"}


def _parse_set_transform(parts, raw):
    try:
        to_idx = _find_key(parts, "to")
        if to_idx is None:
            return {"success": False, "error": "格式: move <name> to x y z [rx ry rz] [sx sy sz]"}
        name = " ".join(parts[1:to_idx])
        payload = {"objectName": name, "x": 0, "y": 0, "z": 0, "rx": 0, "ry": 0, "rz": 0, "sx": 1, "sy": 1, "sz": 1}

        nums = []
        for s in parts[to_idx + 1:]:
            try:
                nums.append(float(s))
            except ValueError:
                continue
        if len(nums) >= 1: payload["x"] = nums[0]
        if len(nums) >= 2: payload["y"] = nums[1]
        if len(nums) >= 3: payload["z"] = nums[2]

        for key in ["rx", "ry", "rz", "sx", "sy", "sz"]:
            idx = _find_key(parts, key)
            if idx is not None:
                payload[key] = float(parts[idx + 1])

        return {"success": True, "endpoint": "/set-transform", "method": "POST", "payload": payload}
    except Exception as e:
        return {"success": False, "error": f"解析失败: {e}"}


def _parse_recolor(parts, raw):
    to_idx = _find_key(parts, "to")
    if to_idx is None:
        return {"success": False, "error": "格式: recolor <name> to <color>"}
    name = " ".join(parts[1:to_idx])
    color = parts[to_idx + 1] if to_idx + 1 < len(parts) else "#FFFFFF"
    return {"success": True, "endpoint": "/recolor-effect", "method": "POST",
            "payload": {"objectName": name, "mainColor": color, "includeChildren": True,
                         "affectParticles": True, "affectLights": True, "affectRenderers": True, "affectLines": True}}


def _parse_scale(parts, raw):
    by_idx = _find_key(parts, "by")
    if by_idx is None:
        return {"success": False, "error": "格式: scale <name> by <multiplier>"}
    name = " ".join(parts[1:by_idx])
    mult = float(parts[by_idx + 1]) if by_idx + 1 < len(parts) else 1.0
    return {"success": True, "endpoint": "/scale-effect", "method": "POST",
            "payload": {"objectName": name, "scaleMultiplier": mult, "scaleTransform": True,
                         "scaleParticleSize": True, "scaleParticleSpeed": False, "includeChildren": True}}


def _parse_timing(parts, raw):
    name = parts[1] if len(parts) > 1 else ""
    if not name:
        return {"success": False, "error": "格式: timing <name> duration <mult> speed <mult>"}
    dur = _get_kwarg(parts, "duration", 1.0, float)
    spd = _get_kwarg(parts, "speed", 1.0, float)
    return {"success": True, "endpoint": "/adjust-effect-timing", "method": "POST",
            "payload": {"objectName": name, "durationMultiplier": dur, "speedMultiplier": spd, "includeChildren": True}}


def _parse_update_particle(parts, raw):
    name = parts[2] if len(parts) > 2 else ""
    if not name:
        return {"success": False, "error": "格式: update particle <name> [color #RRGGBB] [rate n] [lifetime n] [speed n] [size n] ..."}
    color = _get_kwarg(parts, "color", "")
    rate = _get_kwarg(parts, "rate", -1, float)
    lifetime = _get_kwarg(parts, "lifetime", -1, float)
    speed = _get_kwarg(parts, "speed", -1, float)
    size = _get_kwarg(parts, "size", -1, float)
    duration = _get_kwarg(parts, "duration", -1, float)
    loop_str = _get_kwarg(parts, "loop", "keep")
    return {"success": True, "endpoint": "/update-particle-system", "method": "POST",
            "payload": {"objectName": name, "includeChildren": True, "color": color,
                         "emissionRate": rate, "startLifetime": lifetime, "startSpeed": speed,
                         "startSize": size, "duration": duration, "loop": loop_str}}


def _parse_update_light(parts, raw):
    name = parts[2] if len(parts) > 2 else ""
    if not name:
        return {"success": False, "error": "格式: update light <name> [color #RRGGBB] [intensity n] [range n]"}
    color = _get_kwarg(parts, "color", "")
    intensity = _get_kwarg(parts, "intensity", -1, float)
    range_val = _get_kwarg(parts, "range", -1, float)
    return {"success": True, "endpoint": "/update-light", "method": "POST",
            "payload": {"objectName": name, "includeChildren": True, "color": color, "intensity": intensity, "range": range_val}}


def _parse_variants(parts, raw):
    source_name = _get_kwarg(parts, "variants", "")
    if not source_name:
        for i, p in enumerate(parts):
            if p.lower() in ("variants", "variant") and i + 1 < len(parts):
                source_name = parts[i + 1]
                break
    if not source_name:
        return {"success": False, "error": "格式: variants <source> [prefix <name>] [colors c1,c2,c3] [count n] [spacing n]"}
    prefix = _get_kwarg(parts, "prefix", "AI_Variant")
    colors = _get_kwarg(parts, "colors", "#33AAFF,#AA33FF,#FFAA33")
    count = _get_kwarg(parts, "count", 3, int)
    spacing = _get_kwarg(parts, "spacing", 3.0, float)
    return {"success": True, "endpoint": "/create-effect-variants", "method": "POST",
            "payload": {"sourceObjectName": source_name, "variantPrefix": prefix, "colors": colors,
                         "count": count, "spacing": spacing, "saveAsPrefab": False}}


def _parse_save_prefab(parts, raw):
    try:
        prefab_idx = _find_key(parts, "prefab")
        if prefab_idx is None or prefab_idx + 1 >= len(parts):
            return {"success": False, "error": "格式: save prefab <object_name> [path]"}
        rest = parts[prefab_idx + 1:]
        obj_name = rest[0]
        path = " ".join(rest[1:]) if len(rest) > 1 else ""
        return {"success": True, "endpoint": "/save-prefab", "method": "POST",
                "payload": {"objectName": obj_name, "prefabPath": path}}
    except Exception as e:
        return {"success": False, "error": f"解析失败: {e}"}


def _parse_capture(parts, raw):
    fname = _get_kwarg(parts, "name", "unity_capture")
    view_type = _get_kwarg(parts, "type", "scene")
    width = _get_kwarg(parts, "w", 1280, int)
    height = _get_kwarg(parts, "h", 720, int)
    return {"success": True, "endpoint": "/capture-view", "method": "POST",
            "payload": {"fileName": fname, "viewType": view_type, "width": width, "height": height}}


def _parse_report(parts, raw):
    name = parts[1] if len(parts) > 1 else ""
    if not name:
        return {"success": False, "error": "格式: report <object_name> [filename]"}
    fname = " ".join(parts[2:]) if len(parts) > 2 else ""
    return {"success": True, "endpoint": "/export-effect-report", "method": "POST",
            "payload": {"objectName": name, "fileName": fname}}


# ----- API 路由：可用命令列表 -----
@app.route("/api/available-commands")
def api_available_commands():
    return jsonify(AVAILABLE_COMMANDS)


# ----- API 路由：执行命令 -----
@app.route("/api/unity-command", methods=["POST"])
def api_unity_command():
    """接收文本命令 -> 解析 -> 调用 Unity"""
    data = request.get_json(silent=True) or {}
    text = data.get("command", "").strip()

    if not text:
        return jsonify({"success": False, "error": "命令不能为空"})

    parsed = parse_command(text)
    if not parsed.get("success"):
        return jsonify(parsed)

    endpoint = parsed.get("endpoint", "")
    method = parsed.get("method", "POST")
    payload = parsed.get("payload", {})
    command_label = parsed.get("label", text)

    try:
        import requests as req
        from config import UNITY_BASE_URL, HTTP_TIMEOUT

        if method == "GET":
            url = f"{UNITY_BASE_URL}{endpoint}"
            resp = req.get(url, timeout=HTTP_TIMEOUT)
        else:
            url = f"{UNITY_BASE_URL}{endpoint}"
            resp = req.post(url, json=payload, timeout=HTTP_TIMEOUT)

        resp.raise_for_status()
        result = resp.json()

        return jsonify({
            "success": True,
            "command": command_label,
            "endpoint": endpoint,
            "message": f"命令执行成功: {command_label}",
            "data": result,
        })
    except req.ConnectionError:
        return jsonify({
            "success": False,
            "command": command_label,
            "error": "无法连接到 Unity HTTP 服务，请确保 Unity 正在运行且 MCP 服务器已启动",
        })
    except req.Timeout:
        return jsonify({
            "success": False,
            "command": command_label,
            "error": "请求超时，Unity 可能正在编译或繁忙",
        })
    except Exception as e:
        return jsonify({
            "success": False,
            "command": command_label,
            "error": str(e),
        })


# --- 主入口 ---
if __name__ == "__main__":
    print("=" * 55)
    print("  Unity MCP Server Dashboard")
    print(f"  Dashboard : http://localhost:{DASHBOARD_PORT}")
    print(f"  API       : http://localhost:{DASHBOARD_PORT}/api")
    print(f"  MCP Server: {SERVER_SCRIPT}")
    print("=" * 55)
    app.run(host=DASHBOARD_HOST, port=DASHBOARD_PORT, debug=False, threaded=True)
