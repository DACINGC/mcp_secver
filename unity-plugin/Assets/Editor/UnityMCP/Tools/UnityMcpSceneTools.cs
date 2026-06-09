using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpSceneTools
    {
        private static void TrySetParent(GameObject child, string parentName)
        {
            if (string.IsNullOrEmpty(parentName)) return;
            GameObject parent = GameObject.Find(parentName);
            if (parent != null)
                child.transform.parent = parent.transform;
        }

        public static string CreateEmpty(RequestModel req)
        {
            string name = string.IsNullOrEmpty(req.name) ? "EmptyObject" : req.name;
            name = UnityMcpPathUtils.SanitizeFileName(name);

            GameObject go = new GameObject(name);
            float x = Mathf.Clamp((float)req.x, -10000f, 10000f);
            float y = Mathf.Clamp((float)req.y, -10000f, 10000f);
            float z = Mathf.Clamp((float)req.z, -10000f, 10000f);
            go.transform.position = new Vector3(x, y, z);
            TrySetParent(go, req.parent);

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

        public static string CreatePrimitive(RequestModel req)
        {
            string type = string.IsNullOrEmpty(req.primitiveType) ? "Cube" : req.primitiveType;
            string name = string.IsNullOrEmpty(req.name) ? type : req.name;
            name = UnityMcpPathUtils.SanitizeFileName(name);

            PrimitiveType pType = type switch
            {
                "Sphere" => PrimitiveType.Sphere,
                "Capsule" => PrimitiveType.Capsule,
                "Cylinder" => PrimitiveType.Cylinder,
                "Cube" => PrimitiveType.Cube,
                "Plane" => PrimitiveType.Plane,
                "Quad" => PrimitiveType.Quad,
                _ => PrimitiveType.Cube,
            };

            GameObject go = GameObject.CreatePrimitive(pType);
            go.name = name;

            float x = Mathf.Clamp((float)req.x, -10000f, 10000f);
            float y = Mathf.Clamp((float)req.y, -10000f, 10000f);
            float z = Mathf.Clamp((float)req.z, -10000f, 10000f);
            go.transform.position = new Vector3(x, y, z);

            float sx = (float)(req.sx > 0 ? req.sx : 1.0);
            float sy = (float)(req.sy > 0 ? req.sy : 1.0);
            float sz = (float)(req.sz > 0 ? req.sz : 1.0);

            if (type == "Sphere" && req.radius > 0)
            {
                float r = Mathf.Clamp((float)req.radius, 0.01f, 100f);
                go.transform.localScale = Vector3.one * r * 2f;
            }
            else if (type == "Cube" && req.width > 0)
            {
                float w = Mathf.Clamp((float)req.width, 0.01f, 1000f);
                go.transform.localScale = Vector3.one * w;
            }
            else
            {
                go.transform.localScale = new Vector3(
                    Mathf.Clamp(sx, 0.01f, 1000f),
                    Mathf.Clamp(sy, 0.01f, 1000f),
                    Mathf.Clamp(sz, 0.01f, 1000f)
                );
            }

            if (!string.IsNullOrEmpty(req.color) && req.color != "#FFFFFF")
            {
                Color color = UnityMcpColorUtils.ParseHtmlColor(req.color);
                Renderer renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = color;
                    renderer.material = mat;
                }
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Primitive");
            TrySetParent(go, req.parent);
            return UnityMcpResponseUtils.Success($"Created {type} '{name}'", name);
        }

        public static string CreateSampleScene(RequestModel req)
        {
            string rootName = string.IsNullOrEmpty(req.name) ? "SampleScene" : req.name;
            rootName = UnityMcpPathUtils.SanitizeFileName(rootName);
            float gs = Mathf.Clamp((float)(req.groundSize > 0 ? req.groundSize : 20.0), 1f, 500f);
            bool walls = req.includeWalls;
            bool lights = req.includeLights;

            GameObject root = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Sample Scene");
            TrySetParent(root, req.parent);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, 0, 0);
            ground.transform.localScale = new Vector3(gs / 10f, 1, gs / 10f);
            ground.transform.parent = root.transform;

            if (!string.IsNullOrEmpty(req.color))
            {
                Color c = UnityMcpColorUtils.ParseHtmlColor(req.color);
                Renderer r = ground.GetComponent<Renderer>();
                if (r != null)
                {
                    Material m = new Material(Shader.Find("Standard"));
                    m.color = c;
                    r.material = m;
                }
            }

            if (lights)
            {
                GameObject dirLight = new GameObject("Directional Light");
                Light dl = dirLight.AddComponent<Light>();
                dl.type = LightType.Directional;
                dl.color = Color.white;
                dl.intensity = 1.0f;
                dirLight.transform.position = new Vector3(0, gs * 0.5f, 0);
                dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);
                dirLight.transform.parent = root.transform;

                GameObject ptLight = new GameObject("Point Light");
                Light pl = ptLight.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = new Color(1f, 0.8f, 0.6f);
                pl.intensity = 2.0f;
                pl.range = gs * 0.3f;
                ptLight.transform.position = new Vector3(0, gs * 0.2f, 0);
                ptLight.transform.parent = root.transform;
            }

            if (walls)
            {
                float half = gs / 2f;
                float w = 1f;
                Color wallColor = new Color(0.6f, 0.6f, 0.6f);

                void BuildWall(string wn, float px, float py, float pz, float sx, float sy, float sz)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = wn;
                    wall.transform.position = new Vector3(px, py, pz);
                    wall.transform.localScale = new Vector3(sx, sy, sz);
                    Renderer wr = wall.GetComponent<Renderer>();
                    if (wr != null)
                    {
                        Material wm = new Material(Shader.Find("Standard"));
                        wm.color = wallColor;
                        wr.material = wm;
                    }
                    wall.transform.parent = root.transform;
                }

                BuildWall("Wall_North", 0, w / 2f, half, gs, w, w);
                BuildWall("Wall_South", 0, w / 2f, -half, gs, w, w);
                BuildWall("Wall_East", half, w / 2f, 0, w, w, gs);
                BuildWall("Wall_West", -half, w / 2f, 0, w, w, gs);
            }

            return UnityMcpResponseUtils.Success($"Created sample scene '{rootName}'", rootName);
        }

        public static string ResetScene(RequestModel req)
        {
            string suiteName = "AI_TestSuite";
            GameObject suite = GameObject.Find(suiteName);
            int removed = 0;

            if (suite != null)
            {
                Object.DestroyImmediate(suite);
                removed = 1;
                return UnityMcpResponseUtils.Success($"Destroyed '{suiteName}' container and all its children");
            }

            bool hasSpecific = req.keepLights || req.keepTerrain;
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var toRemove = new List<GameObject>();
            int kept = 0;

            foreach (var go in rootObjects)
            {
                bool skip = false;
                if (hasSpecific)
                {
                    if (req.keepLights && go.GetComponent<Light>() != null) skip = true;
                    if (req.keepTerrain && go.GetComponent<Terrain>() != null) skip = true;
                }
                else
                {
                    if (go.GetComponent<Camera>() != null) skip = true;
                    if (go.name == "Directional Light") skip = true;
                }
                if (skip) { kept++; continue; }
                toRemove.Add(go);
            }

            foreach (var go in toRemove)
                Object.DestroyImmediate(go);

            return UnityMcpResponseUtils.Success($"Removed {toRemove.Count} objects, kept {kept}");
        }

        public static string CreateCamera(RequestModel req)
        {
            string name = string.IsNullOrEmpty(req.name) ? "Main Camera" : req.name;
            name = UnityMcpPathUtils.SanitizeFileName(name);

            GameObject go = new GameObject(name);
            Camera cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;

            float x = Mathf.Clamp((float)req.x, -10000f, 10000f);
            float y = Mathf.Clamp((float)req.y, -10000f, 10000f);
            float z = Mathf.Clamp((float)req.z, -10000f, 10000f);
            go.transform.position = new Vector3(x, y, z);

            float rx = Mathf.Clamp((float)req.rx, -360f, 360f);
            float ry = Mathf.Clamp((float)req.ry, -360f, 360f);
            float rz = Mathf.Clamp((float)req.rz, -360f, 360f);
            go.transform.rotation = Quaternion.Euler(rx, ry, rz);

            TrySetParent(go, req.parent);
            Undo.RegisterCreatedObjectUndo(go, "Create Camera");
            return UnityMcpResponseUtils.Success($"Created Camera '{name}'", name);
        }

        public static string CreateTestSuite(RequestModel req)
        {
            string name = string.IsNullOrEmpty(req.name) ? "AI_TestSuite" : req.name;
            name = UnityMcpPathUtils.SanitizeFileName(name);
            GameObject existing = GameObject.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing);

            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Test Suite");
            return UnityMcpResponseUtils.Success($"Created test suite container '{name}'", name);
        }
    }
}
