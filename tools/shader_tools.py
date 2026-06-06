from tools.unity_http import post_to_unity


def register_shader_tools(mcp):
    @mcp.tool()
    def list_material_properties(
        material_path: str
    ) -> dict:
        """List all Shader properties of a Material asset.

        Parameters:
        - material_path: Path to the .mat file (must be under Assets/).
        """
        payload = {"materialPath": material_path}
        return post_to_unity("/list-material-properties", payload)

    @mcp.tool()
    def set_material_property(
        material_path: str,
        property_name: str,
        property_type: str,
        value: str
    ) -> dict:
        """Set a Shader property value on a Material asset.

        Parameters:
        - material_path: Path to the .mat file.
        - property_name: Name of the shader property (e.g. _Color, _BaseColor).
        - property_type: 'color', 'float', 'range', 'vector', or 'texture'.
        - value: Value string:
            color: HTML hex like "#33AAFF"
            float/range: number like "2.5"
            vector: "x,y,z,w"
            texture: asset path like "Assets/Textures/tex.png"
        """
        payload = {
            "materialPath": material_path,
            "propertyName": property_name,
            "propertyType": property_type,
            "value": value
        }
        return post_to_unity("/set-material-property", payload)

    @mcp.tool()
    def set_vfx_graph_property(
        object_name: str,
        property_name: str,
        property_type: str,
        value: str,
        include_children: bool = True
    ) -> dict:
        """Set a VisualEffect (VFX Graph) exposed property via reflection.

        This does NOT require the VFX Graph package to compile;
        if unavailable, returns an informational error.

        Parameters:
        - object_name: Name of the GameObject with a VisualEffect component.
        - property_name: Exposed property name in the VFX Graph.
        - property_type: 'float', 'int', 'bool', 'color', 'vector3', 'vector4', 'texture'.
        - value: Value string appropriate for the type.
        - include_children: Search child objects for VisualEffect components.
        """
        payload = {
            "objectName": object_name,
            "propertyName": property_name,
            "propertyType": property_type,
            "value": value,
            "includeChildren": include_children
        }
        return post_to_unity("/set-vfx-graph-property", payload)

    @mcp.tool()
    def create_vfx_graph_from_template(
        template_path: str,
        output_name: str,
        x: float = 0,
        y: float = 0,
        z: float = 0,
        scale: float = 1.0,
        main_color: str = "#33AAFF",
        save_as_prefab: bool = True
    ) -> dict:
        """Create a VFX Graph instance from a template Prefab.

        Template must be under Assets/VFX/Templates/ or Assets/AI_Generated/Prefabs/.
        Uses reflection for VFX Graph access -- safe for projects without the package.

        Parameters:
        - template_path: Path to the template prefab.
        - output_name: Name for the new instance.
        - x, y, z: World position.
        - scale: Uniform scale (0.01-100).
        - main_color: Optional HTML color for MainColor/BaseColor property.
        - save_as_prefab: Save result as prefab.
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
        return post_to_unity("/create-vfx-graph-from-template", payload)
