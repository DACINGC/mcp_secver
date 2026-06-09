using UnityEngine;
using UnityEngine.Rendering;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    public static class UnityMcpEnvironmentTools
    {
        public static string SetEnvironment(RequestModel req)
        {
            if (req.fogEnabled)
            {
                RenderSettings.fog = true;
                if (!string.IsNullOrEmpty(req.fogColor))
                    RenderSettings.fogColor = UnityMcpColorUtils.ParseHtmlColor(req.fogColor);
                string mode = string.IsNullOrEmpty(req.fogMode) ? "exponential" : req.fogMode.ToLower();
                RenderSettings.fogMode = mode switch
                {
                    "linear" => FogMode.Linear,
                    "exponential_squared" => FogMode.ExponentialSquared,
                    _ => FogMode.Exponential,
                };
                RenderSettings.fogDensity = Mathf.Clamp((float)req.fogDensity, 0f, 1f);
            }
            else
            {
                RenderSettings.fog = false;
            }

            if (!string.IsNullOrEmpty(req.ambientColor))
            {
                Color ambient = UnityMcpColorUtils.ParseHtmlColor(req.ambientColor);
                float intensity = Mathf.Clamp((float)req.ambientIntensity, 0f, 8f);
                if (intensity <= 0f) intensity = 1f;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = ambient * intensity;
            }

            return UnityMcpResponseUtils.Success("Environment settings updated");
        }
    }
}
