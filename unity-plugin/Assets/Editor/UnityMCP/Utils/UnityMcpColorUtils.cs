using UnityEngine;

namespace UnityMCP.Utils
{
    public static class UnityMcpColorUtils
    {
        public static Color ParseHtmlColor(string html)
        {
            if (string.IsNullOrEmpty(html))
                return Color.white;

            if (html.StartsWith("#"))
                html = html.Substring(1);

            if (html.Length == 3)
            {
                string r = char.ToString(html[0]);
                string g = char.ToString(html[1]);
                string b = char.ToString(html[2]);
                html = r + r + g + g + b + b;
            }

            if (html.Length == 6)
            {
                if (ColorUtility.TryParseHtmlString("#" + html, out Color color))
                    return color;
            }

            if (html.Length == 8)
            {
                if (ColorUtility.TryParseHtmlString("#" + html, out Color color))
                    return color;
            }

            return Color.white;
        }

        public static Color MultiplyColor(Color color, float intensity)
        {
            return new Color(
                color.r * intensity,
                color.g * intensity,
                color.b * intensity,
                color.a
            );
        }
    }
}
