from tools.unity_http import get_from_unity, post_to_unity


def register_scene_tools(mcp):
    @mcp.tool()
    def init_test_suite(
        name: str = "AI_TestSuite"
    ) -> dict:
        """Create a root container GameObject for all AI test generations.

        All subsequent objects created by AI should be parented under this suite
        by passing parent='AI_TestSuite'. The reset_scene tool will destroy this
        container (and all children) for a clean reset without affecting cameras or lights.

        Parameters:
        - name: Name for the test suite root container (default: AI_TestSuite).
        """
        payload = {"name": name}
        return post_to_unity("/create-test-suite", payload)

    @mcp.tool()
    def create_empty(
        name: str = "EmptyObject",
        x: float = 0.0,
        y: float = 0.0,
        z: float = 0.0,
        parent: str = ""
    ) -> dict:
        """Create an empty GameObject in the Unity scene.
        If parent is set, the object will be parented under that GameObject."""
        payload = {
            "name": name,
            "x": max(-10000.0, min(10000.0, x)),
            "y": max(-10000.0, min(10000.0, y)),
            "z": max(-10000.0, min(10000.0, z)),
            "parent": parent,
        }
        return post_to_unity("/create-empty", payload)

    @mcp.tool()
    def create_camera(
        name: str = "Main Camera",
        x: float = 0.0,
        y: float = 1.0,
        z: float = -10.0,
        rx: float = 0.0,
        ry: float = 0.0,
        rz: float = 0.0,
        parent: str = ""
    ) -> dict:
        """Create a Camera object in the Unity scene.

        Parameters:
        - name: Name for the camera GameObject.
        - x, y, z: Position in world space.
        - rx, ry, rz: Rotation in Euler angles.
        - parent: Parent GameObject name to parent under (e.g. 'AI_TestSuite').
        """
        payload = {
            "name": name,
            "x": max(-10000.0, min(10000.0, x)),
            "y": max(-10000.0, min(10000.0, y)),
            "z": max(-10000.0, min(10000.0, z)),
            "rx": max(-360.0, min(360.0, rx)),
            "ry": max(-360.0, min(360.0, ry)),
            "rz": max(-360.0, min(360.0, rz)),
            "parent": parent,
        }
        return post_to_unity("/create-camera", payload)

    @mcp.tool()
    def list_scene_objects() -> dict:
        """List all GameObjects in the current Unity scene."""
        return get_from_unity("/list-scene-objects")

    @mcp.tool()
    def set_transform(
        object_name: str,
        x: float = 0.0,
        y: float = 0.0,
        z: float = 0.0,
        rx: float = 0.0,
        ry: float = 0.0,
        rz: float = 0.0,
        sx: float = 1.0,
        sy: float = 1.0,
        sz: float = 1.0
    ) -> dict:
        """Set the transform (position, rotation, scale) of a GameObject."""
        payload = {
            "objectName": object_name,
            "x": max(-10000.0, min(10000.0, x)),
            "y": max(-10000.0, min(10000.0, y)),
            "z": max(-10000.0, min(10000.0, z)),
            "rx": max(-360.0, min(360.0, rx)),
            "ry": max(-360.0, min(360.0, ry)),
            "rz": max(-360.0, min(360.0, rz)),
            "sx": max(0.01, min(1000.0, sx)),
            "sy": max(0.01, min(1000.0, sy)),
            "sz": max(0.01, min(1000.0, sz))
        }
        return post_to_unity("/set-transform", payload)

    @mcp.tool()
    def create_primitive(
        primitive_type: str = "Cube",
        name: str = "",
        color: str = "",
        x: float = 0.0,
        y: float = 0.0,
        z: float = 0.0,
        sx: float = 0.0,
        sy: float = 0.0,
        sz: float = 0.0,
        radius: float = 0.0,
        size: float = 0.0,
        parent: str = ""
    ) -> dict:
        """Create a 3D primitive (Cube, Sphere, Capsule, Cylinder, Plane, Quad) in the Unity scene.

        Creates a basic geometric shape and optionally assigns a color material.
        For Sphere, use 'radius' parameter. For Cube, use 'size' for uniform scaling.

        Parameters:
        - primitive_type: Type of primitive - Cube, Sphere, Capsule, Cylinder, Plane, or Quad.
        - name: Name for the object (defaults to the primitive type name).
        - color: HTML hex color like "#FF4400". Leave empty for default material.
        - x, y, z: Position in world space.
        - sx, sy, sz: Custom scale (overrides radius/size if > 0).
        - radius: Scale factor for Sphere (diameter = radius * 2).
        - size: Uniform scale for Cube and other primitives.
        - parent: Parent GameObject name to parent this object under (e.g. 'AI_TestSuite').
        """
        payload = {
            "primitiveType": primitive_type,
            "name": name,
            "color": color if color else "",
            "x": max(-10000.0, min(10000.0, x)),
            "y": max(-10000.0, min(10000.0, y)),
            "z": max(-10000.0, min(10000.0, z)),
            "sx": max(0.0, sx),
            "sy": max(0.0, sy),
            "sz": max(0.0, sz),
            "radius": max(0.0, radius),
            "width": max(0.0, size),
            "parent": parent,
        }
        return post_to_unity("/create-primitive", payload)

    @mcp.tool()
    def create_sample_scene(
        name: str = "SampleScene",
        ground_color: str = "#4CAF50",
        ground_size: float = 20.0,
        include_walls: bool = False,
        include_lights: bool = True,
        style: str = "default",
        parent: str = ""
    ) -> dict:
        """Create a complete sample scene with ground, lighting, and optional walls.

        Generates a ready-to-use scene with a ground plane, directional + point lights,
        and optionally enclosing walls.

        Parameters:
        - name: Root object name for the scene group.
        - ground_color: HTML hex color for the ground plane.
        - ground_size: Size of the ground plane (1-500).
        - include_walls: Add enclosing walls around the ground perimeter.
        - include_lights: Add directional and point lights.
        - style: Visual style (ignored in current version, reserved for future use).
        - parent: Parent GameObject name to parent this scene under (e.g. 'AI_TestSuite').
        """
        payload = {
            "name": name,
            "color": ground_color,
            "groundSize": max(1.0, min(500.0, ground_size)),
            "includeWalls": include_walls,
            "includeLights": include_lights,
            "style": style,
            "parent": parent,
        }
        return post_to_unity("/create-sample-scene", payload)

    @mcp.tool()
    def reset_scene(
        keep_lights: bool = False,
        keep_terrain: bool = False,
        create_default: bool = False
    ) -> dict:
        """Reset the Unity scene by removing AI-generated content.

        SAFE default: if an 'AI_TestSuite' container exists, only destroys that container
        and its children, keeping all other scene objects (cameras, lights, etc.).

        If no 'AI_TestSuite' is found:
        - By default, keeps Camera and Directional Light objects.
        - Set keep_lights=True to keep all Light objects.
        - Set keep_terrain=True to keep all Terrain objects.

        Parameters:
        - keep_lights: Keep all Light components in the scene.
        - keep_terrain: Keep all Terrain components in the scene.
        - create_default: (reserved) Create a default scene after clearing.
        """
        payload = {
            "keepLights": keep_lights,
            "keepTerrain": keep_terrain,
            "createDefault": create_default,
        }
        return post_to_unity("/reset-scene", payload)
