using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpTuningTools
    {
        public static string UpdateParticleSystem(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            var particleSystems = go.GetComponentsInChildren<ParticleSystem>(true);
            if (particleSystems.Length == 0)
                return UnityMcpResponseUtils.Error($"No ParticleSystem found on '{objectName}' or its children");

            int affected = 0;
            bool localDuration = req.duration > 0;
            bool localEmissionRate = req.emissionRate > 0;
            bool localStartLifetime = req.startLifetime > 0;
            bool localStartSpeed = req.startSpeed > 0;
            bool localStartSize = req.startSize > 0;
            bool localLoop = req.loop;
            bool localColor = !string.IsNullOrEmpty(req.color);

            Color color = Color.white;
            if (localColor)
                color = UnityMcpColorUtils.ParseHtmlColor(req.color);

            foreach (var ps in particleSystems)
            {
                var main = ps.main;

                if (localDuration) main.duration = Mathf.Clamp((float)req.duration, 0.01f, 300f);
                if (localStartLifetime) main.startLifetime = Mathf.Clamp((float)req.startLifetime, 0.01f, 300f);
                if (localStartSpeed) main.startSpeed = Mathf.Clamp((float)req.startSpeed, 0f, 1000f);
                if (localStartSize) main.startSize = Mathf.Clamp((float)req.startSize, 0.001f, 100f);
                if (localLoop) main.loop = req.loop;
                if (localColor) main.startColor = color;

                if (localEmissionRate)
                {
                    var emission = ps.emission;
                    emission.rateOverTime = Mathf.Clamp((float)req.emissionRate, 0f, 100000f);
                }

                affected++;
            }

            EditorUtility.SetDirty(go);
            return UnityMcpResponseUtils.Success(
                $"Updated {affected} ParticleSystem(s) on '{objectName}'",
                objectName, null, affected);
        }

        public static string UpdateLight(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            var lights = go.GetComponentsInChildren<Light>(true);
            if (lights.Length == 0)
                return UnityMcpResponseUtils.Error($"No Light found on '{objectName}' or its children");

            int affected = 0;
            bool localColor = !string.IsNullOrEmpty(req.color);
            bool localIntensity = req.intensity > 0;
            bool localRange = req.range > 0;

            Color color = Color.white;
            if (localColor)
                color = UnityMcpColorUtils.ParseHtmlColor(req.color);

            foreach (var light in lights)
            {
                if (localColor) light.color = color;
                if (localIntensity) light.intensity = Mathf.Clamp((float)req.intensity, 0f, 100000f);
                if (localRange) light.range = Mathf.Clamp((float)req.range, 0.01f, 1000f);
                affected++;
            }

            EditorUtility.SetDirty(go);
            return UnityMcpResponseUtils.Success(
                $"Updated {affected} Light(s) on '{objectName}'",
                objectName, null, affected);
        }

        public static string UpdateLineRenderer(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            var lineRenderers = go.GetComponentsInChildren<LineRenderer>(true);
            if (lineRenderers.Length == 0)
                return UnityMcpResponseUtils.Error($"No LineRenderer found on '{objectName}' or its children");

            int affected = 0;
            bool localColor = !string.IsNullOrEmpty(req.color);
            bool localWidth = req.width > 0;
            bool localStartWidth = req.sx > 0;
            bool localEndWidth = req.sy > 0;

            Color color = Color.white;
            if (localColor)
                color = UnityMcpColorUtils.ParseHtmlColor(req.color);

            foreach (var lr in lineRenderers)
            {
                if (localColor && lr.sharedMaterial != null)
                {
                    Material mat = lr.sharedMaterial;
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", color);
                    else if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", color);

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", UnityMcpColorUtils.MultiplyColor(color, 2f));
                        mat.EnableKeyword("_EMISSION");
                    }
                }

                if (localWidth)
                {
                    lr.startWidth = Mathf.Clamp((float)req.width, 0.001f, 10f);
                    lr.endWidth = Mathf.Clamp((float)req.width * 0.3f, 0.001f, 10f);
                }
                else
                {
                    if (localStartWidth) lr.startWidth = Mathf.Clamp((float)req.sx, 0.001f, 10f);
                    if (localEndWidth) lr.endWidth = Mathf.Clamp((float)req.sy, 0.001f, 10f);
                }

                affected++;
            }

            EditorUtility.SetDirty(go);
            return UnityMcpResponseUtils.Success(
                $"Updated {affected} LineRenderer(s) on '{objectName}'",
                objectName, null, affected);
        }

        public static string RecolorEffect(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            if (string.IsNullOrEmpty(req.color))
                return UnityMcpResponseUtils.Error("color is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            Color newColor = UnityMcpColorUtils.ParseHtmlColor(req.color);
            int affected = 0;

            bool affectParticles = req.affectParticles;
            bool affectLights = req.affectLights;
            bool affectRenderers = req.affectRenderers;
            bool affectLines = req.affectLines;

            if (!affectParticles && !affectLights && !affectRenderers && !affectLines)
            {
                affectParticles = true;
                affectLights = true;
                affectRenderers = true;
                affectLines = true;
            }

            if (affectParticles)
            {
                var particleSystems = go.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particleSystems)
                {
                    var main = ps.main;
                    main.startColor = newColor;

                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        Material mat = CloneMaterialForObject(renderer.sharedMaterial, objectName, ps.name);
                        if (mat == null)
                            mat = renderer.sharedMaterial;

                        if (mat.HasProperty("_BaseColor"))
                            mat.SetColor("_BaseColor", newColor);
                        else if (mat.HasProperty("_Color"))
                            mat.SetColor("_Color", newColor);

                        Color emissionColor = UnityMcpColorUtils.MultiplyColor(newColor, 3f);
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.SetColor("_EmissionColor", emissionColor);
                            mat.EnableKeyword("_EMISSION");
                        }

                        renderer.material = mat;
                    }
                    affected++;
                }
            }

            if (affectLights)
            {
                var lights = go.GetComponentsInChildren<Light>(true);
                foreach (var light in lights)
                {
                    light.color = newColor;
                    affected++;
                }
            }

            if (affectRenderers)
            {
                var renderers = go.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer is ParticleSystemRenderer || renderer is LineRenderer)
                        continue;

                    if (renderer.sharedMaterial != null)
                    {
                        Material mat = CloneMaterialForObject(renderer.sharedMaterial, objectName, renderer.name);
                        if (mat == null)
                            mat = renderer.sharedMaterial;

                        if (mat.HasProperty("_BaseColor"))
                            mat.SetColor("_BaseColor", newColor);
                        else if (mat.HasProperty("_Color"))
                            mat.SetColor("_Color", newColor);

                        renderer.material = mat;
                    }
                    affected++;
                }
            }

            if (affectLines)
            {
                var lineRenderers = go.GetComponentsInChildren<LineRenderer>(true);
                foreach (var lr in lineRenderers)
                {
                    if (lr.sharedMaterial != null)
                    {
                        Material mat = CloneMaterialForObject(lr.sharedMaterial, objectName, lr.name);
                        if (mat == null)
                            mat = lr.sharedMaterial;

                        if (mat.HasProperty("_BaseColor"))
                            mat.SetColor("_BaseColor", newColor);
                        else if (mat.HasProperty("_Color"))
                            mat.SetColor("_Color", newColor);

                        Color emissionColor = UnityMcpColorUtils.MultiplyColor(newColor, 2f);
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.SetColor("_EmissionColor", emissionColor);
                            mat.EnableKeyword("_EMISSION");
                        }

                        lr.sharedMaterial = mat;
                    }
                    affected++;
                }
            }

            EditorUtility.SetDirty(go);
            return UnityMcpResponseUtils.Success(
                $"Recolored {affected} component(s) on '{objectName}' to {req.color}",
                objectName, null, affected);
        }

        public static string ScaleEffect(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            float scaleMultiplier = Mathf.Clamp((float)req.scaleMultiplier, 0.01f, 100f);

            if (scaleMultiplier <= 0f)
                return UnityMcpResponseUtils.Error("scaleMultiplier must be > 0");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            int affected = 0;

            if (req.scaleTransform)
            {
                go.transform.localScale = Vector3.Scale(go.transform.localScale, Vector3.one * scaleMultiplier);
                affected++;
            }

            if (req.scaleParticleSize && req.affectParticles)
            {
                var particleSystems = go.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particleSystems)
                {
                    var main = ps.main;
                    float curSize = main.startSize.constant;
                    main.startSize = Mathf.Clamp(curSize * scaleMultiplier, 0.001f, 100f);

                    var sizeOverLifetime = ps.sizeOverLifetime;
                    if (sizeOverLifetime.enabled)
                    {
                        var curve = sizeOverLifetime.size;
                        if (curve.mode == ParticleSystemCurveMode.Curve || curve.mode == ParticleSystemCurveMode.TwoCurves)
                        {
                            AnimationCurve animCurve = curve.curve;
                            if (animCurve != null)
                            {
                                var keys = animCurve.keys;
                                for (int i = 0; i < keys.Length; i++)
                                    keys[i].value *= scaleMultiplier;
                                animCurve.keys = keys;
                                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, animCurve);
                            }
                        }
                        else
                        {
                            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(curve.constant * scaleMultiplier);
                        }
                    }
                    affected++;
                }
            }

            if (req.scaleParticleSpeed && req.affectParticles)
            {
                var particleSystems = go.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particleSystems)
                {
                    var main = ps.main;
                    float curSpeed = main.startSpeed.constant;
                    main.startSpeed = Mathf.Clamp(curSpeed * scaleMultiplier, 0f, 1000f);

                    var velocityOverLifetime = ps.velocityOverLifetime;
                    if (velocityOverLifetime.enabled)
                    {
                        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(velocityOverLifetime.x.constant * scaleMultiplier);
                        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(velocityOverLifetime.y.constant * scaleMultiplier);
                        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(velocityOverLifetime.z.constant * scaleMultiplier);
                    }
                    affected++;
                }
            }

            EditorUtility.SetDirty(go);
            return UnityMcpResponseUtils.Success(
                $"Scaled effect '{objectName}' by {scaleMultiplier}x",
                objectName, null, affected);
        }

        public static string AdjustEffectTiming(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            int affected = 0;
            bool hasDuration = req.duration > 0;
            bool hasDurationMul = req.durationMultiplier > 0;
            bool hasSpeedMul = req.speedMultiplier > 0;

            float duration = Mathf.Clamp((float)req.duration, 0.01f, 300f);
            float durationMultiplier = Mathf.Clamp((float)req.durationMultiplier, 0.01f, 100f);
            float speedMultiplier = Mathf.Clamp((float)req.speedMultiplier, 0.01f, 100f);

            var particleSystems = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems)
            {
                var main = ps.main;

                if (hasDuration)
                    main.duration = duration;
                else if (hasDurationMul)
                    main.duration = Mathf.Clamp(main.duration * durationMultiplier, 0.01f, 300f);

                if (hasSpeedMul)
                {
                    float curLifetime = main.startLifetime.constant;
                    main.startLifetime = Mathf.Clamp(curLifetime / speedMultiplier, 0.01f, 300f);

                    float curSpeed = main.startSpeed.constant;
                    main.startSpeed = Mathf.Clamp(curSpeed * speedMultiplier, 0f, 1000f);
                }

                affected++;
            }

            EditorUtility.SetDirty(go);
            return UnityMcpResponseUtils.Success(
                $"Adjusted timing for '{objectName}' ({affected} ParticleSystem(s))",
                objectName, null, affected);
        }

        private static Material CloneMaterialForObject(Material original, string objectName, string partName)
        {
            string path = AssetDatabase.GetAssetPath(original);
            if (string.IsNullOrEmpty(path))
                return null;

            string normalizedPath = path.Replace("\\", "/");
            if (normalizedPath.StartsWith("Assets/AI_Generated/Materials/"))
                return null;

            string sanitizedObjectName = UnityMcpPathUtils.SanitizeFileName(objectName);
            string sanitizedPartName = UnityMcpPathUtils.SanitizeFileName(partName);
            string newMatName = $"{sanitizedObjectName}_{sanitizedPartName}_Recolored";

            string savePath = UnityMcpPathUtils.GetMaterialSavePath(newMatName);
            if (!AssetDatabase.CopyAsset(path, savePath))
                return null;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<Material>(savePath);
        }
    }
}
