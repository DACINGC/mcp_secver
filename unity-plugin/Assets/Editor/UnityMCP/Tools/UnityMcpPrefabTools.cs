using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpPrefabTools
    {
        public static string SavePrefab(RequestModel req)
        {
            string objectName = req.objectName;
            if (string.IsNullOrEmpty(objectName))
                return UnityMcpResponseUtils.Error("objectName is required");

            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return UnityMcpResponseUtils.Error($"GameObject '{objectName}' not found");

            string savePath;
            if (!string.IsNullOrEmpty(req.prefabPath))
            {
                if (!UnityMcpPathUtils.IsValidAssetPath(req.prefabPath))
                    return UnityMcpResponseUtils.Error("Invalid prefab path");

                string folder = System.IO.Path.GetDirectoryName(req.prefabPath).Replace("\\", "/");
                string fileName = System.IO.Path.GetFileNameWithoutExtension(req.prefabPath);
                string safeName = UnityMcpPathUtils.SanitizeFileName(fileName);
                savePath = folder + "/" + safeName + ".prefab";
            }
            else
            {
                savePath = UnityMcpPathUtils.GetPrefabSavePath(objectName);
            }

            PrefabUtility.SaveAsPrefabAsset(go, savePath, out bool success);
            if (!success)
                return UnityMcpResponseUtils.Error("Failed to save prefab");

            AssetDatabase.Refresh();
            return UnityMcpResponseUtils.Success($"Saved prefab to {savePath}", objectName);
        }
    }
}
