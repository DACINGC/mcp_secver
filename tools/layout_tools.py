from tools.unity_http import post_to_unity


def register_layout_tools(mcp):
    @mcp.tool()
    def layout_objects(
        object_name: str = "",
        prefab_path: str = "",
        pattern: str = "grid",
        count: int = 10,
        spacing: float = 2.0,
        radius: float = 5.0
    ) -> dict:
        """Arrange multiple copies of an object in a spatial pattern.

        Duplicates a GameObject (by name) or instantiates a Prefab (by path) in
        grid, circle, random, or line layout patterns.

        Parameters:
        - object_name: Name of the source GameObject to duplicate.
        - prefab_path: Path to a Prefab asset (e.g. "Assets/MyPrefab.prefab").
                       Takes priority over objectName if both are provided.
        - pattern: Layout pattern: 'grid', 'circle', 'random', or 'line'.
        - count: Number of objects to place (1-1000).
        - spacing: Distance between objects in grid/line patterns (0.1-100).
        - radius: Radius for circle/random patterns (0.1-100).
        """
        payload = {
            "objectName": object_name,
            "prefabPath": prefab_path,
            "pattern": pattern,
            "count": max(1, min(1000, count)),
            "spacing": max(0.1, min(100.0, spacing)),
            "radius": max(0.1, min(100.0, radius)),
        }
        return post_to_unity("/layout-objects", payload)
