from tools.unity_http import post_to_unity


def register_prefab_tools(mcp):
    @mcp.tool()
    def save_prefab(
        object_name: str,
        prefab_path: str = ""
    ) -> dict:
        """Save an existing GameObject in the Unity scene as a prefab."""
        payload = {
            "objectName": object_name,
            "prefabPath": prefab_path
        }
        return post_to_unity("/save-prefab", payload)
