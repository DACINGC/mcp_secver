from tools.unity_http import post_to_unity


def register_terrain_tools(mcp):
    @mcp.tool()
    def create_terrain(
        name: str = "Terrain",
        width: int = 500,
        length: int = 500,
        height: float = 50.0,
        resolution: int = 513,
        x: float = 0.0,
        y: float = 0.0,
        z: float = 0.0,
        parent: str = ""
    ) -> dict:
        """Create a Terrain object in the Unity scene.

        Creates a new Terrain with specified dimensions and heightmap resolution.
        Use sculpt_terrain to shape the terrain after creation.

        Parameters:
        - name: Name for the terrain GameObject.
        - width: Terrain width in world units (10-2000).
        - length: Terrain length in world units (10-2000).
        - height: Maximum height of the terrain (1-1000).
        - resolution: Heightmap resolution, recommended 2^n+1 (e.g. 513). Range 33-4097.
        - x, y, z: Position in world space.
        - parent: Parent GameObject name to parent under (e.g. 'AI_TestSuite').
        """
        payload = {
            "name": name,
            "width": max(10, min(2000, width)),
            "length": max(10, min(2000, length)),
            "height": max(1.0, min(1000.0, height)),
            "density": max(33, min(4097, resolution)),
            "x": max(-10000.0, min(10000.0, x)),
            "y": max(-10000.0, min(10000.0, y)),
            "z": max(-10000.0, min(10000.0, z)),
            "parent": parent,
        }
        return post_to_unity("/create-terrain", payload)

    @mcp.tool()
    def sculpt_terrain(
        object_name: str,
        shape: str = "smooth",
        strength: float = 0.5
    ) -> dict:
        """Sculpt a terrain's heightmap with predefined shapes.

        Modifies the heightmap of an existing Terrain object.

        Parameters:
        - object_name: Name of the Terrain GameObject.
        - shape: Heightmap shape pattern: 'flat', 'smooth' (center bump), 'mountain' (sharp peak),
                 'valley' (center dip), or 'random' (Perlin noise).
        - strength: Effect intensity (0.0 - 1.0).
        """
        payload = {
            "objectName": object_name,
            "shape": shape,
            "strength": max(0.0, min(1.0, strength)),
        }
        return post_to_unity("/sculpt-terrain", payload)

    @mcp.tool()
    def paint_terrain(
        object_name: str,
        layer_type: str = "grass"
    ) -> dict:
        """Paint a terrain surface with a texture layer.

        Applies a terrain texture layer (grass, sand, rock, snow) to an existing Terrain.

        Parameters:
        - object_name: Name of the Terrain GameObject.
        - layer_type: Texture type: 'grass', 'sand', 'rock', 'snow', or 'custom'.
                      'custom' requires a texturePath parameter (not yet supported in Unity).
        """
        payload = {
            "objectName": object_name,
            "layerType": layer_type,
        }
        return post_to_unity("/paint-terrain", payload)
