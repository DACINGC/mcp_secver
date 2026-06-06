from tools.unity_http import post_to_unity


def register_variant_tools(mcp):
    @mcp.tool()
    def create_effect_variants(
        source_object_name: str,
        variant_prefix: str = "AI_Variant",
        colors: str = "#33AAFF,#AA33FF,#FFAA33",
        count: int = 3,
        spacing: float = 3.0,
        save_as_prefab: bool = True
    ) -> dict:
        """Create multiple color variants of an existing effect.

        Duplicates the source object along the X axis, recoloring each copy.

        Parameters:
        - source_object_name: Name of the source GameObject to duplicate.
        - variant_prefix: Name prefix for each variant.
        - colors: Comma-separated HTML colors, cycled across variants.
        - count: Number of variants to create (1-12).
        - spacing: Distance between variants along X axis (0.5-20).
        - save_as_prefab: Save each variant as a prefab.
        """
        payload = {
            "sourceObjectName": source_object_name,
            "variantPrefix": variant_prefix,
            "colors": colors,
            "count": max(1, min(12, count)),
            "spacing": max(0.5, min(20, spacing)),
            "saveAsPrefab": save_as_prefab
        }
        return post_to_unity("/create-effect-variants", payload)

    @mcp.tool()
    def capture_effect_variants(
        object_prefix: str,
        file_prefix: str = "AI_Variant_Capture",
        view_type: str = "scene",
        width: int = 1280,
        height: int = 720
    ) -> dict:
        """Capture screenshots of multiple effect variants by name prefix.

        Parameters:
        - object_prefix: Name prefix to match root objects (min 3 chars).
        - file_prefix: Prefix for saved capture files.
        - view_type: 'scene' or 'game'.
        - width: Image width (256-3840).
        - height: Image height (256-2160).
        """
        payload = {
            "objectPrefix": object_prefix,
            "filePrefix": file_prefix,
            "viewType": view_type,
            "width": max(256, min(3840, width)),
            "height": max(256, min(2160, height))
        }
        return post_to_unity("/capture-effect-variants", payload)
