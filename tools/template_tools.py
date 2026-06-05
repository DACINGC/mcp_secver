from tools.unity_http import post_to_unity


def register_template_tools(mcp):
    @mcp.tool()
    def create_vfx_from_template(
        template_path: str,
        output_name: str,
        x: float = 0,
        y: float = 0,
        z: float = 0,
        scale: float = 1.0,
        main_color: str = "",
        save_as_prefab: bool = True
    ) -> dict:
        """Create a VFX instance from a template Prefab with optional color override.

        Template must be under Assets/VFX/Templates/ or Assets/AI_Generated/Prefabs/.
        New prefab is saved to Assets/AI_Generated/Prefabs/.

        Parameters:
        - template_path: Path to the template prefab.
        - output_name: Name for the new instance.
        - x, y, z: World position.
        - scale: Uniform scale (0.01-100).
        - main_color: Optional HTML color to override particle/light colors.
        - save_as_prefab: Save the result as a new prefab.
        """
        payload = {
            "templatePath": template_path,
            "outputName": output_name,
            "x": x,
            "y": y,
            "z": z,
            "scale": max(0.01, min(100.0, scale)),
            "mainColor": main_color,
            "saveAsPrefab": save_as_prefab
        }
        return post_to_unity("/create-vfx-from-template", payload)

    @mcp.tool()
    def instantiate_prefab(
        prefab_path: str,
        object_name: str = "",
        x: float = 0,
        y: float = 0,
        z: float = 0,
        scale: float = 1.0
    ) -> dict:
        """Instantiate a Prefab into the current Unity scene.

        Parameters:
        - prefab_path: Path to the .prefab file (must be under Assets/).
        - object_name: Optional new name for the instance.
        - x, y, z: World position.
        - scale: Uniform scale (0.01-100).
        """
        payload = {
            "prefabPath": prefab_path,
            "objectName": object_name,
            "x": x,
            "y": y,
            "z": z,
            "scale": max(0.01, min(100.0, scale))
        }
        return post_to_unity("/instantiate-prefab", payload)
