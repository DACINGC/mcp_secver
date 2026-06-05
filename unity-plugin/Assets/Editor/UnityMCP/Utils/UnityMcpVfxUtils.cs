using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityMCP.Utils
{
    public static class UnityMcpVfxUtils
    {
        private static readonly string[,] ShaderPriority = new string[,]
        {
            { "Universal Render Pipeline/Particles/Unlit", "particle" },
            { "Universal Render Pipeline/Lit", "general" },
            { "Particles/Standard Unlit", "particle" },
            { "Standard", "general" },
            { "Unlit/Color", "general" }
        };

        public static Material CreateGeneratedMaterial(string materialName, Color color, bool emission = false, Color? emissionColor = null)
        {
            Shader shader = FindBestShader("particle");
            Material mat = new Material(shader);

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            if (emission && emissionColor.HasValue && mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emissionColor.Value);
                mat.EnableKeyword("_EMISSION");
            }

            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", 0f);

            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            string path = UnityMcpPathUtils.GetMaterialSavePath(materialName);
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            return mat;
        }

        public static Material CreateLineRendererMaterial(string materialName, Color color)
        {
            Shader shader = FindBestShader("general");
            Material mat = new Material(shader);

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            if (mat.HasProperty("_EmissionColor"))
            {
                Color hdr = UnityMcpColorUtils.MultiplyColor(color, 2.0f);
                mat.SetColor("_EmissionColor", hdr);
                mat.EnableKeyword("_EMISSION");
            }

            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", 0f);

            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            string path = UnityMcpPathUtils.GetMaterialSavePath(materialName);
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            return mat;
        }

        public static Shader FindBestShader(string category)
        {
            for (int i = 0; i < ShaderPriority.GetLength(0); i++)
            {
                string name = ShaderPriority[i, 0];
                string cat = ShaderPriority[i, 1];
                if (cat == category || cat == "general")
                {
                    Shader s = Shader.Find(name);
                    if (s != null) return s;
                }
            }
            for (int i = 0; i < ShaderPriority.GetLength(0); i++)
            {
                Shader s = Shader.Find(ShaderPriority[i, 0]);
                if (s != null) return s;
            }
            return Shader.Find("Standard");
        }

        public static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        public static ParticleSystem CreateParticleChild(Transform parent, string name)
        {
            GameObject child = CreateChild(parent, name);
            return child.AddComponent<ParticleSystem>();
        }

        public static void ConfigureMain(ParticleSystem ps, float duration, float startLifetime, float startSpeed, float startSize, bool loop)
        {
            var main = ps.main;
            main.duration = duration;
            main.startLifetime = startLifetime;
            main.startSpeed = startSpeed;
            main.startSize = startSize;
            main.loop = loop;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
        }

        public static void ConfigureMainColor(ParticleSystem ps, Color color)
        {
            var main = ps.main;
            main.startColor = color;
        }

        public static void ConfigureEmissionRate(ParticleSystem ps, float rateOverTime)
        {
            var emission = ps.emission;
            emission.rateOverTime = rateOverTime;
        }

        public static void ConfigureBurst(ParticleSystem ps, int count, float time = 0f, int cycleCount = 1)
        {
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(time, (short)count)
            });
        }

        public static void ConfigureShapeCircle(ParticleSystem ps, float radius)
        {
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.arc = 360f;
        }

        public static void ConfigureShapeSphere(ParticleSystem ps, float radius)
        {
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
        }

        public static void ConfigureShapeCone(ParticleSystem ps, float angle, float radius)
        {
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
        }

        public static void ConfigureColorOverLifetime(ParticleSystem ps, Gradient gradient)
        {
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        public static void ConfigureSizeOverLifetime(ParticleSystem ps, AnimationCurve curve)
        {
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        public static void ConfigureVelocityOverLifetime(ParticleSystem ps, Vector3 velocity)
        {
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(velocity.x);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(velocity.y);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(velocity.z);
        }

        public static void ConfigureNoise(ParticleSystem ps, float strength, float frequency)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = strength;
            noise.frequency = frequency;
            noise.octaveCount = 2;
        }

        public static void ConfigureTrails(ParticleSystem ps, float lifetime, Color color, float minVertexDistance = 0.1f)
        {
            var trails = ps.trails;
            trails.enabled = true;
            trails.lifetime = lifetime;
            trails.minVertexDistance = minVertexDistance;
            trails.colorOverLifetime = color;
        }

        public static void SetParticleMaterial(ParticleSystem ps, Material mat)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = mat;
            renderer.sortMode = ParticleSystemSortMode.Distance;
        }

        public static GameObject CreatePointLight(Transform parent, string name, Color color, float intensity, float range)
        {
            GameObject go = CreateChild(parent, name);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            return go;
        }

        public static GameObject CreateLineRendererArc(Transform parent, string name, float radius, float width, Color color, int segments = 24)
        {
            GameObject go = CreateChild(parent, name);
            LineRenderer lr = go.AddComponent<LineRenderer>();

            Material mat = CreateLineRendererMaterial(name + "_Mat", color);
            lr.sharedMaterial = mat;
            lr.startWidth = width;
            lr.endWidth = width * 0.3f;
            lr.positionCount = segments + 1;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = t * Mathf.PI;
                float x = Mathf.Sin(angle) * radius;
                float y = Mathf.Cos(angle) * radius * 0.3f;
                float z = 0f;
                lr.SetPosition(i, new Vector3(x, y + radius * 0.5f, z));
            }

            return go;
        }

        public static GameObject CreateLineRendererCircle(Transform parent, string name, float radius, float width, Color color, int segments = 32, float yOffset = 0f)
        {
            GameObject go = CreateChild(parent, name);
            LineRenderer lr = go.AddComponent<LineRenderer>();

            Material mat = CreateLineRendererMaterial(name + "_Mat", color);
            lr.sharedMaterial = mat;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.loop = true;
            lr.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                lr.SetPosition(i, new Vector3(x, yOffset, z));
            }

            return go;
        }

        public static List<Vector3> GenerateLightningPoints(Vector3 start, Vector3 end, float displacement, int pointCount, System.Random rng)
        {
            var points = new List<Vector3>();
            points.Add(start);

            Vector3 direction = (end - start).normalized;
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            if (right.magnitude < 0.01f)
                right = Vector3.Cross(direction, Vector3.forward).normalized;
            Vector3 up = Vector3.Cross(right, direction).normalized;

            for (int i = 1; i < pointCount - 1; i++)
            {
                float t = i / (float)(pointCount - 1);
                Vector3 basePos = Vector3.Lerp(start, end, t);
                float offset = (float)(rng.NextDouble() * 2.0 - 1.0) * displacement * (1f - Mathf.Abs(t - 0.5f) * 1.8f);
                basePos += right * offset * 0.5f;
                basePos += up * (float)(rng.NextDouble() * 2.0 - 1.0) * offset * 0.3f;
                points.Add(basePos);
            }

            points.Add(end);
            return points;
        }

        public static GameObject CreateLightningBolt(Transform parent, string name, List<Vector3> points, float width, Color color)
        {
            GameObject go = CreateChild(parent, name);
            LineRenderer lr = go.AddComponent<LineRenderer>();

            Material mat = CreateLineRendererMaterial(name + "_Mat", color);
            lr.sharedMaterial = mat;
            lr.startWidth = width;
            lr.endWidth = width * 0.3f;
            lr.positionCount = points.Count;

            for (int i = 0; i < points.Count; i++)
                lr.SetPosition(i, points[i]);

            return go;
        }

        public static void SaveAsPrefab(GameObject root, string effectName)
        {
            string safeName = UnityMcpPathUtils.SanitizeFileName(effectName);
            string baseFolder = "Assets/AI_Generated/Prefabs";
            UnityMcpPathUtils.EnsureDirectoryExists(baseFolder);
            string path = $"{baseFolder}/{safeName}.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                int counter = 1;
                while (AssetDatabase.LoadAssetAtPath<GameObject>($"{baseFolder}/{safeName}_{counter}.prefab") != null)
                    counter++;
                path = $"{baseFolder}/{safeName}_{counter}.prefab";
            }

            PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (success)
                AssetDatabase.Refresh();
        }
    }
}
