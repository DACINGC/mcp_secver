from tools.unity_http import post_to_unity


def register_environment_tools(mcp):
    @mcp.tool()
    def set_environment(
        fog_enabled: bool = False,
        fog_color: str = "#808080",
        fog_mode: str = "exponential",
        fog_density: float = 0.01,
        ambient_color: str = "#FFFFFF",
        ambient_intensity: float = 1.0
    ) -> dict:
        """Configure Unity render settings: fog, ambient lighting, and atmosphere.

        Controls global environment settings like fog (color, density, mode) and
        ambient light color/intensity.

        Parameters:
        - fog_enabled: Enable or disable fog.
        - fog_color: Fog color in HTML hex format (e.g. "#808080").
        - fog_mode: Fog mode: 'linear', 'exponential', or 'exponential_squared'.
        - fog_density: Fog density (0.0 - 1.0). Only affects exponential modes.
        - ambient_color: Ambient light color in HTML hex format.
        - ambient_intensity: Ambient light intensity multiplier (0-8).
        """
        payload = {
            "fogEnabled": fog_enabled,
            "fogColor": fog_color,
            "fogMode": fog_mode,
            "fogDensity": max(0.0, min(1.0, fog_density)),
            "ambientColor": ambient_color,
            "ambientIntensity": max(0.0, min(8.0, ambient_intensity)),
        }
        return post_to_unity("/set-environment", payload)
