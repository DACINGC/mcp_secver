using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpReportTools
    {
        public static string ExportEffectReport(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            int particleSystemCount = 0;
            int lightCount = 0;
            int rendererCount = 0;
            int lineRendererCount = 0;
            int trailRendererCount = 0;
            int childCount = go.transform.childCount;

            var materialPaths = new HashSet<string>();

            var allComponents = go.GetComponentsInChildren<Component>(true);
            var componentNames = new List<string>();

            foreach (var comp in allComponents)
            {
                if (comp == null) continue;

                string typeName = comp.GetType().Name;
                if (!componentNames.Contains(typeName))
                    componentNames.Add(typeName);

                if (comp is ParticleSystem) particleSystemCount++;
                else if (comp is Light) lightCount++;
                else if (comp is LineRenderer) { lineRendererCount++; rendererCount++; }
                else if (comp is TrailRenderer) { trailRendererCount++; rendererCount++; }
                else if (comp is Renderer) rendererCount++;

                if (comp is Renderer ren)
                {
                    if (ren.sharedMaterial != null)
                    {
                        string matPath = AssetDatabase.GetAssetPath(ren.sharedMaterial);
                        if (!string.IsNullOrEmpty(matPath) && matPath.StartsWith("Assets/"))
                            materialPaths.Add(matPath);
                    }
                }
            }

            var report = new EffectReport
            {
                objectName = go.name,
                generatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                position = new Vector3Info
                {
                    x = go.transform.position.x,
                    y = go.transform.position.y,
                    z = go.transform.position.z
                },
                rotation = new Vector3Info
                {
                    x = go.transform.eulerAngles.x,
                    y = go.transform.eulerAngles.y,
                    z = go.transform.eulerAngles.z
                },
                scale = new Vector3Info
                {
                    x = go.transform.localScale.x,
                    y = go.transform.localScale.y,
                    z = go.transform.localScale.z
                },
                childCount = childCount,
                particleSystemCount = particleSystemCount,
                lightCount = lightCount,
                rendererCount = rendererCount,
                lineRendererCount = lineRendererCount,
                trailRendererCount = trailRendererCount,
                components = componentNames,
                materialPaths = materialPaths.ToList()
            };

            string fileName = string.IsNullOrEmpty(req.fileName)
                ? $"{objectName}_Report"
                : req.fileName;

            if (string.IsNullOrEmpty(req.filePrefix))
            {
                string savePath = UnityMcpPathUtils.GetReportSavePath(fileName);

                string jsonContent = JsonUtility.ToJson(report, true);
                System.IO.File.WriteAllText(savePath, jsonContent);
                AssetDatabase.Refresh();

                var response = new ResponseModel
                {
                    success = true,
                    message = $"Report saved to {savePath}",
                    objectName = objectName,
                    assetPath = savePath,
                    effectReport = report
                };
                return JsonUtility.ToJson(response);
            }
            else
            {
                var response = new ResponseModel
                {
                    success = true,
                    message = $"Report generated for '{objectName}'",
                    objectName = objectName,
                    effectReport = report
                };
                return JsonUtility.ToJson(response);
            }
        }
    }
}
