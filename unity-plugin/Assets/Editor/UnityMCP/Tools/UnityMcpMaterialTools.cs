using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpMaterialTools
    {
        private static readonly string[] ShaderFallbackList = new string[]
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Lit",
            "Particles/Standard Unlit",
            "Standard",
            "Unlit/Color"
        };

        public static string CreateMaterial(RequestModel req)
        {
            string materialName = string.IsNullOrEmpty(req.materialName) ? "AI_Material" : req.materialName;
            materialName = UnityMcpPathUtils.SanitizeFileName(materialName);

            Shader shader = FindBestShader(req.shaderName);
            Material mat = new Material(shader);

            Color color = UnityMcpColorUtils.ParseHtmlColor(req.color);
            ApplyMainColor(mat, color);

            float emissionIntensity = Mathf.Clamp((float)req.emissionIntensity, 0f, 20f);
            if (emissionIntensity > 0f)
            {
                Color emissionColor = UnityMcpColorUtils.ParseHtmlColor(req.emissionColor);
                Color hdrEmission = UnityMcpColorUtils.MultiplyColor(emissionColor, emissionIntensity);
                ApplyEmission(mat, hdrEmission);
            }

            string assetPath = UnityMcpPathUtils.GetMaterialSavePath(materialName);
            AssetDatabase.CreateAsset(mat, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return UnityMcpResponseUtils.Success($"Material created: {assetPath}", materialName, assetPath);
        }

        public static string AssignMaterial(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            string materialPath = req.materialPath;
            if (string.IsNullOrEmpty(materialPath))
                return UnityMcpResponseUtils.Error("materialPath is required");

            if (!UnityMcpPathUtils.IsSafeMaterialPath(materialPath))
                return UnityMcpResponseUtils.Error("Invalid material path");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                return UnityMcpResponseUtils.Error($"Material not found at: {materialPath}");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found in scene");

            int affectedCount = 0;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                affectedCount++;
            }
            else
            {
                var childRenderers = go.GetComponentsInChildren<Renderer>();
                foreach (var r in childRenderers)
                {
                    r.sharedMaterial = material;
                    affectedCount++;
                }
            }

            return UnityMcpResponseUtils.Success(
                $"Material assigned to {affectedCount} renderer(s)",
                objectName,
                materialPath,
                affectedCount
            );
        }

        public static string CreateAdditiveParticleMaterial(RequestModel req)
        {
            string materialName = string.IsNullOrEmpty(req.materialName) ? "AI_AdditiveParticle" : req.materialName;
            materialName = UnityMcpPathUtils.SanitizeFileName(materialName);

            Shader shader = FindBestShader("Universal Render Pipeline/Particles/Unlit");
            Material mat = new Material(shader);

            Color color = UnityMcpColorUtils.ParseHtmlColor(req.color);
            ApplyMainColor(mat, color);

            float emissionIntensity = Mathf.Clamp((float)req.emissionIntensity, 0f, 20f);
            Color emissionColor = UnityMcpColorUtils.ParseHtmlColor("#33AAFF");
            Color hdrEmission = UnityMcpColorUtils.MultiplyColor(emissionColor, emissionIntensity);
            ApplyEmission(mat, hdrEmission);

            ConfigureTransparentParticleMaterial(mat);

            string assetPath = UnityMcpPathUtils.GetMaterialSavePath(materialName);
            AssetDatabase.CreateAsset(mat, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return UnityMcpResponseUtils.Success($"Additive particle material created: {assetPath}", materialName, assetPath);
        }

        public static string SetMaterialColor(RequestModel req)
        {
            string materialPath = req.materialPath;
            if (string.IsNullOrEmpty(materialPath))
                return UnityMcpResponseUtils.Error("materialPath is required");

            if (!UnityMcpPathUtils.IsSafeMaterialPath(materialPath))
                return UnityMcpResponseUtils.Error("Invalid material path");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                return UnityMcpResponseUtils.Error($"Material not found at: {materialPath}");

            Color color = UnityMcpColorUtils.ParseHtmlColor(req.color);
            ApplyMainColor(material, color);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return UnityMcpResponseUtils.Success($"Color set on {materialPath}", "", materialPath);
        }

        public static string SetMaterialEmission(RequestModel req)
        {
            string materialPath = req.materialPath;
            if (string.IsNullOrEmpty(materialPath))
                return UnityMcpResponseUtils.Error("materialPath is required");

            if (!UnityMcpPathUtils.IsSafeMaterialPath(materialPath))
                return UnityMcpResponseUtils.Error("Invalid material path");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                return UnityMcpResponseUtils.Error($"Material not found at: {materialPath}");

            float intensity = Mathf.Clamp((float)req.emissionIntensity, 0f, 20f);
            Color emissionColor = UnityMcpColorUtils.ParseHtmlColor(req.emissionColor);
            Color hdrEmission = UnityMcpColorUtils.MultiplyColor(emissionColor, intensity);

            ApplyEmission(material, hdrEmission);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return UnityMcpResponseUtils.Success($"Emission set on {materialPath}", "", materialPath);
        }

        private static Shader FindBestShader(string preferredShader)
        {
            if (!string.IsNullOrEmpty(preferredShader))
            {
                Shader shader = Shader.Find(preferredShader);
                if (shader != null)
                    return shader;
            }

            foreach (string shaderName in ShaderFallbackList)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                    return shader;
            }

            return Shader.Find("Standard");
        }

        private static void ApplyMainColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
        }

        private static void ApplyEmission(Material mat, Color hdrColor)
        {
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", hdrColor);
                mat.EnableKeyword("_EMISSION");
            }
        }

        private static void ConfigureTransparentParticleMaterial(Material mat)
        {
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);

            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 2f);

            if (mat.HasProperty("_ColorMode"))
                mat.SetFloat("_ColorMode", 0f);

            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", 0f);

            if (mat.HasProperty("_AlphaClip"))
                mat.SetFloat("_AlphaClip", 0f);

            string shaderName = mat.shader != null ? mat.shader.name : "";
            if (shaderName.Contains("Standard") && mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 2f);
            }
        }
    }
}
