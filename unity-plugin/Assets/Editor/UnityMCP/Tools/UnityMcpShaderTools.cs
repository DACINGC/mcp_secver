using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpShaderTools
    {
        public static string ListMaterialProperties(RequestModel req)
        {
            string objectName = req.objectName;
            string materialPath = req.materialPath;

            Material mat = null;

            if (!string.IsNullOrEmpty(materialPath))
            {
                if (!UnityMcpPathUtils.IsSafeReadableAssetPath(materialPath))
                    return UnityMcpResponseUtils.Error($"Invalid material path: {materialPath}");

                mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (mat == null)
                    return UnityMcpResponseUtils.Error($"Material not found at path: {materialPath}");
            }
            else if (!string.IsNullOrEmpty(objectName))
            {
                GameObject go = GameObject.Find(objectName);
                if (go == null)
                    return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

                var renderer = go.GetComponent<Renderer>();
                if (renderer == null)
                    return UnityMcpResponseUtils.Error($"No Renderer found on '{objectName}'");

                mat = renderer.sharedMaterial;
                if (mat == null)
                    return UnityMcpResponseUtils.Error($"No shared material found on '{objectName}' renderer");
            }
            else
            {
                return UnityMcpResponseUtils.Error("Either objectName or materialPath is required");
            }

            var properties = new List<MaterialPropertyInfo>();
            SerializedObject serializedMat = new SerializedObject(mat);
            SerializedProperty savedProps = serializedMat.FindProperty("m_SavedProperties");

            if (savedProps != null)
            {
                SerializedProperty texEnvs = savedProps.FindPropertyRelative("m_TexEnvs");
                if (texEnvs != null && texEnvs.isArray)
                {
                    for (int i = 0; i < texEnvs.arraySize; i++)
                    {
                        var prop = texEnvs.GetArrayElementAtIndex(i);
                        string propName = prop.FindPropertyRelative("first").stringValue;
                        var texProp = prop.FindPropertyRelative("second");
                        var tex = texProp.FindPropertyRelative("m_Texture");
                        var texObj = tex != null ? tex.objectReferenceValue : null;
                        properties.Add(new MaterialPropertyInfo
                        {
                            name = propName,
                            displayName = propName,
                            type = "Texture",
                            currentValueString = texObj != null ? texObj.name : "None"
                        });
                    }
                }

                SerializedProperty floats = savedProps.FindPropertyRelative("m_Floats");
                if (floats != null && floats.isArray)
                {
                    for (int i = 0; i < floats.arraySize; i++)
                    {
                        var prop = floats.GetArrayElementAtIndex(i);
                        string propName = prop.FindPropertyRelative("first").stringValue;
                        float value = prop.FindPropertyRelative("second").floatValue;
                        properties.Add(new MaterialPropertyInfo
                        {
                            name = propName,
                            displayName = propName,
                            type = "Float",
                            currentValueString = value.ToString("G")
                        });
                    }
                }

                SerializedProperty colors = savedProps.FindPropertyRelative("m_Colors");
                if (colors != null && colors.isArray)
                {
                    for (int i = 0; i < colors.arraySize; i++)
                    {
                        var prop = colors.GetArrayElementAtIndex(i);
                        string propName = prop.FindPropertyRelative("first").stringValue;
                        var colorProp = prop.FindPropertyRelative("second");
                        Color colorValue = colorProp.colorValue;
                        string colorStr = $"#{ColorUtility.ToHtmlStringRGB(colorValue)}";
                        properties.Add(new MaterialPropertyInfo
                        {
                            name = propName,
                            displayName = propName,
                            type = "Color",
                            currentValueString = colorStr
                        });
                    }
                }
            }

            if (properties.Count == 0)
            {
                string[] texNames = mat.GetTexturePropertyNames();
                foreach (var pn in texNames)
                {
                    properties.Add(new MaterialPropertyInfo
                    {
                        name = pn,
                        displayName = pn,
                        type = "Texture",
                        currentValueString = mat.GetTexture(pn)?.name ?? "None"
                    });
                }

                Shader shader = mat.shader;
                if (shader != null)
                {
                    int propCount = shader.GetPropertyCount();
                    var seen = new HashSet<string>(texNames);
                    for (int i = 0; i < propCount; i++)
                    {
                        string propName = shader.GetPropertyName(i);
                        if (seen.Contains(propName)) continue;
                        seen.Add(propName);

                        ShaderPropertyType shaderType = shader.GetPropertyType(i);
                        string typeStr = shaderType.ToString();

                        if (shaderType == ShaderPropertyType.Float || shaderType == ShaderPropertyType.Range)
                        {
                            properties.Add(new MaterialPropertyInfo
                            {
                                name = propName,
                                displayName = propName,
                                type = typeStr,
                                currentValueString = mat.GetFloat(propName).ToString("G")
                            });
                        }
                        else if (shaderType == ShaderPropertyType.Color)
                        {
                            Color col = mat.HasProperty(propName) ? mat.GetColor(propName) : Color.white;
                            properties.Add(new MaterialPropertyInfo
                            {
                                name = propName,
                                displayName = propName,
                                type = "Color",
                                currentValueString = $"#{ColorUtility.ToHtmlStringRGB(col)}"
                            });
                        }
                        else if (shaderType == ShaderPropertyType.Vector)
                        {
                            Vector4 vec = mat.HasProperty(propName) ? mat.GetVector(propName) : Vector4.zero;
                            properties.Add(new MaterialPropertyInfo
                            {
                                name = propName,
                                displayName = propName,
                                type = "Vector",
                                currentValueString = vec.ToString("G")
                            });
                        }
                    }
                }
            }

            var response = new ResponseModel
            {
                success = true,
                message = $"Found {properties.Count} properties on material '{mat.name}'",
                materialProperties = properties
            };
            return JsonUtility.ToJson(response);
        }

        public static string SetMaterialProperty(RequestModel req)
        {
            string objectName = req.objectName;
            string materialPath = req.materialPath;
            string propertyName = req.propertyName;
            string propertyType = req.propertyType;
            string value = req.value;

            if (string.IsNullOrEmpty(propertyName))
                return UnityMcpResponseUtils.Error("propertyName is required");
            if (string.IsNullOrEmpty(propertyType))
                return UnityMcpResponseUtils.Error("propertyType is required");

            Material mat = null;

            if (!string.IsNullOrEmpty(materialPath))
            {
                if (!UnityMcpPathUtils.IsSafeReadableAssetPath(materialPath))
                    return UnityMcpResponseUtils.Error($"Invalid material path: {materialPath}");

                mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (mat == null)
                    return UnityMcpResponseUtils.Error($"Material not found at path: {materialPath}");
            }
            else if (!string.IsNullOrEmpty(objectName))
            {
                GameObject go = GameObject.Find(objectName);
                if (go == null)
                    return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

                var renderer = go.GetComponent<Renderer>();
                if (renderer == null)
                    return UnityMcpResponseUtils.Error($"No Renderer found on '{objectName}'");

                mat = renderer.sharedMaterial;
                if (mat == null)
                    return UnityMcpResponseUtils.Error($"No shared material found on '{objectName}'");
            }
            else
            {
                return UnityMcpResponseUtils.Error("Either objectName or materialPath is required");
            }

            string propTypeLower = propertyType.ToLowerInvariant();
            string valueStr = value ?? "";

            switch (propTypeLower)
            {
                case "float":
                case "int":
                case "range":
                    if (float.TryParse(valueStr, out float fVal))
                        mat.SetFloat(propertyName, fVal);
                    else
                        return UnityMcpResponseUtils.Error($"Cannot parse '{valueStr}' as float");
                    break;

                case "color":
                    Color cVal = UnityMcpColorUtils.ParseHtmlColor(valueStr);
                    mat.SetColor(propertyName, cVal);
                    if (propertyName.Contains("Emission", StringComparison.OrdinalIgnoreCase))
                        mat.EnableKeyword("_EMISSION");
                    break;

                case "keyword":
                    bool enable = valueStr.ToLowerInvariant() == "true" || valueStr == "1";
                    if (enable)
                        mat.EnableKeyword(propertyName);
                    else
                        mat.DisableKeyword(propertyName);
                    break;

                default:
                    return UnityMcpResponseUtils.Error($"Unsupported property type: {propertyType}. Supported: float, int, range, color, keyword");
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            return UnityMcpResponseUtils.Success(
                $"Set material property '{propertyName}' ({propertyType}) to '{valueStr}'",
                null, AssetDatabase.GetAssetPath(mat));
        }

        public static string SetVfxGraphProperty(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            System.Type vfxType = System.Type.GetType("UnityEngine.VFX.VisualEffect, UnityEngine.VFXModule");
            if (vfxType == null)
                vfxType = System.Type.GetType("UnityEngine.VFX.VisualEffect, Unity.VisualEffectGraph.Runtime");
            if (vfxType == null)
                return UnityMcpResponseUtils.Error("VFX Graph package is not available in this project");

            Component vfx = go.GetComponent(vfxType);
            if (vfx == null)
                return UnityMcpResponseUtils.Error($"No VisualEffect component found on '{objectName}'");

            string propertyName = req.propertyName;
            string propertyType = req.propertyType;
            string value = req.value;

            if (string.IsNullOrEmpty(propertyName))
                return UnityMcpResponseUtils.Error("propertyName is required");
            if (string.IsNullOrEmpty(propertyType))
                return UnityMcpResponseUtils.Error("propertyType is required");
            if (value == null)
                value = "";

            try
            {
                string propTypeLower = propertyType.ToLowerInvariant();

                switch (propTypeLower)
                {
                    case "float":
                        if (float.TryParse(value, out float fv))
                        {
                            MethodInfo setFloat = vfxType.GetMethod("SetFloat", new[] { typeof(string), typeof(float) });
                            if (setFloat != null) setFloat.Invoke(vfx, new object[] { propertyName, fv });
                        }
                        break;

                    case "int":
                        if (int.TryParse(value, out int iv))
                        {
                            MethodInfo setInt = vfxType.GetMethod("SetInt", new[] { typeof(string), typeof(int) });
                            if (setInt != null) setInt.Invoke(vfx, new object[] { propertyName, iv });
                        }
                        break;

                    case "bool":
                        bool bv = value.ToLowerInvariant() == "true" || value == "1";
                        MethodInfo setBool = vfxType.GetMethod("SetBool", new[] { typeof(string), typeof(bool) });
                        if (setBool != null) setBool.Invoke(vfx, new object[] { propertyName, bv });
                        break;

                    case "vector2":
                    case "vector3":
                    case "vector4":
                        string[] parts = value.Split(',');
                        if (parts.Length >= 3 && float.TryParse(parts[0], out float vx) && float.TryParse(parts[1], out float vy) && float.TryParse(parts[2], out float vz))
                        {
                            float vw = 1f;
                            if (parts.Length >= 4) float.TryParse(parts[3], out vw);

                            if (propTypeLower == "vector3")
                            {
                                MethodInfo setV3 = vfxType.GetMethod("SetVector3", new[] { typeof(string), typeof(Vector3) });
                                if (setV3 != null) setV3.Invoke(vfx, new object[] { propertyName, new Vector3(vx, vy, vz) });
                            }
                            else
                            {
                                MethodInfo setV4 = vfxType.GetMethod("SetVector4", new[] { typeof(string), typeof(Vector4) });
                                if (setV4 != null) setV4.Invoke(vfx, new object[] { propertyName, new Vector4(vx, vy, vz, vw) });
                            }
                        }
                        break;

                    case "color":
                        Color cv = UnityMcpColorUtils.ParseHtmlColor(value);
                        MethodInfo setColor = vfxType.GetMethod("SetVector4", new[] { typeof(string), typeof(Vector4) });
                        if (setColor != null)
                            setColor.Invoke(vfx, new object[] { propertyName, new Vector4(cv.r, cv.g, cv.b, cv.a) });
                        break;

                    default:
                        return UnityMcpResponseUtils.Error($"Unsupported VFX property type: {propertyType}");
                }
            }
            catch (Exception ex)
            {
                return UnityMcpResponseUtils.Error($"Error setting VFX property '{propertyName}': {ex.Message}");
            }

            EditorUtility.SetDirty(vfx);
            return UnityMcpResponseUtils.Success(
                $"Set VFX Graph property '{propertyName}' ({propertyType}) to '{value}' on '{objectName}'",
                objectName);
        }

        public static string CreateVfxGraphFromTemplate(RequestModel req)
        {
            string templatePath = req.templatePath;
            string outputName = req.outputName;

            if (string.IsNullOrEmpty(templatePath))
                return UnityMcpResponseUtils.Error("templatePath is required");
            if (string.IsNullOrEmpty(outputName))
                return UnityMcpResponseUtils.Error("outputName is required");

            if (!UnityMcpPathUtils.IsSafeReadableAssetPath(templatePath))
                return UnityMcpResponseUtils.Error($"Invalid template path: {templatePath}");

            System.Type vfxType = System.Type.GetType("UnityEngine.VFX.VisualEffectAsset, UnityEngine.VFXModule");
            if (vfxType == null)
                vfxType = System.Type.GetType("UnityEngine.VFX.VisualEffectAsset, Unity.VisualEffectGraph.Runtime");
            if (vfxType == null)
                return UnityMcpResponseUtils.Error("VFX Graph package is not available in this project");

            UnityEngine.Object templateAsset = AssetDatabase.LoadAssetAtPath(templatePath, vfxType);
            if (templateAsset == null)
                return UnityMcpResponseUtils.Error($"VFX Graph template not found at: {templatePath}");

            string baseFolder = "Assets/AI_Generated/Prefabs";
            UnityMcpPathUtils.EnsureDirectoryExists(baseFolder);

            GameObject go = new GameObject(outputName);
            System.Type visualEffectType = System.Type.GetType("UnityEngine.VFX.VisualEffect, UnityEngine.VFXModule");
            if (visualEffectType == null)
                visualEffectType = System.Type.GetType("UnityEngine.VFX.VisualEffect, Unity.VisualEffectGraph.Runtime");

            if (visualEffectType != null)
            {
                Component vfx = go.AddComponent(visualEffectType);
                PropertyInfo assetProp = visualEffectType.GetProperty("visualEffectAsset");
                if (assetProp != null)
                    assetProp.SetValue(vfx, templateAsset, null);
            }

            Undo.RegisterCreatedObjectUndo(go, $"Create VFX Graph from template");

            string prefabPath = UnityMcpPathUtils.GetPrefabSavePath(outputName);
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath, out bool success);

            return UnityMcpResponseUtils.Success(
                $"Created VFX Graph '{outputName}' from template",
                outputName, prefabPath);
        }
    }
}
