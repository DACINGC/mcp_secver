using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpSceneTools
    {
        public static string CreateEmpty(RequestModel req)
        {
            string name = string.IsNullOrEmpty(req.name) ? "EmptyObject" : req.name;
            name = UnityMcpPathUtils.SanitizeFileName(name);

            GameObject go = new GameObject(name);
            float x = Mathf.Clamp((float)req.x, -10000f, 10000f);
            float y = Mathf.Clamp((float)req.y, -10000f, 10000f);
            float z = Mathf.Clamp((float)req.z, -10000f, 10000f);
            go.transform.position = new Vector3(x, y, z);

            Undo.RegisterCreatedObjectUndo(go, "Create Empty");
            return UnityMcpResponseUtils.Success($"Created {name}", name);
        }

        public static string ListSceneObjects()
        {
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => go.scene.isLoaded && go.hideFlags == HideFlags.None)
                .ToList();

            var sceneObjects = new List<SceneObjectInfo>();
            foreach (var go in allObjects)
            {
                var info = new SceneObjectInfo
                {
                    name = go.name,
                    type = go.GetType().Name,
                    x = go.transform.position.x,
                    y = go.transform.position.y,
                    z = go.transform.position.z
                };
                sceneObjects.Add(info);
            }

            var response = new ResponseModel
            {
                success = true,
                message = $"Found {sceneObjects.Count} objects",
                objects = sceneObjects
            };
            return UnityMcpResponseUtils.ToJson(response);
        }

        public static string SetTransform(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            float x = Mathf.Clamp((float)req.x, -10000f, 10000f);
            float y = Mathf.Clamp((float)req.y, -10000f, 10000f);
            float z = Mathf.Clamp((float)req.z, -10000f, 10000f);
            float rx = Mathf.Clamp((float)req.rx, -360f, 360f);
            float ry = Mathf.Clamp((float)req.ry, -360f, 360f);
            float rz = Mathf.Clamp((float)req.rz, -360f, 360f);
            float sx = Mathf.Clamp((float)req.sx, 0.01f, 1000f);
            float sy = Mathf.Clamp((float)req.sy, 0.01f, 1000f);
            float sz = Mathf.Clamp((float)req.sz, 0.01f, 1000f);

            Undo.RecordObject(go.transform, "Set Transform");
            go.transform.position = new Vector3(x, y, z);
            go.transform.rotation = Quaternion.Euler(rx, ry, rz);
            go.transform.localScale = new Vector3(sx, sy, sz);

            return UnityMcpResponseUtils.Success($"Set transform of {objectName}", objectName);
        }
    }
}
