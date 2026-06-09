using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP
{
    [InitializeOnLoad]
    public static class UnityMcpHttpServer
    {
        private static HttpListener _listener;
        private static Thread _serverThread;
        private static bool _isRunning;
        private static readonly int Port = 8765;

        /// <summary>
        /// 服务器是否正在运行（供外部 UI 读取）
        /// </summary>
        public static bool IsRunning => _isRunning && _listener != null && _listener.IsListening;

        static UnityMcpHttpServer()
        {
            EditorApplication.delayCall += () =>
            {
                Debug.Log("[UnityMCP] Auto-starting HTTP server...");
                StartServer();
            };
        }

        [MenuItem("Unity MCP/Start Server")]
        public static void StartServer()
        {
            if (_isRunning)
            {
                Debug.Log("[UnityMCP] Server is already running.");
                return;
            }

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            _isRunning = true;

            _serverThread = new Thread(ListenerLoop)
            {
                IsBackground = true,
                Name = "UnityMCP_HttpListener"
            };
            _serverThread.Start();

            Debug.Log($"[UnityMCP] Unity MCP HTTP Server started at http://localhost:{Port}/");
        }

        [MenuItem("Unity MCP/Stop Server")]
        public static void StopServer()
        {
            _isRunning = false;

            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
                _listener = null;
            }

            _serverThread = null;
            Debug.Log("[UnityMCP] Unity MCP HTTP Server stopped.");
        }

        private static void ListenerLoop()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ProcessRequest(context);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UnityMCP] Listener error: {ex.Message}");
                }
            }
        }

        private static void ProcessRequest(HttpListenerContext context)
        {
            var capturedContext = context;
            string capturedJson = string.Empty;

            try
            {
                HttpListenerRequest request = context.Request;
                string path = request.Url.AbsolutePath.ToLowerInvariant();

                if (request.HttpMethod == "GET")
                {
                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            capturedJson = UnityMcpRouter.RouteGet(path);
                        }
                        catch (Exception ex)
                        {
                            capturedJson = UnityMcpResponseUtils.Error($"Handler error: {ex.Message}");
                        }
                        SendResponse(capturedContext, capturedJson);
                    };
                }
                else if (request.HttpMethod == "POST")
                {
                    string body;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }

                    RequestModel req;
                    if (!string.IsNullOrEmpty(body))
                    {
                        req = JsonUtility.FromJson<RequestModel>(body);
                    }
                    else
                    {
                        req = new RequestModel();
                    }

                    var capturedReq = req;
                    var capturedPath = path;

                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            capturedJson = UnityMcpRouter.RoutePost(capturedPath, capturedReq);
                        }
                        catch (Exception ex)
                        {
                            capturedJson = UnityMcpResponseUtils.Error($"Handler error: {ex.Message}");
                        }
                        SendResponse(capturedContext, capturedJson);
                    };
                }
                else
                {
                    capturedJson = UnityMcpResponseUtils.Error($"Unsupported method: {request.HttpMethod}");
                    SendResponse(capturedContext, capturedJson);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityMCP] ProcessRequest error: {ex.Message}");
                capturedJson = UnityMcpResponseUtils.Error($"Internal error: {ex.Message}");
                SendResponse(capturedContext, capturedJson);
            }
        }

        private static void SendResponse(HttpListenerContext context, string json)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityMCP] Response error: {ex.Message}");
            }
        }
    }
}
