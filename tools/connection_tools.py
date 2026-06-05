from tools.unity_http import get_from_unity


def register_connection_tools(mcp):
    @mcp.tool()
    def ping_unity() -> dict:
        """Check if the Unity HTTP server is reachable."""
        return get_from_unity("/ping")
