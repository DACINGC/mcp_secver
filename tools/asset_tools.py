from tools.unity_http import post_to_unity


def register_asset_tools(mcp):
    @mcp.tool()
    def list_generated_assets(
        asset_type: str = "all"
    ) -> dict:
        """List previously generated assets under Assets/AI_Generated/.

        Parameters:
        - asset_type: 'all', 'prefab', 'material', or 'capture'.
        """
        payload = {"assetType": asset_type}
        return post_to_unity("/list-generated-assets", payload)

    @mcp.tool()
    def clear_ai_generated_scene_objects(
        prefix: str = "AI_"
    ) -> dict:
        """Delete AI-generated GameObjects from the scene by name prefix.

        Only removes scene objects, never deletes assets.
        Prefix must be at least 3 characters long.

        Parameters:
        - prefix: Name prefix to match (default 'AI_', min 3 chars).
        """
        payload = {"prefix": prefix}
        return post_to_unity("/clear-ai-generated-scene-objects", payload)

    @mcp.tool()
    def get_object_info(
        object_name: str,
        include_children: bool = True
    ) -> dict:
        """Get detailed information about a GameObject in the scene.

        Returns transform data, component list, child info, and counts.

        Parameters:
        - object_name: Name of the GameObject to inspect.
        - include_children: Include child object information.
        """
        payload = {
            "objectName": object_name,
            "includeChildren": include_children
        }
        return post_to_unity("/get-object-info", payload)
