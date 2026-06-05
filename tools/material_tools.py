from .unity_http import post_to_unity


def register_material_tools(mcp):
    @mcp.tool()
    def create_material(
        material_name: str,
        color: str = "#FFFFFF",
        shader_name: str = "Universal Render Pipeline/Particles/Unlit",
        emission_color: str = "#000000",
        emission_intensity: float = 0.0
    ) -> dict:
        """Create a Unity Material asset under Assets/AI_Generated/Materials/.

        Parameters:
        - material_name: Name for the material asset.
        - color: Base color in HTML hex format, e.g. "#FF5733".
        - shader_name: Preferred shader name, will fallback if missing.
        - emission_color: Emission color in HTML hex format.
        - emission_intensity: Emission intensity (0-20). 0 disables emission.
        """
        payload = {
            "materialName": material_name,
            "color": color,
            "shaderName": shader_name,
            "emissionColor": emission_color,
            "emissionIntensity": max(0.0, min(20.0, emission_intensity))
        }
        return post_to_unity("/create-material", payload)

    @mcp.tool()
    def assign_material(
        object_name: str,
        material_path: str
    ) -> dict:
        """Assign a Material to a GameObject in the Unity scene.

        Parameters:
        - object_name: Name of the GameObject to assign the material to.
        - material_path: Path to the .mat file, e.g. "Assets/AI_Generated/Materials/MyMaterial.mat".
        """
        payload = {
            "objectName": object_name,
            "materialPath": material_path
        }
        return post_to_unity("/assign-material", payload)

    @mcp.tool()
    def create_additive_particle_material(
        material_name: str,
        color: str = "#33AAFF",
        emission_intensity: float = 2.0
    ) -> dict:
        """Create a transparent/additive particle Material suitable for Particle Systems.

        Parameters:
        - material_name: Name for the material asset.
        - color: Base color in HTML hex format.
        - emission_intensity: Emission intensity (0-20).
        """
        payload = {
            "materialName": material_name,
            "color": color,
            "emissionIntensity": max(0.0, min(20.0, emission_intensity))
        }
        return post_to_unity("/create-additive-particle-material", payload)

    @mcp.tool()
    def set_material_color(
        material_path: str,
        color: str = "#FFFFFF"
    ) -> dict:
        """Change the base color of an existing Unity Material.

        Parameters:
        - material_path: Path to the .mat file.
        - color: New base color in HTML hex format.
        """
        payload = {
            "materialPath": material_path,
            "color": color
        }
        return post_to_unity("/set-material-color", payload)

    @mcp.tool()
    def set_material_emission(
        material_path: str,
        emission_color: str = "#33AAFF",
        emission_intensity: float = 2.0
    ) -> dict:
        """Change the emission color and intensity of an existing Unity Material.

        Parameters:
        - material_path: Path to the .mat file.
        - emission_color: Emission color in HTML hex format.
        - emission_intensity: Emission intensity (0-20).
        """
        payload = {
            "materialPath": material_path,
            "emissionColor": emission_color,
            "emissionIntensity": max(0.0, min(20.0, emission_intensity))
        }
        return post_to_unity("/set-material-emission", payload)
