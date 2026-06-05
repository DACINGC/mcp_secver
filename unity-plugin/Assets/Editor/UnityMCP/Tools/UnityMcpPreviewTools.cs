using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpPreviewTools
    {
        public static string FocusSceneObject(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            Selection.activeGameObject = go;

            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();

            return UnityMcpResponseUtils.Success($"Focused on {objectName}", objectName);
        }

        public static string PlayEffect(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            int affectedCount = 0;

            if (req.includeChildren)
            {
                var allPs = go.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in allPs)
                {
                    ps.Clear();
                    ps.Play();
                    affectedCount++;
                }
            }
            else
            {
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Clear();
                    ps.Play();
                    affectedCount = 1;
                }
            }

            return UnityMcpResponseUtils.Success(
                $"Played {affectedCount} particle system(s) on {objectName}",
                objectName, null, affectedCount);
        }

        public static string StopEffect(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            bool clear = req.clearParticles;
            int affectedCount = 0;

            if (req.includeChildren)
            {
                var allPs = go.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in allPs)
                {
                    ps.Stop();
                    if (clear) ps.Clear();
                    affectedCount++;
                }
            }
            else
            {
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop();
                    if (clear) ps.Clear();
                    affectedCount = 1;
                }
            }

            return UnityMcpResponseUtils.Success(
                $"Stopped {affectedCount} particle system(s) on {objectName}",
                objectName, null, affectedCount);
        }

        public static string CaptureView(RequestModel req)
        {
            string fileName = string.IsNullOrEmpty(req.fileName) ? "unity_capture" : req.fileName;
            string viewType = string.IsNullOrEmpty(req.viewType) ? "scene" : req.viewType.ToLowerInvariant();
            int width = Mathf.Clamp((int)req.width, 256, 3840);
            int height = Mathf.Clamp((int)req.height, 256, 2160);

            string savePath = UnityMcpPathUtils.GetCaptureSavePath(fileName);

            if (viewType == "game")
            {
                string absPath = System.IO.Path.GetFullPath(savePath);
                ScreenCapture.CaptureScreenshot(absPath);
                AssetDatabase.Refresh();

                if (System.IO.File.Exists(absPath))
                {
                    return UnityMcpResponseUtils.Success(
                        $"GameView capture saved to {savePath}", "", savePath);
                }
                else
                {
                    viewType = "scene";
                }
            }

            if (viewType == "scene")
            {
                if (SceneView.lastActiveSceneView == null)
                    return UnityMcpResponseUtils.Error("No active SceneView found. Open a Scene view tab.");

                Camera cam = SceneView.lastActiveSceneView.camera;
                if (cam == null)
                    return UnityMcpResponseUtils.Error("SceneView has no camera.");

                RenderTexture rt = new RenderTexture(width, height, 24);
                RenderTexture prevTarget = cam.targetTexture;
                RenderTexture prevActive = RenderTexture.active;

                try
                {
                    cam.targetTexture = rt;
                    cam.Render();

                    RenderTexture.active = rt;
                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();

                    byte[] pngData = tex.EncodeToPNG();
                    string absPath = System.IO.Path.GetFullPath(savePath);
                    System.IO.File.WriteAllBytes(absPath, pngData);

                    DestroyUtility(tex);

                    AssetDatabase.Refresh();
                    return UnityMcpResponseUtils.Success(
                        $"SceneView capture saved to {savePath}", "", savePath);
                }
                finally
                {
                    cam.targetTexture = prevTarget;
                    RenderTexture.active = prevActive;
                    if (rt != null)
                        DestroyUtility(rt);
                }
            }

            return UnityMcpResponseUtils.Error($"Unsupported viewType: {viewType}. Use 'scene' or 'game'.");
        }

        private static void DestroyUtility(Object obj)
        {
            if (obj != null)
                Object.DestroyImmediate(obj);
        }
    }
}
