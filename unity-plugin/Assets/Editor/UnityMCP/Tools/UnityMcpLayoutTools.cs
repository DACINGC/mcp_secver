using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpLayoutTools
    {
        public static string LayoutObjects(RequestModel req)
        {
            string objectName = req.objectName;
            string prefabPath = req.prefabPath;
            string pattern = string.IsNullOrEmpty(req.pattern) ? "grid" : req.pattern.ToLower();
            int count = Mathf.Clamp((int)(req.count > 0 ? req.count : 10), 1, 1000);
            float spacing = Mathf.Clamp((float)(req.spacing > 0 ? req.spacing : 2.0), 0.1f, 100f);
            float radius = Mathf.Clamp((float)(req.radius > 0 ? req.radius : 5.0), 0.1f, 100f);
            bool usePrefab = !string.IsNullOrEmpty(prefabPath);

            GameObject source = null;
            GameObject prefab = null;
            List<GameObject> placed = new List<GameObject>();

            if (usePrefab)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                    return UnityMcpResponseUtils.Error($"Prefab not found at: {prefabPath}");
            }
            else
            {
                if (string.IsNullOrEmpty(objectName))
                    return UnityMcpResponseUtils.Error("Either objectName or prefabPath is required");
                source = GameObject.Find(objectName);
                if (source == null)
                    return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");
            }

            string baseName = usePrefab ? prefab.name : source.name;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = pattern switch
                {
                    "circle" => GetCirclePosition(i, count, radius),
                    "random" => GetRandomPosition(radius),
                    "line" => GetLinePosition(i, spacing),
                    _ => GetGridPosition(i, spacing),
                };

                GameObject instance;
                if (usePrefab)
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                }
                else
                {
                    instance = Object.Instantiate(source);
                    instance.transform.localScale = source.transform.localScale;
                    instance.transform.rotation = source.transform.rotation;
                }

                if (instance != null)
                {
                    instance.name = $"{baseName}_{i:D3}";
                    instance.transform.position = pos;
                    Undo.RegisterCreatedObjectUndo(instance, "Layout Object");
                    placed.Add(instance);
                }
            }

            return UnityMcpResponseUtils.Success($"Placed {placed.Count} objects in '{pattern}' layout", null, null, placed.Count);
        }

        private static Vector3 GetGridPosition(int index, float spacing)
        {
            int cols = Mathf.CeilToInt(Mathf.Sqrt(index + 1));
            int row = index / cols;
            int col = index % cols;
            return new Vector3(col * spacing - (cols - 1) * spacing / 2f, 0, row * spacing);
        }

        private static Vector3 GetCirclePosition(int index, int count, float radius)
        {
            float angle = (360f / count) * index * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
        }

        private static Vector3 GetRandomPosition(float radius)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float r = Random.Range(0f, radius);
            return new Vector3(Mathf.Sin(angle) * r, 0, Mathf.Cos(angle) * r);
        }

        private static Vector3 GetLinePosition(int index, float spacing)
        {
            return new Vector3(index * spacing - (spacing * 4.5f), 0, 0);
        }
    }
}
