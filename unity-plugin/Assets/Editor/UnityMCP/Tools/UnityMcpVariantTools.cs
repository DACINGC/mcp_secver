using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpVariantTools
    {
        public static string CreateEffectVariants(RequestModel req)
        {
            string sourceObjectName = req.sourceObjectName;
            if (string.IsNullOrEmpty(sourceObjectName))
                return UnityMcpResponseUtils.Error("sourceObjectName is required");

            int count = Mathf.Clamp(req.count, 1, 50);
            float spacing = Mathf.Clamp((float)req.spacing, 0.1f, 100f);
            string variantPrefix = string.IsNullOrEmpty(req.variantPrefix) ? sourceObjectName : req.variantPrefix;

            GameObject source = GameObject.Find(sourceObjectName);
            if (source == null)
                return UnityMcpResponseUtils.Error($"Source GameObject '{sourceObjectName}' not found");

            var createdNames = new List<string>();

            for (int i = 0; i < count; i++)
            {
                string variantName = $"{variantPrefix}_{i + 1}";
                GameObject clone = GameObject.Instantiate(source);
                clone.name = variantName;
                clone.transform.position = source.transform.position + new Vector3((i - (count - 1) * 0.5f) * spacing, 0f, 0f);

                Undo.RegisterCreatedObjectUndo(clone, $"Create variant {variantName}");
                createdNames.Add(variantName);
            }

            var response = new ResponseModel
            {
                success = true,
                message = $"Created {count} variant(s) from '{sourceObjectName}'",
                objectName = sourceObjectName,
                affectedCount = count,
                objectNames = createdNames
            };
            return JsonUtility.ToJson(response);
        }

        public static string CaptureEffectVariants(RequestModel req)
        {
            string objectPrefix = req.objectPrefix ?? "";
            if (string.IsNullOrEmpty(objectPrefix))
                return UnityMcpResponseUtils.Error("objectPrefix is required");

            string filePrefix = string.IsNullOrEmpty(req.filePrefix) ? objectPrefix : req.filePrefix;
            string viewType = string.IsNullOrEmpty(req.viewType) ? "front" : req.viewType.ToLowerInvariant();

            var rootObjects = UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().GetRootGameObjects();

            var matching = new List<GameObject>();
            foreach (var go in rootObjects)
            {
                if (go.name.StartsWith(objectPrefix))
                    matching.Add(go);
            }

            if (matching.Count == 0)
                return UnityMcpResponseUtils.Error($"No GameObject found with prefix '{objectPrefix}'");

            var capturedPaths = new List<string>();

            if (SceneView.lastActiveSceneView == null)
                return UnityMcpResponseUtils.Error("No active SceneView available");

            for (int i = 0; i < matching.Count; i++)
            {
                var go = matching[i];
                Renderer renderer = null;
                try { renderer = go.GetComponent<Renderer>(); } catch { }
                Bounds bounds = renderer != null ? renderer.bounds : new Bounds(go.transform.position, Vector3.one * 2f);
                SceneView.lastActiveSceneView.Frame(bounds, false);
                SceneView.lastActiveSceneView.Repaint();

                string fileName = $"{filePrefix}_Variant_{i + 1}";
                string savePath = UnityMcpPathUtils.GetCaptureSavePath(fileName);

                var captureReq = new RequestModel
                {
                    objectName = go.name,
                    fileName = fileName,
                    viewType = viewType
                };

                string captureResult = UnityMcpPreviewTools.CaptureView(captureReq);
                capturedPaths.Add(savePath);
            }

            var response = new ResponseModel
            {
                success = true,
                message = $"Captured {capturedPaths.Count} variant(s) with prefix '{objectPrefix}'",
                objectName = objectPrefix,
                affectedCount = capturedPaths.Count,
                assetPaths = capturedPaths
            };
            return JsonUtility.ToJson(response);
        }
    }
}
