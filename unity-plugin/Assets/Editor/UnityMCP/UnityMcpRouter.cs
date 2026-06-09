using System;
using System.Collections.Generic;
using UnityMCP.Tools;
using UnityMCP.Utils;

namespace UnityMCP
{
    public static class UnityMcpRouter
    {
        private static readonly Dictionary<string, Func<RequestModel, string>> _postRoutes;
        private static readonly Dictionary<string, Func<string>> _getRoutes;

        static UnityMcpRouter()
        {
            _postRoutes = new Dictionary<string, Func<RequestModel, string>>
            {
                { "/create-empty", (req) => UnityMcpSceneTools.CreateEmpty(req) },
                { "/set-transform", (req) => UnityMcpSceneTools.SetTransform(req) },
                { "/create-particle-effect", (req) => UnityMcpVfxTools.CreateParticleEffect(req) },
                { "/create-light", (req) => UnityMcpVfxTools.CreateLight(req) },
                { "/save-prefab", (req) => UnityMcpPrefabTools.SavePrefab(req) },
                { "/create-material", (req) => UnityMcpMaterialTools.CreateMaterial(req) },
                { "/assign-material", (req) => UnityMcpMaterialTools.AssignMaterial(req) },
                { "/create-additive-particle-material", (req) => UnityMcpMaterialTools.CreateAdditiveParticleMaterial(req) },
                { "/set-material-color", (req) => UnityMcpMaterialTools.SetMaterialColor(req) },
                { "/set-material-emission", (req) => UnityMcpMaterialTools.SetMaterialEmission(req) },
                { "/create-magic-portal", (req) => UnityMcpVfxTools.CreateMagicPortal(req) },
                { "/create-fire-explosion", (req) => UnityMcpVfxTools.CreateFireExplosion(req) },
                { "/create-lightning-hit", (req) => UnityMcpVfxTools.CreateLightningHit(req) },
                { "/create-heal-aura", (req) => UnityMcpVfxTools.CreateHealAura(req) },
                { "/create-smoke-burst", (req) => UnityMcpVfxTools.CreateSmokeBurst(req) },
                { "/create-slash-trail", (req) => UnityMcpVfxTools.CreateSlashTrail(req) },
                { "/focus-scene-object", (req) => UnityMcpPreviewTools.FocusSceneObject(req) },
                { "/play-effect", (req) => UnityMcpPreviewTools.PlayEffect(req) },
                { "/stop-effect", (req) => UnityMcpPreviewTools.StopEffect(req) },
                { "/capture-view", (req) => UnityMcpPreviewTools.CaptureView(req) },
                { "/create-vfx-from-template", (req) => UnityMcpTemplateTools.CreateVfxFromTemplate(req) },
                { "/instantiate-prefab", (req) => UnityMcpTemplateTools.InstantiatePrefab(req) },
                { "/list-generated-assets", (req) => UnityMcpAssetTools.ListGeneratedAssets(req) },
                { "/clear-ai-generated-scene-objects", (req) => UnityMcpAssetTools.ClearAiGeneratedSceneObjects(req) },
                { "/get-object-info", (req) => UnityMcpAssetTools.GetObjectInfo(req) },
                { "/update-particle-system", (req) => UnityMcpTuningTools.UpdateParticleSystem(req) },
                { "/update-light", (req) => UnityMcpTuningTools.UpdateLight(req) },
                { "/update-line-renderer", (req) => UnityMcpTuningTools.UpdateLineRenderer(req) },
                { "/recolor-effect", (req) => UnityMcpTuningTools.RecolorEffect(req) },
                { "/scale-effect", (req) => UnityMcpTuningTools.ScaleEffect(req) },
                { "/adjust-effect-timing", (req) => UnityMcpTuningTools.AdjustEffectTiming(req) },
                { "/create-effect-variants", (req) => UnityMcpVariantTools.CreateEffectVariants(req) },
                { "/capture-effect-variants", (req) => UnityMcpVariantTools.CaptureEffectVariants(req) },
                { "/list-material-properties", (req) => UnityMcpShaderTools.ListMaterialProperties(req) },
                { "/set-material-property", (req) => UnityMcpShaderTools.SetMaterialProperty(req) },
                { "/set-vfx-graph-property", (req) => UnityMcpShaderTools.SetVfxGraphProperty(req) },
                { "/create-vfx-graph-from-template", (req) => UnityMcpShaderTools.CreateVfxGraphFromTemplate(req) },
                { "/export-effect-report", (req) => UnityMcpReportTools.ExportEffectReport(req) },

                // --- EXTEND_SCENE 新增路由 ---
                { "/create-primitive", (req) => UnityMcpSceneTools.CreatePrimitive(req) },
                { "/create-sample-scene", (req) => UnityMcpSceneTools.CreateSampleScene(req) },
                { "/reset-scene", (req) => UnityMcpSceneTools.ResetScene(req) },
                { "/create-terrain", (req) => UnityMcpTerrainTools.CreateTerrain(req) },
                { "/sculpt-terrain", (req) => UnityMcpTerrainTools.SculptTerrain(req) },
                { "/paint-terrain", (req) => UnityMcpTerrainTools.PaintTerrain(req) },
                { "/set-environment", (req) => UnityMcpEnvironmentTools.SetEnvironment(req) },
                { "/layout-objects", (req) => UnityMcpLayoutTools.LayoutObjects(req) },
                { "/create-camera", (req) => UnityMcpSceneTools.CreateCamera(req) },
                { "/create-test-suite", (req) => UnityMcpSceneTools.CreateTestSuite(req) },
            };

            _getRoutes = new Dictionary<string, Func<string>>
            {
                { "/ping", () => UnityMcpConnectionTools.Ping() },
                { "/list-scene-objects", () => UnityMcpSceneTools.ListSceneObjects() }
            };
        }

        public static string RouteGet(string path)
        {
            if (_getRoutes.TryGetValue(path, out var handler))
            {
                return handler();
            }
            return UnityMcpResponseUtils.Error($"Unknown GET endpoint: {path}");
        }

        public static string RoutePost(string path, RequestModel request)
        {
            if (_postRoutes.TryGetValue(path, out var handler))
            {
                return handler(request);
            }
            return UnityMcpResponseUtils.Error($"Unknown POST endpoint: {path}");
        }
    }
}
