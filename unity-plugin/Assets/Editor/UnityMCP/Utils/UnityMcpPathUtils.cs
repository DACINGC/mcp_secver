using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace UnityMCP.Utils
{
    public static class UnityMcpPathUtils
    {
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "GameObject";

            string invalid = new string(Path.GetInvalidFileNameChars());
            string pattern = "[" + Regex.Escape(invalid) + "]";
            string sanitized = Regex.Replace(fileName, pattern, "_");

            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "GameObject";

            return sanitized;
        }

        public static bool IsValidAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.Contains(".."))
                return false;

            string normalized = path.Replace("\\", "/");
            if (!normalized.StartsWith("Assets/"))
                return false;

            return true;
        }

        public static bool IsSafeMaterialPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.Contains(".."))
                return false;

            string normalized = path.Replace("\\", "/");
            if (!normalized.StartsWith("Assets/"))
                return false;

            if (!normalized.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        public static bool IsSafeGeneratedMaterialPath(string path)
        {
            if (!IsSafeMaterialPath(path))
                return false;

            string normalized = path.Replace("\\", "/");
            if (!normalized.StartsWith("Assets/AI_Generated/Materials/"))
                return false;

            return true;
        }

        public static bool IsSafeReadablePrefabPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.Contains(".."))
                return false;

            string normalized = path.Replace("\\", "/");
            if (!normalized.StartsWith("Assets/"))
                return false;

            if (!normalized.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        public static bool IsSafeGeneratedPrefabPath(string path)
        {
            if (!IsSafeReadablePrefabPath(path))
                return false;

            string normalized = path.Replace("\\", "/");
            if (!normalized.StartsWith("Assets/AI_Generated/Prefabs/"))
                return false;

            return true;
        }

        public static bool IsSafeTemplatePrefabPath(string path)
        {
            if (!IsSafeReadablePrefabPath(path))
                return false;

            string normalized = path.Replace("\\", "/");
            if (!normalized.StartsWith("Assets/VFX/Templates/") &&
                !normalized.StartsWith("Assets/AI_Generated/Prefabs/"))
                return false;

            return true;
        }

        public static bool IsSafeCapturePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.Contains(".."))
                return false;

            string normalized = path.Replace("\\", "/");
            if (!normalized.StartsWith("Assets/AI_Generated/Captures/"))
                return false;

            if (!normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        public static string GetPrefabSavePath(string objectName)
        {
            string baseFolder = "Assets/AI_Generated/Prefabs";
            EnsureDirectoryExists(baseFolder);

            string safeName = SanitizeFileName(objectName);
            string path = $"{baseFolder}/{safeName}.prefab";

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path) != null)
            {
                int counter = 1;
                while (AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
                    $"{baseFolder}/{safeName}_{counter}.prefab") != null)
                {
                    counter++;
                }
                path = $"{baseFolder}/{safeName}_{counter}.prefab";
            }

            return path;
        }

        public static string GetMaterialSavePath(string materialName)
        {
            string baseFolder = "Assets/AI_Generated/Materials";
            EnsureDirectoryExists(baseFolder);

            string safeName = SanitizeFileName(materialName);
            string path = $"{baseFolder}/{safeName}.mat";

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path) != null)
            {
                int counter = 1;
                while (AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(
                    $"{baseFolder}/{safeName}_{counter}.mat") != null)
                {
                    counter++;
                }
                path = $"{baseFolder}/{safeName}_{counter}.mat";
            }

            return path;
        }

        public static string GetCaptureSavePath(string fileName)
        {
            string baseFolder = "Assets/AI_Generated/Captures";
            EnsureDirectoryExists(baseFolder);

            string safeName = SanitizeFileName(fileName);
            string path = $"{baseFolder}/{safeName}.png";

            if (File.Exists(path))
            {
                int counter = 1;
                while (File.Exists($"{baseFolder}/{safeName}_{counter}.png"))
                    counter++;
                path = $"{baseFolder}/{safeName}_{counter}.png";
            }

            return path;
        }

        public static void EnsureDirectoryExists(string path)
        {
            string normalized = path.Replace("\\", "/");
            string[] parts = normalized.Split('/');

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                current += "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(current))
                {
                    string parent = string.Join("/", parts, 0, i);
                    string newFolder = parts[i];
                    AssetDatabase.CreateFolder(parent, newFolder);
                }
            }
        }
    }
}
