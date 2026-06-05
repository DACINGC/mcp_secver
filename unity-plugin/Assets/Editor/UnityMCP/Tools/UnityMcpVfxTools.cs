using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpVfxTools
    {
        public static string CreateParticleEffect(RequestModel req)
        {
            string effectName = string.IsNullOrEmpty(req.effectName) ? "ParticleEffect" : req.effectName;
            effectName = UnityMcpPathUtils.SanitizeFileName(effectName);

            GameObject root = new GameObject(effectName);
            ParticleSystem ps = root.AddComponent<ParticleSystem>();

            float duration = Mathf.Clamp((float)req.duration, 0.01f, 300f);
            float emissionRate = Mathf.Clamp((float)req.emissionRate, 0f, 100000f);
            float startLifetime = Mathf.Clamp((float)req.startLifetime, 0.01f, 300f);
            float startSpeed = Mathf.Clamp((float)req.startSpeed, 0f, 1000f);
            float startSize = Mathf.Clamp((float)req.startSize, 0.001f, 100f);
            float radius = Mathf.Clamp((float)req.radius, 0f, 100f);
            bool loop = req.loop;
            Color color = UnityMcpColorUtils.ParseHtmlColor(req.color);

            var main = ps.main;
            main.duration = duration;
            main.startLifetime = startLifetime;
            main.startSpeed = startSpeed;
            main.startSize = startSize;
            main.loop = loop;
            main.startColor = color;

            var emission = ps.emission;
            emission.rateOverTime = emissionRate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

            Undo.RegisterCreatedObjectUndo(root, "Create Particle Effect");
            return UnityMcpResponseUtils.Success($"Created particle effect {effectName}", effectName);
        }

        public static string CreateLight(RequestModel req)
        {
            string name = string.IsNullOrEmpty(req.name) ? "PointLight" : req.name;
            name = UnityMcpPathUtils.SanitizeFileName(name);

            GameObject go = new GameObject(name);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;

            Color color = UnityMcpColorUtils.ParseHtmlColor(req.color);
            light.color = color;

            light.intensity = Mathf.Clamp((float)req.intensity, 0f, 100000f);
            light.range = Mathf.Clamp((float)req.range, 0.01f, 1000f);

            float x = Mathf.Clamp((float)req.x, -10000f, 10000f);
            float y = Mathf.Clamp((float)req.y, -10000f, 10000f);
            float z = Mathf.Clamp((float)req.z, -10000f, 10000f);
            go.transform.position = new Vector3(x, y, z);

            Undo.RegisterCreatedObjectUndo(go, "Create Light");
            return UnityMcpResponseUtils.Success($"Created light {name}", name);
        }

        public static string CreateMagicPortal(RequestModel req)
        {
            string effectName = Sanitize(req.effectName, "MagicPortal");
            float radius = Mathf.Clamp((float)req.radius, 0.2f, 10f);
            float duration = Mathf.Clamp((float)req.duration, 0.5f, 30f);
            bool loop = req.loop;
            Color mainColor = UnityMcpColorUtils.ParseHtmlColor(string.IsNullOrEmpty(req.mainColor) ? "#33AAFF" : req.mainColor);

            GameObject root = new GameObject(effectName);

            ParticleSystem ringPs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Portal_Ring_Particles");
            UnityMcpVfxUtils.ConfigureMain(ringPs, duration, 2.0f, 0.5f, 0.15f, loop);
            UnityMcpVfxUtils.ConfigureMainColor(ringPs, mainColor);
            UnityMcpVfxUtils.ConfigureEmissionRate(ringPs, 120f);
            UnityMcpVfxUtils.ConfigureShapeCircle(ringPs, radius);

            var ringGradient = new Gradient();
            ringGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(mainColor, 0f), new GradientColorKey(mainColor, 0.5f), new GradientColorKey(mainColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(ringPs, ringGradient);

            var ringSizeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.3f));
            UnityMcpVfxUtils.ConfigureSizeOverLifetime(ringPs, ringSizeCurve);

            Material ringMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Ring_Mat", mainColor, true, UnityMcpColorUtils.MultiplyColor(mainColor, 3f));
            UnityMcpVfxUtils.SetParticleMaterial(ringPs, ringMat);
            ringPs.transform.localPosition = Vector3.zero;

            ParticleSystem corePs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Portal_Core_Particles");
            UnityMcpVfxUtils.ConfigureMain(corePs, duration, 2.5f, 0.3f, 0.5f, loop);
            UnityMcpVfxUtils.ConfigureMainColor(corePs, Color.Lerp(mainColor, Color.white, 0.3f));
            UnityMcpVfxUtils.ConfigureEmissionRate(corePs, 40f);
            UnityMcpVfxUtils.ConfigureShapeSphere(corePs, radius * 0.5f);

            var coreGradient = new Gradient();
            coreGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.Lerp(mainColor, Color.white, 0.5f), 0f), new GradientColorKey(mainColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(corePs, coreGradient);

            Material coreMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Core_Mat", Color.Lerp(mainColor, Color.white, 0.3f), true, UnityMcpColorUtils.MultiplyColor(mainColor, 4f));
            UnityMcpVfxUtils.SetParticleMaterial(corePs, coreMat);

            ParticleSystem sparkPs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Portal_Spark_Particles");
            UnityMcpVfxUtils.ConfigureMain(sparkPs, duration, 0.8f, 3f, 0.06f, loop);
            UnityMcpVfxUtils.ConfigureMainColor(sparkPs, Color.white);
            UnityMcpVfxUtils.ConfigureEmissionRate(sparkPs, 40f);
            UnityMcpVfxUtils.ConfigureShapeCircle(sparkPs, radius * 1.1f);

            var sparkGradient = new Gradient();
            sparkGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(mainColor, 0.5f), new GradientColorKey(mainColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(sparkPs, sparkGradient);

            Material sparkMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Spark_Mat", mainColor, true, UnityMcpColorUtils.MultiplyColor(mainColor, 5f));
            UnityMcpVfxUtils.SetParticleMaterial(sparkPs, sparkMat);

            UnityMcpVfxUtils.CreateLineRendererCircle(root.transform, "Portal_Rotating_Ring", radius, 0.08f, mainColor);

            GameObject lightGo = UnityMcpVfxUtils.CreatePointLight(root.transform, "Portal_Light", mainColor, Mathf.Clamp(5f, 2f, 8f), radius * 3f);
            lightGo.transform.localPosition = Vector3.zero;

            Undo.RegisterCreatedObjectUndo(root, "Create Magic Portal");

            if (req.saveAsPrefab)
                UnityMcpVfxUtils.SaveAsPrefab(root, effectName);

            return UnityMcpResponseUtils.Success($"Created magic portal {effectName}", effectName);
        }

        public static string CreateFireExplosion(RequestModel req)
        {
            string effectName = Sanitize(req.effectName, "FireExplosion");
            float radius = Mathf.Clamp((float)req.radius, 0.2f, 20f);
            float intensity = Mathf.Clamp((float)req.intensity, 0.1f, 5f);
            float duration = Mathf.Clamp((float)req.duration, 0.2f, 10f);

            GameObject root = new GameObject(effectName);

            Color fireColor = new Color(1f, 0.5f, 0.1f);
            Color brightFire = new Color(1f, 0.8f, 0.2f);
            Color darkFire = new Color(0.8f, 0.2f, 0f);

            ParticleSystem firePs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Fire_Burst");
            UnityMcpVfxUtils.ConfigureMain(firePs, duration, 0.6f, radius * intensity * 1.5f, 0.4f, false);
            UnityMcpVfxUtils.ConfigureMainColor(firePs, fireColor);
            UnityMcpVfxUtils.ConfigureBurst(firePs, Mathf.RoundToInt(60 * intensity));
            UnityMcpVfxUtils.ConfigureShapeSphere(firePs, radius * 0.3f);

            var fireGradient = new Gradient();
            fireGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(brightFire, 0f), new GradientColorKey(fireColor, 0.4f), new GradientColorKey(darkFire, 0.8f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(firePs, fireGradient);

            var fireSizeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.3f, 1.5f), new Keyframe(1f, 0f));
            UnityMcpVfxUtils.ConfigureSizeOverLifetime(firePs, fireSizeCurve);

            Material fireMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Fire_Mat", fireColor, true, UnityMcpColorUtils.MultiplyColor(brightFire, 3f));
            UnityMcpVfxUtils.SetParticleMaterial(firePs, fireMat);

            ParticleSystem smokePs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Smoke_Burst");
            UnityMcpVfxUtils.ConfigureMain(smokePs, duration * 1.2f, 1.5f, radius * intensity * 0.5f, 0.8f, false);
            UnityMcpVfxUtils.ConfigureMainColor(smokePs, new Color(0.3f, 0.3f, 0.3f));
            UnityMcpVfxUtils.ConfigureBurst(smokePs, Mathf.RoundToInt(20 * intensity));

            var smokeGradient = new Gradient();
            smokeGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.4f, 0.35f, 0.3f), 0f), new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 0.5f), new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0.3f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(smokePs, smokeGradient);

            var smokeSizeCurve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.5f, 1.5f), new Keyframe(1f, 2.5f));
            UnityMcpVfxUtils.ConfigureSizeOverLifetime(smokePs, smokeSizeCurve);

            UnityMcpVfxUtils.ConfigureVelocityOverLifetime(smokePs, new Vector3(0f, 0.5f, 0f));
            UnityMcpVfxUtils.ConfigureNoise(smokePs, 0.5f, 0.3f);

            Material smokeMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Smoke_Mat", new Color(0.3f, 0.3f, 0.3f));
            UnityMcpVfxUtils.SetParticleMaterial(smokePs, smokeMat);

            ParticleSystem sparkPs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Sparks");
            UnityMcpVfxUtils.ConfigureMain(sparkPs, duration * 0.8f, 0.4f, radius * intensity * 3f, 0.08f, false);
            UnityMcpVfxUtils.ConfigureMainColor(sparkPs, brightFire);
            UnityMcpVfxUtils.ConfigureBurst(sparkPs, Mathf.RoundToInt(30 * intensity));
            UnityMcpVfxUtils.ConfigureShapeSphere(sparkPs, radius * 0.5f);

            var sparkFade = new Gradient();
            sparkFade.SetKeys(
                new GradientColorKey[] { new GradientColorKey(brightFire, 0f), new GradientColorKey(Color.yellow, 0.3f), new GradientColorKey(darkFire, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(sparkPs, sparkFade);

            UnityMcpVfxUtils.ConfigureTrails(sparkPs, 0.1f, Color.yellow);

            Material sparkMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Spark_Mat", brightFire, true, UnityMcpColorUtils.MultiplyColor(Color.yellow, 5f));
            UnityMcpVfxUtils.SetParticleMaterial(sparkPs, sparkMat);

            Color flashColor = new Color(1f, 0.7f, 0.2f);
            GameObject lightGo = UnityMcpVfxUtils.CreatePointLight(root.transform, "Flash_Light", flashColor,
                Mathf.Clamp(8f * intensity, 0f, 20f), radius * 4f);
            lightGo.transform.localPosition = Vector3.zero;

            Undo.RegisterCreatedObjectUndo(root, "Create Fire Explosion");

            if (req.saveAsPrefab)
                UnityMcpVfxUtils.SaveAsPrefab(root, effectName);

            return UnityMcpResponseUtils.Success($"Created fire explosion {effectName}", effectName);
        }

        public static string CreateLightningHit(RequestModel req)
        {
            string effectName = Sanitize(req.effectName, "LightningHit");
            Color mainColor = UnityMcpColorUtils.ParseHtmlColor(string.IsNullOrEmpty(req.mainColor) ? "#AA33FF" : req.mainColor);
            float height = Mathf.Clamp((float)req.height, 0.5f, 20f);
            float radius = Mathf.Clamp((float)req.radius, 0.1f, 10f);
            float duration = Mathf.Clamp((float)req.duration, 0.1f, 5f);
            int branchCount = Mathf.Clamp(req.branchCount, 1, 20);

            GameObject root = new GameObject(effectName);

            System.Random rng = new System.Random(12345);

            Vector3 startPos = new Vector3(0f, height, 0f);
            Vector3 endPos = new Vector3(0f, 0f, 0f);

            List<Vector3> mainPoints = UnityMcpVfxUtils.GenerateLightningPoints(startPos, endPos, radius * 0.5f, 8 + rng.Next(4), rng);
            float boltWidth = Mathf.Clamp(0.12f, 0.05f, 0.25f);
            UnityMcpVfxUtils.CreateLightningBolt(root.transform, "Lightning_Main_Bolt", mainPoints, boltWidth, mainColor);

            for (int i = 0; i < branchCount; i++)
            {
                int branchIndex = 2 + rng.Next(Mathf.Max(1, mainPoints.Count - 4));
                Vector3 branchStart = mainPoints[branchIndex];
                Vector3 branchDir = new Vector3(
                    (float)(rng.NextDouble() * 2.0 - 1.0),
                    (float)(rng.NextDouble() * -0.5f - 0.2f),
                    (float)(rng.NextDouble() * 2.0 - 1.0)
                ).normalized;
                float branchLength = (float)(rng.NextDouble() * 0.5 + 0.2f) * radius;
                Vector3 branchEnd = branchStart + branchDir * branchLength;

                int branchPointsCount = 3 + rng.Next(3);
                List<Vector3> branchPts = UnityMcpVfxUtils.GenerateLightningPoints(branchStart, branchEnd, radius * 0.15f, branchPointsCount, rng);
                UnityMcpVfxUtils.CreateLightningBolt(root.transform, $"Lightning_Branch_{i}", branchPts, boltWidth * 0.5f, Color.Lerp(mainColor, Color.white, 0.3f));
            }

            ParticleSystem sparkPs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Impact_Sparks");
            UnityMcpVfxUtils.ConfigureMain(sparkPs, duration * 0.5f, 0.4f, radius * 5f, 0.06f, false);
            UnityMcpVfxUtils.ConfigureMainColor(sparkPs, mainColor);
            UnityMcpVfxUtils.ConfigureBurst(sparkPs, 40);
            UnityMcpVfxUtils.ConfigureShapeSphere(sparkPs, radius * 0.3f);
            sparkPs.transform.localPosition = endPos;

            var sparkGrad = new Gradient();
            sparkGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(mainColor, 0.5f), new GradientColorKey(mainColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(sparkPs, sparkGrad);

            Material sparkMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Spark_Mat", mainColor, true, UnityMcpColorUtils.MultiplyColor(mainColor, 5f));
            UnityMcpVfxUtils.SetParticleMaterial(sparkPs, sparkMat);

            GameObject lightGo = UnityMcpVfxUtils.CreatePointLight(root.transform, "Lightning_Light", mainColor,
                Mathf.Clamp(8f, 5f, 12f), radius * 4f);
            lightGo.transform.localPosition = endPos;

            Undo.RegisterCreatedObjectUndo(root, "Create Lightning Hit");

            if (req.saveAsPrefab)
                UnityMcpVfxUtils.SaveAsPrefab(root, effectName);

            return UnityMcpResponseUtils.Success($"Created lightning hit {effectName}", effectName);
        }

        public static string CreateHealAura(RequestModel req)
        {
            string effectName = Sanitize(req.effectName, "HealAura");
            Color mainColor = UnityMcpColorUtils.ParseHtmlColor(string.IsNullOrEmpty(req.mainColor) ? "#55FF88" : req.mainColor);
            float radius = Mathf.Clamp((float)req.radius, 0.2f, 10f);
            float duration = Mathf.Clamp((float)req.duration, 0.5f, 30f);
            bool loop = req.loop;

            GameObject root = new GameObject(effectName);

            UnityMcpVfxUtils.CreateLineRendererCircle(root.transform, "Aura_Ring", radius, 0.05f, mainColor, 32, 0.05f);

            ParticleSystem risingPs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Rising_Particles");
            UnityMcpVfxUtils.ConfigureMain(risingPs, duration, 2.0f, 0.5f, 0.15f, loop);
            UnityMcpVfxUtils.ConfigureMainColor(risingPs, mainColor);
            UnityMcpVfxUtils.ConfigureEmissionRate(risingPs, 30f);
            UnityMcpVfxUtils.ConfigureShapeCircle(risingPs, radius * 0.8f);
            UnityMcpVfxUtils.ConfigureVelocityOverLifetime(risingPs, new Vector3(0f, 1.0f, 0f));

            var risingGrad = new Gradient();
            risingGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(mainColor, 0f), new GradientColorKey(Color.Lerp(mainColor, Color.white, 0.5f), 0.5f), new GradientColorKey(mainColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.4f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(risingPs, risingGrad);

            var risingSize = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.5f, 1f), new Keyframe(1f, 1.5f));
            UnityMcpVfxUtils.ConfigureSizeOverLifetime(risingPs, risingSize);

            Material risingMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Rising_Mat", mainColor, true, UnityMcpColorUtils.MultiplyColor(mainColor, 2f));
            UnityMcpVfxUtils.SetParticleMaterial(risingPs, risingMat);

            ParticleSystem sparklePs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Healing_Sparkles");
            UnityMcpVfxUtils.ConfigureMain(sparklePs, duration, 1.0f, 0.3f, 0.05f, loop);
            UnityMcpVfxUtils.ConfigureMainColor(sparklePs, Color.Lerp(mainColor, Color.yellow, 0.3f));
            UnityMcpVfxUtils.ConfigureEmissionRate(sparklePs, 15f);
            UnityMcpVfxUtils.ConfigureShapeSphere(sparklePs, radius * 0.5f);

            var sparkleGrad = new Gradient();
            sparkleGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.Lerp(mainColor, Color.yellow, 0.3f), 0.5f), new GradientColorKey(mainColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(sparklePs, sparkleGrad);

            Material sparkleMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Sparkle_Mat", Color.Lerp(mainColor, Color.yellow, 0.3f), true, UnityMcpColorUtils.MultiplyColor(mainColor, 3f));
            UnityMcpVfxUtils.SetParticleMaterial(sparklePs, sparkleMat);

            GameObject lightGo = UnityMcpVfxUtils.CreatePointLight(root.transform, "Aura_Light", mainColor,
                Mathf.Clamp(2.5f, 1f, 4f), radius * 3f);
            lightGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            Undo.RegisterCreatedObjectUndo(root, "Create Heal Aura");

            if (req.saveAsPrefab)
                UnityMcpVfxUtils.SaveAsPrefab(root, effectName);

            return UnityMcpResponseUtils.Success($"Created heal aura {effectName}", effectName);
        }

        public static string CreateSmokeBurst(RequestModel req)
        {
            string effectName = Sanitize(req.effectName, "SmokeBurst");
            Color color = UnityMcpColorUtils.ParseHtmlColor(string.IsNullOrEmpty(req.color) ? "#777777" : req.color);
            float radius = Mathf.Clamp((float)req.radius, 0.2f, 20f);
            float duration = Mathf.Clamp((float)req.duration, 0.5f, 20f);
            float density = Mathf.Clamp((float)req.density, 0.1f, 5f);

            GameObject root = new GameObject(effectName);

            ParticleSystem mainPs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Smoke_Main");
            UnityMcpVfxUtils.ConfigureMain(mainPs, duration, 2.5f, radius * 0.5f, 0.8f, false);
            UnityMcpVfxUtils.ConfigureMainColor(mainPs, color);
            UnityMcpVfxUtils.ConfigureBurst(mainPs, Mathf.RoundToInt(30 * density));
            UnityMcpVfxUtils.ConfigureShapeSphere(mainPs, radius * 0.3f);

            var mainGrad = new Gradient();
            mainGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.Lerp(color, Color.gray, 0.3f), 0.5f), new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.4f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(mainPs, mainGrad);

            var mainSize = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.5f, 1.5f), new Keyframe(1f, 2.5f));
            UnityMcpVfxUtils.ConfigureSizeOverLifetime(mainPs, mainSize);

            UnityMcpVfxUtils.ConfigureVelocityOverLifetime(mainPs, new Vector3(0f, 0.3f, 0f));
            UnityMcpVfxUtils.ConfigureNoise(mainPs, 0.8f, 0.4f);

            Material mainMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Main_Mat", color);
            UnityMcpVfxUtils.SetParticleMaterial(mainPs, mainMat);

            ParticleSystem driftPs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Smoke_Drift");
            UnityMcpVfxUtils.ConfigureMain(driftPs, duration * 1.3f, 3f, 0.2f, 0.5f, false);
            UnityMcpVfxUtils.ConfigureMainColor(driftPs, Color.Lerp(color, Color.gray, 0.5f));
            UnityMcpVfxUtils.ConfigureBurst(driftPs, Mathf.RoundToInt(15 * density));

            var driftGrad = new Gradient();
            driftGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.Lerp(color, Color.gray, 0.5f), 0f), new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(driftPs, driftGrad);

            var driftSize = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 3f));
            UnityMcpVfxUtils.ConfigureSizeOverLifetime(driftPs, driftSize);

            UnityMcpVfxUtils.ConfigureVelocityOverLifetime(driftPs, new Vector3(0.2f, 0.5f, 0.1f));
            UnityMcpVfxUtils.ConfigureNoise(driftPs, 1.0f, 0.2f);

            Material driftMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Drift_Mat", Color.Lerp(color, Color.gray, 0.5f));
            UnityMcpVfxUtils.SetParticleMaterial(driftPs, driftMat);

            ParticleSystem dustPs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Dust_Ring");
            UnityMcpVfxUtils.ConfigureMain(dustPs, duration * 0.8f, 1.0f, 0.3f, 0.1f, false);
            UnityMcpVfxUtils.ConfigureMainColor(dustPs, Color.Lerp(color, Color.gray, 0.7f));
            UnityMcpVfxUtils.ConfigureBurst(dustPs, Mathf.RoundToInt(20 * density));
            UnityMcpVfxUtils.ConfigureShapeCircle(dustPs, radius);

            var dustGrad = new Gradient();
            dustGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.Lerp(color, Color.gray, 0.7f), 0f), new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(dustPs, dustGrad);

            Material dustMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Dust_Mat", Color.Lerp(color, Color.gray, 0.7f));
            UnityMcpVfxUtils.SetParticleMaterial(dustPs, dustMat);

            dustPs.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            Undo.RegisterCreatedObjectUndo(root, "Create Smoke Burst");

            if (req.saveAsPrefab)
                UnityMcpVfxUtils.SaveAsPrefab(root, effectName);

            return UnityMcpResponseUtils.Success($"Created smoke burst {effectName}", effectName);
        }

        public static string CreateSlashTrail(RequestModel req)
        {
            string effectName = Sanitize(req.effectName, "SlashTrail");
            Color mainColor = UnityMcpColorUtils.ParseHtmlColor(string.IsNullOrEmpty(req.mainColor) ? "#66CCFF" : req.mainColor);
            float length = Mathf.Clamp((float)req.length, 0.5f, 20f);
            float width = Mathf.Clamp((float)req.width, 0.02f, 3f);
            float duration = Mathf.Clamp((float)req.duration, 0.1f, 5f);

            GameObject root = new GameObject(effectName);

            GameObject arcGo = UnityMcpVfxUtils.CreateChild(root.transform, "Slash_Arc");
            LineRenderer lr = arcGo.AddComponent<LineRenderer>();

            Material arcMat = UnityMcpVfxUtils.CreateLineRendererMaterial(effectName + "_Arc_Mat", mainColor);
            lr.sharedMaterial = arcMat;
            lr.startWidth = width;
            lr.endWidth = width * 0.1f;

            int segments = 16;
            lr.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = (t - 0.5f) * Mathf.PI;
                float x = Mathf.Sin(angle) * length * 0.5f;
                float y = (Mathf.Cos(angle) - 1f) * length * 0.15f;
                float z = 0f;
                lr.SetPosition(i, new Vector3(x, y + length * 0.05f, z));
            }

            ParticleSystem sparkPs = UnityMcpVfxUtils.CreateParticleChild(root.transform, "Slash_Sparks");
            UnityMcpVfxUtils.ConfigureMain(sparkPs, duration * 0.6f, 0.3f, length * 2f, 0.04f, false);
            UnityMcpVfxUtils.ConfigureMainColor(sparkPs, mainColor);
            UnityMcpVfxUtils.ConfigureBurst(sparkPs, 20);

            var sparkGrad = new Gradient();
            sparkGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(mainColor, 0.3f), new GradientColorKey(mainColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            UnityMcpVfxUtils.ConfigureColorOverLifetime(sparkPs, sparkGrad);

            Material sparkMat = UnityMcpVfxUtils.CreateGeneratedMaterial(effectName + "_Spark_Mat", mainColor, true, UnityMcpColorUtils.MultiplyColor(mainColor, 4f));
            UnityMcpVfxUtils.SetParticleMaterial(sparkPs, sparkMat);

            GameObject lightGo = UnityMcpVfxUtils.CreatePointLight(root.transform, "Slash_Light", mainColor,
                Mathf.Clamp(6f, 2f, 12f), length * 2f);
            lightGo.transform.localPosition = new Vector3(0f, length * 0.3f, 0f);

            Undo.RegisterCreatedObjectUndo(root, "Create Slash Trail");

            if (req.saveAsPrefab)
                UnityMcpVfxUtils.SaveAsPrefab(root, effectName);

            return UnityMcpResponseUtils.Success($"Created slash trail {effectName}", effectName);
        }

        private static string Sanitize(string name, string fallback)
        {
            string result = string.IsNullOrEmpty(name) ? fallback : name;
            return UnityMcpPathUtils.SanitizeFileName(result);
        }
    }
}
