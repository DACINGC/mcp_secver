using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpTerrainTools
    {
        public static string CreateTerrain(RequestModel req)
        {
            string name = string.IsNullOrEmpty(req.name) ? "Terrain" : req.name;
            name = UnityMcpPathUtils.SanitizeFileName(name);

            int w = Mathf.Clamp((int)(req.width > 0 ? req.width : 500), 10, 2000);
            int l = Mathf.Clamp((int)(req.length > 0 ? req.length : 500), 10, 2000);
            float h = Mathf.Clamp((float)(req.height > 0 ? req.height : 50), 1f, 1000f);
            int res = Mathf.Clamp((int)(req.density > 0 ? req.density : 513), 33, 4097);

            TerrainData data = new TerrainData();
            data.heightmapResolution = res;
            data.size = new Vector3(w, h, l);

            float[,] heights = new float[res, res];
            for (int x = 0; x < res; x++)
                for (int y = 0; y < res; y++)
                    heights[x, y] = 0f;
            data.SetHeights(0, 0, heights);

            GameObject go = Terrain.CreateTerrainGameObject(data);
            go.name = name;

            float px = Mathf.Clamp((float)req.x, -10000f, 10000f);
            float py = Mathf.Clamp((float)req.y, -10000f, 10000f);
            float pz = Mathf.Clamp((float)req.z, -10000f, 10000f);
            go.transform.position = new Vector3(px, py, pz);

            Undo.RegisterCreatedObjectUndo(go, "Create Terrain");
            if (!string.IsNullOrEmpty(req.parent))
            {
                GameObject parent = GameObject.Find(req.parent);
                if (parent != null) go.transform.parent = parent.transform;
            }
            return UnityMcpResponseUtils.Success($"Created terrain '{name}' ({w}x{l}, h={h})", name);
        }

        public static string SculptTerrain(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            Terrain terrain = go.GetComponent<Terrain>();
            if (terrain == null)
                return UnityMcpResponseUtils.Error($"'{objectName}' has no Terrain component");

            TerrainData data = terrain.terrainData;
            int res = data.heightmapResolution;
            float strength = Mathf.Clamp((float)req.strength, 0f, 1f);
            if (strength <= 0f) strength = 0.5f;

            string shape = string.IsNullOrEmpty(req.shape) ? "smooth" : req.shape.ToLower();
            float[,] heights = new float[res, res];
            float cx = res / 2f;
            float cy = res / 2f;
            float maxDist = res / 2f;
            int seed = Random.Range(0, 10000);

            for (int x = 0; x < res; x++)
            {
                for (int y = 0; y < res; y++)
                {
                    float val = 0f;
                    switch (shape)
                    {
                        case "flat":
                            val = 0f;
                            break;
                        case "smooth":
                        {
                            float dx = (x - cx) / maxDist;
                            float dy = (y - cy) / maxDist;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            val = Mathf.Clamp01((1f - dist) * strength);
                            break;
                        }
                        case "mountain":
                        {
                            float dx = (x - cx) / maxDist;
                            float dy = (y - cy) / maxDist;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            val = Mathf.Clamp01((1f - dist) * strength);
                            val = val * val * val;
                            break;
                        }
                        case "valley":
                        {
                            float dx = (x - cx) / maxDist;
                            float dy = (y - cy) / maxDist;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            val = Mathf.Clamp01(dist * strength);
                            break;
                        }
                        case "random":
                        {
                            float nx = x * 0.02f;
                            float ny = y * 0.02f;
                            val = Mathf.PerlinNoise(nx + seed, ny + seed) * strength;
                            break;
                        }
                        default:
                            val = 0f;
                            break;
                    }
                    heights[x, y] = val;
                }
            }

            Undo.RecordObject(data, "Sculpt Terrain");
            data.SetHeights(0, 0, heights);
            return UnityMcpResponseUtils.Success($"Sculpted terrain '{objectName}' with shape '{shape}'", objectName);
        }

        public static string PaintTerrain(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            Terrain terrain = go.GetComponent<Terrain>();
            if (terrain == null)
                return UnityMcpResponseUtils.Error($"'{objectName}' has no Terrain component");

            TerrainData data = terrain.terrainData;
            string layerType = string.IsNullOrEmpty(req.layerType) ? "grass" : req.layerType.ToLower();

            int w = data.alphamapWidth;
            int h = data.alphamapHeight;
            float[,,] splat = new float[h, w, 1];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    splat[y, x, 0] = 1f;

            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = layerType switch
            {
                "grass" => Resources.Load<Texture2D>("Textures/grass") ?? EditorGUIUtility.whiteTexture,
                "sand" => Resources.Load<Texture2D>("Textures/sand") ?? EditorGUIUtility.whiteTexture,
                "rock" => Resources.Load<Texture2D>("Textures/rock") ?? EditorGUIUtility.whiteTexture,
                "snow" => Resources.Load<Texture2D>("Textures/snow") ?? EditorGUIUtility.whiteTexture,
                _ => null,
            };
            if (layer.diffuseTexture == null)
                layer.diffuseTexture = EditorGUIUtility.whiteTexture;

            Undo.RecordObject(data, "Paint Terrain");
            data.terrainLayers = new TerrainLayer[] { layer };
            data.SetAlphamaps(0, 0, splat);
            return UnityMcpResponseUtils.Success($"Painted terrain '{objectName}' with layer '{layerType}'", objectName);
        }
    }
}
