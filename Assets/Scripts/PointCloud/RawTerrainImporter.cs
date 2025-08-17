using UnityEngine;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PowerlineSystem
{
    /// <summary>
    /// 从16-bit RAW文件和元数据生成Unity Terrain的简易导入器。
    /// 使用：将RAW和_metadata.json放到 StreamingAssets/extract/，在Editor中运行 ImportRawToTerrain。
    /// </summary>
    public class RawTerrainImporter : MonoBehaviour
    {
        public string rawFileName = ""; // 不含路径，仅文件名，例如 "sample"
        public string metadataFileName = ""; // 可选
        public float terrainScale = 1.0f; // 额外的高度缩放

        // 在运行时创建地形
        public void CreateTerrainFromStreamingAssets()
        {
            string streamingDir = Path.Combine(Application.streamingAssetsPath, "extract");
            if (!Directory.Exists(streamingDir))
            {
                Debug.LogError($"找不到目录: {streamingDir}");
                return;
            }

            string rawPath = Path.Combine(streamingDir, rawFileName + ".raw");
            string metaPath = Path.Combine(streamingDir, metadataFileName);

            if (!File.Exists(rawPath))
            {
                Debug.LogError($"找不到 RAW 文件: {rawPath}");
                return;
            }

            // 读取元数据以获取宽高和高度范围（如果存在）
            int width = 0, height = 0;
            float minH = 0f, maxH = 0f;
            bool metaOk = false;
            if (!string.IsNullOrEmpty(metadataFileName) && File.Exists(metaPath))
            {
                try
                {
                    string json = File.ReadAllText(metaPath);
                    var dict = JsonUtility.FromJson<MetaWrapper>(json);
                    if (dict != null && dict.terrain_metadata != null)
                    {
                        width = dict.terrain_metadata.heightmapWidth;
                        height = dict.terrain_metadata.heightmapHeight;
                        minH = dict.terrain_metadata.min_elevation;
                        maxH = dict.terrain_metadata.max_elevation;
                        metaOk = width > 0 && height > 0;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"读取metadata失败: {e.Message}");
                }
            }

            if (!metaOk)
            {
                Debug.LogWarning("未使用metadata，尝试从文件名或默认方式推断分辨率。您应当提供 metadata JSON。\n默认将尝试使用512x512。");
                width = 512; height = 512;
                minH = 0; maxH = 10;
            }

            // 读取 raw 数据（假定为little-endian 16-bit unsigned, 行优先，width x height）
            byte[] rawBytes = File.ReadAllBytes(rawPath);
            if (rawBytes.Length < width * height * 2)
            {
                Debug.LogError($"RAW 文件大小 {rawBytes.Length} 小于预期 {width * height * 2}");
                return;
            }

            // 将 raw 数据转换为高度浮点数组
            float[,] heightsOrig = new float[height, width]; // [y,x]
            ushort[] values = new ushort[width * height];
            Buffer.BlockCopy(rawBytes, 0, values, 0, width * height * 2);

            // 归一化为 0..1
            ushort minVal = ushort.MaxValue, maxVal = ushort.MinValue;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] < minVal) minVal = values[i];
                if (values[i] > maxVal) maxVal = values[i];
            }
            float range = Mathf.Max(1, (float)(maxVal - minVal));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x; // 数据写入为 heightmap_16bit.T.tobytes() -> 行优先为 (y,x) over transposed array
                    if (idx < 0 || idx >= values.Length) continue;
                    ushort val = values[idx];
                    float normalized = (val - minVal) / range;
                    float realHeight = (maxH - minH) * normalized + minH;
                    heightsOrig[y, x] = (maxH - minH) > 0f ? (realHeight * terrainScale) / (maxH - minH) : 0f;
                }
            }

            // Unity 要求 heightmap 为 square (heightmapResolution x heightmapResolution)
            int res = Mathf.Max(width, height);
            float[,] heights = new float[res, res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    int srcX = Mathf.FloorToInt(x * (width / (float)res));
                    int srcY = Mathf.FloorToInt(y * (height / (float)res));
                    srcX = Mathf.Clamp(srcX, 0, width - 1);
                    srcY = Mathf.Clamp(srcY, 0, height - 1);
                    heights[y, x] = heightsOrig[srcY, srcX];
                }
            }

            // 创建 TerrainData
            TerrainData td = new TerrainData();
            td.heightmapResolution = Mathf.Max(2, res);
            td.size = new Vector3(width, (maxH - minH) * terrainScale, height);
            td.SetHeights(0, 0, heights);

            // 创建 GameObject
            GameObject terrainGO = Terrain.CreateTerrainGameObject(td);
            terrainGO.name = rawFileName + "_Terrain";
            Debug.Log($"已创建地形: {terrainGO.name}");
        }

        /// <summary>
        /// 一步完成：从 StreamingAssets 读取 RAW/metadata 创建地形，并从 Resources 加载 CSV 放置塔点
        /// </summary>
        public void ImportAndPlace(string rawBaseName, string metadataFileNameInStreaming, string csvResourceName)
        {
            if (string.IsNullOrEmpty(rawBaseName))
            {
                Debug.LogError("rawBaseName 为空");
                return;
            }

            this.rawFileName = rawBaseName;
            this.metadataFileName = metadataFileNameInStreaming;
            CreateTerrainFromStreamingAssets();

            if (!string.IsNullOrEmpty(csvResourceName))
            {
                LoadPowerlineCsvFromResources(csvResourceName);
            }
        }

        /// <summary>
        /// 从 Resources 读取 CSV 并在场景中创建简单标记（球体）来显示塔位
        /// CSV 假定每行包含至少三个数值列：x,y,z（以逗号分隔，首行可为表头）
        /// 将 CSV 的 x->Unity.x, y->Unity.z, z->Unity.y
        /// </summary>
        public void LoadPowerlineCsvFromResources(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogWarning("CSV resource name is empty");
                return;
            }

            TextAsset ta = Resources.Load<TextAsset>(Path.GetFileNameWithoutExtension(resourceName));
            if (ta == null)
            {
                Debug.LogWarning($"未在 Resources 找到 CSV: {resourceName}");
                return;
            }

            string[] lines = ta.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return;

            GameObject container = new GameObject(resourceName + "_Markers");
            int created = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                string[] parts = line.Split(',');
                // attempt parse first three numeric values
                float x, y, z;
                int startIdx = 0;
                // skip header if non-numeric
                if (parts.Length < 3) continue;
                if (!float.TryParse(parts[0], out x) || !float.TryParse(parts[1], out y) || !float.TryParse(parts[2], out z))
                {
                    // maybe header line; skip
                    continue;
                }

                Vector3 pos = new Vector3(x, z, y);
                GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.transform.position = pos;
                s.transform.localScale = Vector3.one * 1.0f; // 默认1米
                s.name = $"Tower_{created}";
                s.transform.SetParent(container.transform);
                created++;
            }

            Debug.Log($"已创建 {created} 个塔标记 (Resource: {resourceName})");
        }

        [Serializable]
        private class MetaWrapper
        {
            public TerrainMeta terrain_metadata;
        }

        [Serializable]
        private class TerrainMeta
        {
            public int heightmapWidth;
            public int heightmapHeight;
            public float terrainWorldWidth;
            public float terrainWorldLength;
            public float terrainWorldHeight;
            public float min_elevation;
            public float max_elevation;
        }
    }
}
