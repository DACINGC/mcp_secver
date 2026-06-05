using UnityEngine;

namespace UnityMCP.Utils
{
    public static class UnityMcpResponseUtils
    {
        public static string Success(string message = "OK", string objectName = null)
        {
            var response = new ResponseModel
            {
                success = true,
                message = message,
                objectName = objectName
            };
            return JsonUtility.ToJson(response);
        }

        public static string Success(string message, string objectName, string assetPath)
        {
            var response = new ResponseModel
            {
                success = true,
                message = message,
                objectName = objectName,
                assetPath = assetPath
            };
            return JsonUtility.ToJson(response);
        }

        public static string Success(string message, string objectName, string assetPath, int affectedCount)
        {
            var response = new ResponseModel
            {
                success = true,
                message = message,
                objectName = objectName,
                assetPath = assetPath,
                affectedCount = affectedCount
            };
            return JsonUtility.ToJson(response);
        }

        public static string Error(string message)
        {
            var response = new ResponseModel
            {
                success = false,
                message = message,
                objectName = null
            };
            return JsonUtility.ToJson(response);
        }

        public static string ToJson(object obj)
        {
            return JsonUtility.ToJson(obj);
        }
    }
}
