from tools.unity_http import post_to_unity


def register_report_tools(mcp):
    @mcp.tool()
    def export_effect_report(
        object_name: str,
        file_name: str = ""
    ) -> dict:
        """Export a detailed report of an effect GameObject to a JSON file.

        Report is saved to Assets/AI_Generated/Reports/{file_name}.json

        Parameters:
        - object_name: Name of the GameObject to report on.
        - file_name: Optional report file name. Defaults to '{object_name}_report.json'.
        """
        payload = {
            "objectName": object_name,
            "fileName": file_name
        }
        return post_to_unity("/export-effect-report", payload)
