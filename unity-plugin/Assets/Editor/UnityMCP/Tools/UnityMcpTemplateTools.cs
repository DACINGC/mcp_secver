using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpTemplateTools
    {
        public static string CreateVfxFromTemplate(RequestModel req)
        {
            string templatePath = req.templatePath;
            if (string.IsNullOrEmpty(templatePath))
                return UnityMcpResponseUtils.Error("templatePath is required");

            if (!UnityMcpPathUtils.IsSafeTemplatePrefabPath(templatePath))
                return UnityMcpResponseUtils.Error("templatePath is not allowed. Must be under Assets/VFX/Templates/ or Assets/AI_Generated/Prefabs/");

            GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
            if (template == null)
                return UnityMcpResponseUtils.Error($"Template not found at: {templatePath}");

            GameObject instance = PrefabUtility.InstantiatePrefab(template) as GameObject;
            if (instance == null)
                return UnityMcpResponseUtils.Error("Failed to instantiate template");

            string outputName = string.IsNullOrEmpty(req.outputName) ? template.name + "_Copy" : req.outputName;
            outputName = UnityMcpPathUtils.SanitizeFileName(outputName);
            instance.name = outputName;

            float x = Mathf.Clamp((float)req.x, -10000f, 10000f);
            float y = Mathf.Clamp((float)req.y, -10000f, 10000f);
            float z = Mathf.Clamp((float)req.z, -10000f, 10000f);
            float scale = Mathf.Clamp((float)req.scale, 0.01f, 100f);
            instance.transform.position = new Vector3(x, y, z);
            instance.transform.localScale = new Vector3(scale, scale, scale);

            string mainColor = req.mainColor;
            if (!string.IsNullOrEmpty(mainColor))
            {
                Color color = UnityMcpColorUtils.ParseHtmlColor(mainColor);

                var allParticleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in allParticleSystems)
                {
                    var main = ps.main;
                    main.startColor = color;
                }

                var allLights = instance.GetComponentsInChildren<Light>(true);
                foreach (var light in allLights)
                {
                    light.color = color;
                }

                var allRenderers = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in allRenderers)
                {
                    if (renderer.sharedMaterial != null)
                    {
                        string matName = $"{outputName}_{renderer.name}_Mat";
                        string matPath = UnityMcpPathUtils.GetMaterialSavePath(matName);
                        if (!AssetDatabase.LoadAssetAtPath<Material>(matPath))
                        {
                            Material newMat = new Material(renderer.sharedMaterial);
                            if (newMat.HasProperty("_BaseColor"))
                                newMat.SetColor("_BaseColor", color);
                            else if (newMat.HasProperty("_Color"))
                                newMat.SetColor("_Color", color);

                            AssetDatabase.CreateAsset(newMat, matPath);
                            AssetDatabase.SaveAssets();
                            renderer.sharedMaterial = newMat;
                        }
                    }
                }
            }

            string savedAssetPath = null;
            if (req.saveAsPrefab)
            {
                savedAssetPath = UnityMcpPathUtils.GetPrefabSavePath(outputName);
                PrefabUtility.SaveAsPrefabAsset(instance, savedAssetPath, out bool success);
                if (!success)
                {
                    Object.DestroyImmediate(instance);
                    return UnityMcpResponseUtils.Error("Failed to save prefab");
                }
                AssetDatabase.Refresh();
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create VFX From Template");
            return UnityMcpResponseUtils.Success(
                $"Created {outputName} from template", outputName, savedAssetPath);
        }

        public static string InstantiatePrefab(RequestModel req)
        {
            string prefabPath = req.prefabPath;
            if (string.IsNullOrEmpty(prefabPath))
                return UnityMcpResponseUtils.Error("prefabPath is required");

            if (!UnityMcpPathUtils.IsSafeReadablePrefabPath(prefabPath))
                return UnityMcpResponseUtils.Error("Invalid prefab path");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return UnityMcpResponseUtils.Error($"Prefab not found at: {prefabPath}");

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                return UnityMcpResponseUtils.Error("Failed to instantiate prefab");

            string objectName = string.IsNullOrEmpty(req.objectName) ? prefab.name : req.objectName;
            instance.name = UnityMcpPathUtils.SanitizeFileName(objectName);

            float x = Mathf.Clamp((float)req.x, -10000f, 10000f);
            float y = Mathf.Clamp((float)req.y, -10000f, 10000f);
            float z = Mathf.Clamp((float)req.z, -10000f, 10000f);
            float scale = Mathf.Clamp((float)req.scale, 0.01f, 100f);
            instance.transform.position = new Vector3(x, y, z);
            instance.transform.localScale = new Vector3(scale, scale, scale);

            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
            return UnityMcpResponseUtils.Success(
                $"Instantiated {prefabPath}", instance.name, prefabPath);
        }
    }
}
