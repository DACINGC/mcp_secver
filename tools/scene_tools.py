from tools.unity_http import get_from_unity, post_to_unity


def register_scene_tools(mcp):
    @mcp.tool()
    def create_empty(
        name: str = "EmptyObject",
        x: float = 0.0,
        y: float = 0.0,
        z: float = 0.0
    ) -> dict:
        """Create an empty GameObject in the Unity scene."""
        payload = {
            "name": name,
            "x": max(-10000.0, min(10000.0, x)),
            "y": max(-10000.0, min(10000.0, y)),
            "z": max(-10000.0, min(10000.0, z))
        }
        return post_to_unity("/create-empty", payload)

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
