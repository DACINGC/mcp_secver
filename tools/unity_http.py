import requests
from config import UNITY_BASE_URL, HTTP_TIMEOUT


def get_from_unity(endpoint: str) -> dict:
    url = f"{UNITY_BASE_URL}{endpoint}"
    try:
        response = requests.get(url, timeout=HTTP_TIMEOUT)
        response.raise_for_status()
        return response.json()
    except Exception as e:
        return {"success": False, "message": str(e)}


def post_to_unity(endpoint: str, payload: dict) -> dict:
    url = f"{UNITY_BASE_URL}{endpoint}"
    try:
        response = requests.post(url, json=payload, timeout=HTTP_TIMEOUT)
        response.raise_for_status()
        return response.json()
    except Exception as e:
        return {"success": False, "message": str(e)}
