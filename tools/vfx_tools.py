from tools.unity_http import post_to_unity


def register_vfx_tools(mcp):
    @mcp.tool()
    def create_particle_effect(
        effect_name: str,
        color: str = "#33AAFF",
        duration: float = 2.0,
        emission_rate: float = 80.0,
        start_lifetime: float = 1.5,
        start_speed: float = 2.0,
        start_size: float = 0.2,
        radius: float = 1.0,
        loop: bool = True
    ) -> dict:
        """Create a Particle System based visual effect in the Unity scene."""
        payload = {
            "effectName": effect_name,
            "color": color,
            "duration": max(0.01, min(300.0, duration)),
            "emissionRate": max(0.0, min(100000.0, emission_rate)),
            "startLifetime": max(0.01, min(300.0, start_lifetime)),
            "startSpeed": max(0.0, min(1000.0, start_speed)),
            "startSize": max(0.001, min(100.0, start_size)),
            "radius": max(0.0, min(100.0, radius)),
            "loop": loop
        }
        return post_to_unity("/create-particle-effect", payload)

    @mcp.tool()
    def create_light(
        name: str,
        color: str = "#FFFFFF",
        intensity: float = 3.0,
        range_value: float = 5.0,
        x: float = 0.0,
        y: float = 2.0,
        z: float = 0.0
    ) -> dict:
        """Create a point light in the Unity scene."""
        payload = {
            "name": name,
            "color": color,
            "intensity": max(0.0, min(100000.0, intensity)),
            "range": max(0.01, min(1000.0, range_value)),
            "x": max(-10000.0, min(10000.0, x)),
            "y": max(-10000.0, min(10000.0, y)),
            "z": max(-10000.0, min(10000.0, z))
        }
        return post_to_unity("/create-light", payload)

    @mcp.tool()
    def create_magic_portal(
        effect_name: str,
        main_color: str = "#33AAFF",
        radius: float = 2.0,
        duration: float = 5.0,
        loop: bool = True,
        save_as_prefab: bool = False
    ) -> dict:
        """Create a magic portal visual effect with ring particles, core glow, sparks, and light.

        Generates a layered portal effect including a rotating ring (LineRenderer),
        ring particles, core particles, spark particles, and a point light.
        """
        payload = {
            "effectName": effect_name,
            "mainColor": main_color,
            "radius": max(0.2, min(10.0, radius)),
            "duration": max(0.5, min(30.0, duration)),
            "loop": loop,
            "saveAsPrefab": save_as_prefab
        }
        return post_to_unity("/create-magic-portal", payload)

    @mcp.tool()
    def create_fire_explosion(
        effect_name: str,
        radius: float = 2.0,
        intensity: float = 1.0,
        duration: float = 1.2,
        save_as_prefab: bool = False
    ) -> dict:
        """Create a fire explosion effect with burst flame, smoke, sparks, and flash light.

        Generates a non-looping layered explosion including fire burst particles,
        smoke burst particles, flying sparks, and an intense point light.
        """
        payload = {
            "effectName": effect_name,
            "radius": max(0.2, min(20.0, radius)),
            "intensity": max(0.1, min(5.0, intensity)),
            "duration": max(0.2, min(10.0, duration)),
            "saveAsPrefab": save_as_prefab
        }
        return post_to_unity("/create-fire-explosion", payload)

    @mcp.tool()
    def create_lightning_hit(
        effect_name: str,
        main_color: str = "#AA33FF",
        height: float = 4.0,
        radius: float = 1.0,
        duration: float = 0.8,
        branch_count: int = 5,
        save_as_prefab: bool = False
    ) -> dict:
        """Create a lightning hit effect with bolt, branches, sparks, and light.

        Generates a non-looping lightning strike including a main bolt (LineRenderer),
        random branch bolts, impact sparks, and a point light.
        """
        payload = {
            "effectName": effect_name,
            "mainColor": main_color,
            "height": max(0.5, min(20.0, height)),
            "radius": max(0.1, min(10.0, radius)),
            "duration": max(0.1, min(5.0, duration)),
            "branchCount": max(1, min(20, branch_count)),
            "saveAsPrefab": save_as_prefab
        }
        return post_to_unity("/create-lightning-hit", payload)

    @mcp.tool()
    def create_heal_aura(
        effect_name: str,
        main_color: str = "#55FF88",
        radius: float = 2.0,
        duration: float = 4.0,
        loop: bool = True,
        save_as_prefab: bool = False
    ) -> dict:
        """Create a healing aura effect with ground ring, rising particles, sparkles, and light.

        Generates a layered healing aura including a ring (LineRenderer),
        rising green particles, sparkling particles, and a soft point light.
        """
        payload = {
            "effectName": effect_name,
            "mainColor": main_color,
            "radius": max(0.2, min(10.0, radius)),
            "duration": max(0.5, min(30.0, duration)),
            "loop": loop,
            "saveAsPrefab": save_as_prefab
        }
        return post_to_unity("/create-heal-aura", payload)

    @mcp.tool()
    def create_smoke_burst(
        effect_name: str,
        color: str = "#777777",
        radius: float = 2.0,
        duration: float = 2.5,
        density: float = 1.0,
        save_as_prefab: bool = False
    ) -> dict:
        """Create a smoke burst effect with main smoke, drifting smoke, and ground dust ring.

        Generates a non-looping layered smoke burst including large smoke particles,
        slowly drifting smoke, and a ring of dust particles on the ground.
        """
        payload = {
            "effectName": effect_name,
            "color": color,
            "radius": max(0.2, min(20.0, radius)),
            "duration": max(0.5, min(20.0, duration)),
            "density": max(0.1, min(5.0, density)),
            "saveAsPrefab": save_as_prefab
        }
        return post_to_unity("/create-smoke-burst", payload)

    @mcp.tool()
    def create_slash_trail(
        effect_name: str,
        main_color: str = "#66CCFF",
        length: float = 3.0,
        width: float = 0.3,
        duration: float = 0.5,
        save_as_prefab: bool = False
    ) -> dict:
        """Create a slash trail / blade arc effect with arc line, sparks, and light.

        Generates a non-looping slash trail including an arc-shaped LineRenderer,
        spark particles along the arc, and a brief point light.
        """
        payload = {
            "effectName": effect_name,
            "mainColor": main_color,
            "length": max(0.5, min(20.0, length)),
            "width": max(0.02, min(3.0, width)),
            "duration": max(0.1, min(5.0, duration)),
            "saveAsPrefab": save_as_prefab
        }
        return post_to_unity("/create-slash-trail", payload)
