from mcp.server.fastmcp import FastMCP

from tools.connection_tools import register_connection_tools
from tools.scene_tools import register_scene_tools
from tools.vfx_tools import register_vfx_tools
from tools.prefab_tools import register_prefab_tools
from tools.material_tools import register_material_tools
from tools.preview_tools import register_preview_tools
from tools.template_tools import register_template_tools
from tools.asset_tools import register_asset_tools
from tools.tuning_tools import register_tuning_tools
from tools.variant_tools import register_variant_tools
from tools.shader_tools import register_shader_tools
from tools.report_tools import register_report_tools

mcp = FastMCP("unity-mcp-server")

register_connection_tools(mcp)
register_scene_tools(mcp)
register_vfx_tools(mcp)
register_prefab_tools(mcp)
register_material_tools(mcp)
register_preview_tools(mcp)
register_template_tools(mcp)
register_asset_tools(mcp)
register_tuning_tools(mcp)
register_variant_tools(mcp)
register_shader_tools(mcp)
register_report_tools(mcp)

if __name__ == "__main__":
    mcp.run()
