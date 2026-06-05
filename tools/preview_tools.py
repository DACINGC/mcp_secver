from tools.unity_http import post_to_unity


def register_preview_tools(mcp):
    @mcp.tool()
    def focus_scene_object(
        object_name: str
    ) -> dict:
        """Select and frame a GameObject in the Unity Scene view.

        Parameters:
        - object_name: Name of the GameObject to focus on.
        """
        payload = {"objectName": object_name}
        return post_to_unity("/focus-scene-object", payload)

    @mcp.tool()
    def play_effect(
        object_name: str,
        include_children: bool = True
    ) -> dict:
        """Play all ParticleSystem components on a GameObject.

        Parameters:
        - object_name: Name of the root GameObject.
        - include_children: Also play ParticleSystems on child objects.
        """
        payload = {
            "objectName": object_name,
            "includeChildren": include_children
        }
        return post_to_unity("/play-effect", payload)

    @mcp.tool()
    def stop_effect(
        object_name: str,
        include_children: bool = True,
        clear_particles: bool = True
    ) -> dict:
        """Stop all ParticleSystem components on a GameObject.

        Parameters:
        - object_name: Name of the root GameObject.
        - include_children: Also stop ParticleSystems on child objects.
        - clear_particles: Clear existing particles on stop.
        """
        payload = {
            "objectName": object_name,
            "includeChildren": include_children,
            "clearParticles": clear_particles
        }
        return post_to_unity("/stop-effect", payload)

    @mcp.tool()
    def capture_view(
        file_name: str = "unity_capture",
        view_type: str = "scene",
        width: int = 1280,
        height: int = 720
    ) -> dict:
        """Capture the Unity Scene view or Game view and save as PNG.

        Saves to Assets/AI_Generated/Captures/{file_name}.png

        Parameters:
        - file_name: Name for the captured image file.
        - view_type: 'scene' or 'game'.
        - width: Image width (256-3840).
        - height: Image height (256-2160).
        """
        payload = {
            "fileName": file_name,
            "viewType": view_type,
            "width": max(256, min(3840, width)),
            "height": max(256, min(2160, height))
        }
        return post_to_unity("/capture-view", payload)
