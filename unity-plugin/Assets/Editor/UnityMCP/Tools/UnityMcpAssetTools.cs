using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpAssetTools
    {
        public static string ListGeneratedAssets(RequestModel req)
        {
            string assetType = string.IsNullOrEmpty(req.assetType) ? "all" : req.assetType.ToLowerInvariant();
            string baseFolder = "Assets/AI_Generated";

            if (!AssetDatabase.IsValidFolder(baseFolder))
            {
                var response = new ResponseModel
                {
                    success = true,
                    message = "No generated assets found.",
                    assetPaths = new List<string>()
                };
                return JsonUtility.ToJson(response);
            }

            string[] allAssets = AssetDatabase.FindAssets("", new[] { baseFolder });
            var results = new List<string>();

            foreach (string guid in allAssets)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string ext = Path.GetExtension(path).ToLowerInvariant();

                switch (assetType)
                {
                    case "prefab":
                        if (ext == ".prefab") results.Add(path);
                        break;
                    case "material":
                        if (ext == ".mat") results.Add(path);
                        break;
                    case "capture":
                        if (ext == ".png") results.Add(path);
                        break;
                    default:
                        if (ext == ".prefab" || ext == ".mat" || ext == ".png")
                            results.Add(path);
                        break;
                }
            }

            var resp = new ResponseModel
            {
                success = true,
                message = $"Found {results.Count} asset(s)",
                assetPaths = results
            };
            return JsonUtility.ToJson(resp);
        }

        public static string ClearAiGeneratedSceneObjects(RequestModel req)
        {
            string prefix = req.prefix;
            if (string.IsNullOrEmpty(prefix) || prefix.Length < 3)
                return UnityMcpResponseUtils.Error("prefix must be at least 3 characters");

            var rootObjects = UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().GetRootGameObjects();

            int removedCount = 0;
            var toRemove = new List<GameObject>();

            foreach (var go in rootObjects)
            {
                if (go.name.StartsWith(prefix))
                    toRemove.Add(go);
            }

            foreach (var go in toRemove)
            {
                Undo.DestroyObjectImmediate(go);
                removedCount++;
            }

            return UnityMcpResponseUtils.Success(
                $"Removed {removedCount} object(s) with prefix '{prefix}'",
                null, null, removedCount);
        }

        public static string GetObjectInfo(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            ObjectInfo info = BuildObjectInfo(go, req.includeChildren, 0);

            var response = new ResponseModel
            {
                success = true,
                message = $"Info for {objectName}",
                objectName = objectName,
                objectInfo = info
            };
            return JsonUtility.ToJson(response);
        }

        private static ObjectInfo BuildObjectInfo(GameObject go, bool includeChildren, int depth)
        {
            var info = new ObjectInfo
            {
                name = go.name,
                activeSelf = go.activeSelf,
                position = new Vector3Info
                {
                    x = go.transform.position.x,
                    y = go.transform.position.y,
                    z = go.transform.position.z
                },
                rotation = new Vector3Info
                {
                    x = go.transform.eulerAngles.x,
                    y = go.transform.eulerAngles.y,
                    z = go.transform.eulerAngles.z
                },
                scale = new Vector3Info
                {
                    x = go.transform.localScale.x,
                    y = go.transform.localScale.y,
                    z = go.transform.localScale.z
                },
                components = new List<string>(),
                children = null,
                particleSystemCount = 0,
                lightCount = 0,
                rendererCount = 0
            };

            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp != null)
                    info.components.Add(comp.GetType().Name);
            }

            info.particleSystemCount = go.GetComponents<ParticleSystem>().Length;
            info.lightCount = go.GetComponents<Light>().Length;
            info.rendererCount = go.GetComponents<Renderer>().Length;

            if (includeChildren && depth < 10)
            {
                int childCount = go.transform.childCount;
                int maxChildren = Mathf.Min(childCount, 100);
                if (maxChildren > 0)
                {
                    info.children = new List<ObjectInfo>();
                    for (int i = 0; i < maxChildren; i++)
                    {
                        Transform child = go.transform.GetChild(i);
                        info.children.Add(BuildObjectInfo(child.gameObject, true, depth + 1));
                    }
                }
            }

            return info;
        }
    }
}
