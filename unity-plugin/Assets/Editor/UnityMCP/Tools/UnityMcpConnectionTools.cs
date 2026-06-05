using UnityEditor;
using UnityEngine;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpConnectionTools
    {
        public static string Ping()
        {
            return UnityMcpResponseUtils.Success("pong", "UnityMCP");
        }
    }
}
