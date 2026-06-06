from tools.unity_http import post_to_unity


def register_tuning_tools(mcp):
    @mcp.tool()
    def update_particle_system(
        object_name: str,
        include_children: bool = True,
        color: str = "",
        emission_rate: float = -1,
        start_lifetime: float = -1,
        start_speed: float = -1,
        start_size: float = -1,
        duration: float = -1,
        loop: str = "keep"
    ) -> dict:
        """Modify ParticleSystem parameters on a GameObject.

        Only parameters with non-negative values (or non-empty for color) are applied.
        Parameters:
        - object_name: Name of the target GameObject.
        - include_children: Also modify child ParticleSystems.
        - color: HTML hex color for startColor (empty = skip).
        - emission_rate: Rate over time (0-5000, -1 = skip).
        - start_lifetime: Particle lifetime in seconds (0.05-30, -1 = skip).
        - start_speed: Initial speed (0-100, -1 = skip).
        - start_size: Initial size (0.01-20, -1 = skip).
        - duration: System duration (0.1-30, -1 = skip).
        - loop: "keep", "true", or "false".
        """
        payload = {
            "objectName": object_name,
            "includeChildren": include_children,
            "color": color,
            "emissionRate": -1 if emission_rate < 0 else max(0, min(5000, emission_rate)),
            "startLifetime": -1 if start_lifetime < 0 else max(0.05, min(30, start_lifetime)),
            "startSpeed": -1 if start_speed < 0 else max(0, min(100, start_speed)),
            "startSize": -1 if start_size < 0 else max(0.01, min(20, start_size)),
            "duration": -1 if duration < 0 else max(0.1, min(30, duration)),
            "loop": loop
        }
        return post_to_unity("/update-particle-system", payload)

    @mcp.tool()
    def update_light(
        object_name: str,
        include_children: bool = True,
        color: str = "",
        intensity: float = -1,
        range_value: float = -1
    ) -> dict:
        """Modify Light parameters on a GameObject.

        Parameters:
        - object_name: Name of the target GameObject.
        - include_children: Also modify child Lights.
        - color: HTML hex color (empty = skip).
        - intensity: Light intensity (0-20, -1 = skip).
        - range_value: Light range (0.1-100, -1 = skip).
        """
        payload = {
            "objectName": object_name,
            "includeChildren": include_children,
            "color": color,
            "intensity": -1 if intensity < 0 else max(0, min(20, intensity)),
            "range": -1 if range_value < 0 else max(0.1, min(100, range_value))
        }
        return post_to_unity("/update-light", payload)

    @mcp.tool()
    def update_line_renderer(
        object_name: str,
        include_children: bool = True,
        color: str = "",
        width: float = -1
    ) -> dict:
        """Modify LineRenderer or TrailRenderer on a GameObject.

        Parameters:
        - object_name: Name of the target GameObject.
        - include_children: Also modify child renderers.
        - color: HTML hex color (empty = skip).
        - width: Line width (0.01-5, -1 = skip).
        """
        payload = {
            "objectName": object_name,
            "includeChildren": include_children,
            "color": color,
            "width": -1 if width < 0 else max(0.01, min(5, width))
        }
        return post_to_unity("/update-line-renderer", payload)

    @mcp.tool()
    def recolor_effect(
        object_name: str,
        main_color: str,
        include_children: bool = True,
        affect_particles: bool = True,
        affect_lights: bool = True,
        affect_renderers: bool = True,
        affect_lines: bool = True
    ) -> dict:
        """Recolor an entire effect by modifying particles, lights, renderers, and lines.

        Parameters:
        - object_name: Root GameObject of the effect.
        - main_color: HTML hex color to apply.
        - include_children: Recolor all child objects.
        - affect_particles: Modify ParticleSystem startColor.
        - affect_lights: Modify Light color.
        - affect_renderers: Modify Renderer material colors.
        - affect_lines: Modify LineRenderer/TrailRenderer colors.
        """
        payload = {
            "objectName": object_name,
            "mainColor": main_color,
            "includeChildren": include_children,
            "affectParticles": affect_particles,
            "affectLights": affect_lights,
            "affectRenderers": affect_renderers,
            "affectLines": affect_lines
        }
        return post_to_unity("/recolor-effect", payload)

    @mcp.tool()
    def scale_effect(
        object_name: str,
        scale_multiplier: float = 1.0,
        scale_transform: bool = True,
        scale_particle_size: bool = True,
        scale_particle_speed: bool = False,
        include_children: bool = True
    ) -> dict:
        """Scale an entire effect (transform, particle size, speed).

        Parameters:
        - object_name: Root GameObject of the effect.
        - scale_multiplier: Scale factor (0.05-20).
        - scale_transform: Also scale the transform.
        - scale_particle_size: Scale ParticleSystem startSize.
        - scale_particle_speed: Scale ParticleSystem startSpeed.
        - include_children: Scale all child objects.
        """
        payload = {
            "objectName": object_name,
            "scaleMultiplier": max(0.05, min(20, scale_multiplier)),
            "scaleTransform": scale_transform,
            "scaleParticleSize": scale_particle_size,
            "scaleParticleSpeed": scale_particle_speed,
            "includeChildren": include_children
        }
        return post_to_unity("/scale-effect", payload)

    @mcp.tool()
    def adjust_effect_timing(
        object_name: str,
        duration_multiplier: float = 1.0,
        speed_multiplier: float = 1.0,
        include_children: bool = True
    ) -> dict:
        """Adjust effect duration and playback speed.

        Parameters:
        - object_name: Root GameObject of the effect.
        - duration_multiplier: Multiply duration and lifetime (0.1-10).
        - speed_multiplier: Multiply simulation speed (0.1-10).
        - include_children: Adjust all child objects.
        """
        payload = {
            "objectName": object_name,
            "durationMultiplier": max(0.1, min(10, duration_multiplier)),
            "speedMultiplier": max(0.1, min(10, speed_multiplier)),
            "includeChildren": include_children
        }
        return post_to_unity("/adjust-effect-timing", payload)
