using System;
using System.Collections.Generic;

namespace UnityMCP
{
    [Serializable]
    public class RequestModel
    {
        public string effectName;
        public string name;
        public string objectName;
        public string color;
        public string prefabPath;
        public double duration;
        public double emissionRate;
        public double startLifetime;
        public double startSpeed;
        public double startSize;
        public double radius;
        public bool loop;
        public double intensity;
        public double range;
        public double x;
        public double y;
        public double z;
        public double rx;
        public double ry;
        public double rz;
        public double sx;
        public double sy;
        public double sz;

        public string materialName;
        public string shaderName;
        public string emissionColor;
        public double emissionIntensity;
        public string materialPath;

        public string mainColor;
        public bool saveAsPrefab;
        public double height;
        public int branchCount;
        public double density;
        public double width;
        public double length;

        public bool includeChildren;
        public bool clearParticles;
        public string fileName;
        public string viewType;
        public string templatePath;
        public string outputName;
        public double scale;
        public string assetType;
        public string prefix;
    }

    [Serializable]
    public class ResponseModel
    {
        public bool success;
        public string message;
        public string objectName;
        public string assetPath;
        public int affectedCount;
        public List<SceneObjectInfo> objects;
        public List<string> assetPaths;
        public ObjectInfo objectInfo;

        public ResponseModel()
        {
            success = true;
            message = "OK";
            objectName = null;
            assetPath = null;
            affectedCount = 0;
            objects = null;
            assetPaths = null;
            objectInfo = null;
        }
    }

    [Serializable]
    public class SceneObjectInfo
    {
        public string name;
        public string type;
        public double x;
        public double y;
        public double z;
    }

    [Serializable]
    public class Vector3Info
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class ObjectInfo
    {
        public string name;
        public bool activeSelf;
        public Vector3Info position;
        public Vector3Info rotation;
        public Vector3Info scale;
        public List<string> components;
        public List<ObjectInfo> children;
        public int particleSystemCount;
        public int lightCount;
        public int rendererCount;
    }
}
