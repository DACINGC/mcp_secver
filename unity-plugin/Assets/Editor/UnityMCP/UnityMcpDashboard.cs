using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UnityMCP
{
    /// <summary>
    /// Unity MCP 服务器状态仪表盘 EditorWindow
    /// 实时显示 HTTP 服务器运行状态，提供启停控制和连接检测
    /// </summary>
    public class UnityMcpDashboard : EditorWindow
    {
        #region 常量

        private const string WINDOW_TITLE = "Unity MCP";
        private const int DEFAULT_PORT = 8765;
        private const string MCP_LOG_PREFIX = "[UnityMCP]";

        #endregion

        #region GUI 布局常量

        private const float LABEL_WIDTH = 90f;
        private const float STATUS_INDICATOR_SIZE = 14f;
        private const float BUTTON_HEIGHT = 28f;
        private const int MAX_LOG_LINES = 100;

        #endregion

        #region 状态缓存

        private Vector2 _scrollPosition;
        private readonly Queue<string> _logMessages = new Queue<string>();

        // 连接测试相关
        private string _pingStatus = string.Empty;
        private bool _isPinging;
        private DateTime _pingStartTime;
        private long _lastTestLatencyMs = -1;

        // 刷新计时
        private double _lastRepaintTime;

        // 日志锁
        private readonly object _logLock = new object();

        #endregion

        #region 样式缓存

        private GUIStyle _statusIndicatorOnStyle;
        private GUIStyle _statusIndicatorOffStyle;
        private GUIStyle _headerLabelStyle;
        private GUIStyle _valueTextStyle;
        private GUIStyle _logEntryStyle;
        private GUIStyle _logErrorStyle;
        private GUIStyle _sectionHeaderStyle;
        private bool _stylesInitialized;

        #endregion

        #region 窗口入口

        [MenuItem("Unity MCP/Dashboard _F12", priority = 1)]
        public static void OpenDashboard()
        {
            var window = GetWindow<UnityMcpDashboard>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(380, 420);
            window.Show();
        }

        #endregion

        #region 生命周期

        private void OnEnable()
        {
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            _lastRepaintTime = EditorApplication.timeSinceStartup;
            EnqueueLog("仪表盘已打开");
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
        }

        private void Update()
        {
            // 每 0.3 秒刷新一次，确保状态实时更新
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastRepaintTime > 0.3)
            {
                _lastRepaintTime = now;
                Repaint();
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        #endregion

        #region 样式初始化

        private void EnsureStyles()
        {
            if (_stylesInitialized)
                return;

            _statusIndicatorOnStyle = new GUIStyle
            {
                normal = { background = MakeColorTexture(2, 2, Color.green) }
            };

            _statusIndicatorOffStyle = new GUIStyle
            {
                normal = { background = MakeColorTexture(2, 2, Color.grey) }
            };

            _headerLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11
            };

            _valueTextStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                fontSize = 11
            };

            _logEntryStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                normal = { textColor = new Color(0.75f, 0.75f, 0.75f) },
                fontSize = 10,
                margin = new RectOffset(2, 2, 0, 1)
            };

            _logErrorStyle = new GUIStyle(_logEntryStyle)
            {
                normal = { textColor = new Color(1f, 0.4f, 0.4f) }
            };

            _stylesInitialized = true;
        }

        private Texture2D MakeColorTexture(int width, int height, Color color)
        {
            var tex = new Texture2D(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    tex.SetPixel(x, y, color);
            tex.Apply();
            return tex;
        }

        #endregion

        #region OnGUI

        private void OnGUI()
        {
            EnsureStyles();

            DrawHeaderBar();
            EditorGUILayout.Space(4);

            DrawServerStatusCard();
            EditorGUILayout.Space(6);

            DrawControlsCard();
            EditorGUILayout.Space(6);

            DrawConnectionTestCard();
            EditorGUILayout.Space(6);

            DrawLogCard();
        }

        #endregion

        #region 绘制各区域

        /// <summary>
        /// 顶部标题栏
        /// </summary>
        private void DrawHeaderBar()
        {
            var bgRect = EditorGUILayout.BeginHorizontal(GUI.skin.box);
            {
                EditorGUILayout.LabelField("Unity MCP Server", _headerLabelStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("v1.0", _valueTextStyle);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 服务器运行状态卡片
        /// </summary>
        private void DrawServerStatusCard()
        {
            EditorGUILayout.LabelField("服务器状态", _sectionHeaderStyle);

            bool isRunning = UnityMcpHttpServer.IsRunning;

            EditorGUI.indentLevel++;

            // 状态行
            DrawStatusRow(isRunning);

            // 端口
            DrawLabelValueRow("端口", isRunning ? DEFAULT_PORT.ToString() : "—", showCopy: isRunning);

            // 监听地址
            string url = $"http://localhost:{DEFAULT_PORT}/";
            DrawLabelValueRow("监听地址", isRunning ? url : "—", showCopy: isRunning);

            // 服务模式
            DrawLabelValueRow("服务模式", isRunning ? "HTTP (JSON)" : "—");

            // 启动时间
            DrawLabelValueRow("进程", isRunning ? "运行中" : "已停止");

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 状态指示灯 + 文本
        /// </summary>
        private void DrawStatusRow(bool isRunning)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("状态", GUILayout.Width(LABEL_WIDTH));

                // 指示灯
                var indicatorRect = EditorGUILayout.GetControlRect(
                    false, STATUS_INDICATOR_SIZE, GUILayout.Width(STATUS_INDICATOR_SIZE));
                indicatorRect.y += (EditorGUIUtility.singleLineHeight - STATUS_INDICATOR_SIZE) / 2f;
                indicatorRect.width = STATUS_INDICATOR_SIZE;
                indicatorRect.height = STATUS_INDICATOR_SIZE;

                GUI.DrawTexture(indicatorRect,
                    isRunning
                        ? (Texture2D)EditorGUIUtility.whiteTexture
                        : Texture2D.whiteTexture);

                // 状态文本
                string statusText = isRunning ? "运行中" : "已停止";
                Color statusColor = isRunning ? Color.green : Color.grey;

                var style = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = statusColor }
                };
                EditorGUILayout.LabelField(statusText, style, GUILayout.Width(60f));

                // 状态微标签
                EditorGUILayout.LabelField(
                    isRunning ? "● 接受请求中" : "○ 未启动",
                    _valueTextStyle);

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 标签-值 行（可带复制按钮）
        /// </summary>
        private void DrawLabelValueRow(string label, string value, bool showCopy = false)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(LABEL_WIDTH));
                EditorGUILayout.SelectableLabel(value, EditorStyles.textField, GUILayout.Height(18f));

                if (showCopy && !string.IsNullOrEmpty(value) && value != "—")
                {
                    if (GUILayout.Button("复制", EditorStyles.miniButton, GUILayout.Width(40f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = value;
                        if (Event.current != null)
                            ShowNotification(new GUIContent("已复制"));
                    }
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 控制按钮卡片
        /// </summary>
        private void DrawControlsCard()
        {
            EditorGUILayout.LabelField("控制", _sectionHeaderStyle);

            bool isRunning = UnityMcpHttpServer.IsRunning;

            EditorGUI.indentLevel++;

            // 启停按钮行
            EditorGUILayout.BeginHorizontal();
            {
                using (new EditorGUI.DisabledGroupScope(isRunning))
                {
                    if (GUILayout.Button("▶ 启动", GUILayout.Height(BUTTON_HEIGHT)))
                    {
                        UnityMcpHttpServer.StartServer();
                        EnqueueLog("启动服务器");
                    }
                }

                GUILayout.Space(4);

                using (new EditorGUI.DisabledGroupScope(!isRunning))
                {
                    if (GUILayout.Button("■ 停止", GUILayout.Height(BUTTON_HEIGHT)))
                    {
                        UnityMcpHttpServer.StopServer();
                        EnqueueLog("停止服务器");
                        ClearPingCache();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // 重启按钮
            if (isRunning)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("↻ 重启", GUILayout.Height(BUTTON_HEIGHT)))
                    {
                        EnqueueLog("重启服务器...");
                        UnityMcpHttpServer.StopServer();
                        EditorApplication.delayCall += () =>
                        {
                            UnityMcpHttpServer.StartServer();
                            EnqueueLog("服务器已重启");
                        };
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 连接检测卡片
        /// </summary>
        private void DrawConnectionTestCard()
        {
            EditorGUILayout.LabelField("连接检测", _sectionHeaderStyle);

            bool isRunning = UnityMcpHttpServer.IsRunning;

            EditorGUI.indentLevel++;

            if (!isRunning)
            {
                EditorGUILayout.HelpBox("服务器未启动，无法进行连接检测。", MessageType.Info);
            }
            else
            {
                // Ping 按钮
                EditorGUILayout.BeginHorizontal();
                {
                    using (new EditorGUI.DisabledGroupScope(_isPinging))
                    {
                        string buttonLabel = _isPinging ? "检测中..." : "Ping 测试";
                        if (GUILayout.Button(buttonLabel, GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            PingServer();
                        }
                    }

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                // 结果显示
                DrawPingResult();
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Ping 结果展示行
        /// </summary>
        private void DrawPingResult()
        {
            if (string.IsNullOrEmpty(_pingStatus))
                return;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("最近检测", GUILayout.Width(LABEL_WIDTH));

                bool success = _pingStatus == "pong";
                Color color = success ? Color.green : Color.red;

                var labelStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = color },
                    richText = true
                };

                string latency = _lastTestLatencyMs > 0
                    ? $"  ({_lastTestLatencyMs} ms)"
                    : string.Empty;

                EditorGUILayout.LabelField(
                    success
                        ? $"✓ 连接成功{latency}"
                        : $"✗ 失败 — {_pingStatus}",
                    labelStyle);

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 活动日志卡片
        /// </summary>
        private void DrawLogCard()
        {
            EditorGUILayout.LabelField("活动日志", _sectionHeaderStyle);

            // 工具栏
            EditorGUILayout.BeginHorizontal();
            {
                int count;
                lock (_logLock)
                {
                    count = _logMessages.Count;
                }

                EditorGUILayout.LabelField(
                    count > 0 ? $"共 {count} 条" : "暂无日志",
                    _valueTextStyle);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("清空", EditorStyles.miniButton, GUILayout.Width(50f)))
                {
                    lock (_logLock)
                    {
                        _logMessages.Clear();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // 日志滚动区域
            var scrollRect = EditorGUILayout.BeginVertical(
                GUI.skin.box,
                GUILayout.ExpandHeight(true));

            Color defaultBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

            _scrollPosition = EditorGUILayout.BeginScrollView(
                _scrollPosition,
                false,
                true);

            // 还原背景
            GUI.backgroundColor = defaultBg;

            lock (_logLock)
            {
                if (_logMessages.Count == 0)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        EditorGUILayout.LabelField("（无日志）", EditorStyles.centeredGreyMiniLabel);
                    }
                }
                else
                {
                    foreach (string msg in _logMessages)
                    {
                        bool isError = msg.Contains("[Error]");
                        EditorGUILayout.LabelField(msg, isError ? _logErrorStyle : _logEntryStyle);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region 逻辑方法

        /// <summary>
        /// 入队日志（线程安全）
        /// </summary>
        private void EnqueueLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string entry = $"[{timestamp}] {message}";

            lock (_logLock)
            {
                _logMessages.Enqueue(entry);
                while (_logMessages.Count > MAX_LOG_LINES)
                {
                    _logMessages.Dequeue();
                }
            }

            // 自动滚到底部
            _scrollPosition.y = float.MaxValue;
        }

        /// <summary>
        /// 异步 Ping 服务器
        /// </summary>
        private void PingServer()
        {
            if (_isPinging) return;

            _isPinging = true;
            _pingStartTime = DateTime.UtcNow;
            _pingStatus = "检测中...";

            string url = $"http://localhost:{DEFAULT_PORT}/ping";

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "GET";
                    request.Timeout = 3000;

                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8))
                    {
                        string responseText = reader.ReadToEnd().Trim();
                        long latencyMs = (long)(DateTime.UtcNow - _pingStartTime).TotalMilliseconds;

                        bool success = responseText.Contains("\"success\":true")
                                       || responseText.Contains("\"message\":\"pong\"")
                                       || responseText == "pong";

                        _lastTestLatencyMs = latencyMs;

                        if (success)
                        {
                            _pingStatus = "pong";
                            EnqueueLog($"Ping 成功 | 延迟 {latencyMs}ms");
                        }
                        else
                        {
                            _pingStatus = $"响应异常: {Truncate(responseText, 60)}";
                            EnqueueLog($"[Error] Ping 响应异常: {Truncate(responseText, 60)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _lastTestLatencyMs = -1;
                    _pingStatus = ex.Message;
                    EnqueueLog($"[Error] Ping 失败: {ex.Message}");
                }
                finally
                {
                    _isPinging = false;
                }
            });
        }

        /// <summary>
        /// 清除 ping 结果缓存
        /// </summary>
        private void ClearPingCache()
        {
            _pingStatus = string.Empty;
            _isPinging = false;
            _lastTestLatencyMs = -1;
        }

        #endregion

        #region 日志回调

        /// <summary>
        /// 过滤 Unity 日志，捕获 MCP 相关消息
        /// </summary>
        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (!logString.Contains(MCP_LOG_PREFIX, StringComparison.OrdinalIgnoreCase))
                return;

            string prefix = type switch
            {
                LogType.Error => "[Error] ",
                LogType.Exception => "[Error] ",
                LogType.Warning => "[Warn]  ",
                _ => string.Empty
            };

            EnqueueLog($"{prefix}{Truncate(logString, 180)}");
        }

        #endregion

        #region 工具方法

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        #endregion
    }
}
