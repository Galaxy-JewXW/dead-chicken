using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PowerlineSystem
{
    /// <summary>
    /// 性能模式枚举
    /// </summary>
    public enum PerformanceMode
    {
        Fast,       // 快速模式 - 优先速度
        Balanced,   // 平衡模式 - 速度和质量平衡
        Quality     // 质量模式 - 优先质量
    }
    
    /// <summary>
    /// 电力线点云管理器
    /// 专门为电力线可视化系统设计的点云加载和管理组件
    /// 支持多种点云格式，与电力线系统无缝集成
    /// </summary>
    public class PowerlinePointCloudManager : MonoBehaviour
    {
        [Header("点云数据配置")]
        [Tooltip("点云数据文件路径（不包含扩展名）")]
        public string dataPath = "/PointCloud/powerline_scan";
        
        [Tooltip("是否在Start时自动加载点云")]
        public bool autoLoadOnStart = true;
        
        [Tooltip("点云材质")]
        public Material pointCloudMaterial;
        
        [Header("渲染设置")]
        [Tooltip("点云缩放比例")]
        public float scale = 1f;
        
        [Tooltip("是否反转Y和Z轴")]
        public bool invertYZ = false;
        
        [Tooltip("强制重新加载点云")]
        public bool forceReload = false;
        
        [Tooltip("点云透明度")]
        [Range(0f, 1f)]
        public float opacity = 1f;
        
        [Header("性能优化")]
        [Tooltip("每个网格的最大点数")]
        public int maxPointsPerMesh = 100000; // 增加每个网格的点数，减少网格数量
        
        [Tooltip("启用LOD系统")]
        public bool enableLOD = false; // 默认禁用LOD以提高性能
        
        [Tooltip("LOD距离阈值")]
        public float[] lodDistances = { 100f, 500f, 1000f };
        
        [Tooltip("性能模式")]
        public PerformanceMode performanceMode = PerformanceMode.Balanced;
        
        [Tooltip("快速加载模式 - 跳过颜色计算")]
        public bool fastLoadingMode = false;
        
        [Header("电力线系统集成")]
        [Tooltip("场景初始化器引用")]
        public SceneInitializer sceneInitializer;
        
        [Tooltip("是否自动适配电力线区域")]
        public bool autoFitToPowerlines = false;
        
        [Tooltip("点云偏移量")]
        public Vector3 pointCloudOffset = Vector3.zero;
        
        [Header("UI集成")]
        [Tooltip("是否显示加载进度")]
        public bool showLoadingProgress = true;
        
        [Tooltip("是否在UI中显示点云控制")]
        public bool enableUIControls = true;
        
        // 私有变量
        private GameObject pointCloudContainer;
        private List<GameObject> pointCloudMeshes = new List<GameObject>();
        private bool isLoaded = false;
        private bool isLoading = false;
        private float loadingProgress = 0f;
        private string loadingStatus = "";
        
        // 点云数据
        private int totalPoints;
        private int totalMeshGroups;
        private Vector3[] points;
        private Color[] colors;
        private Vector3 boundsMin;
        private Vector3 boundsMax;
        private Vector3 boundsCenter;
        
        // 性能监控
        private int renderedPoints = 0;
        private float lastUpdateTime = 0f;
        
        // 事件
        public System.Action<float> OnLoadingProgress;
        public System.Action OnLoadingComplete;
        public System.Action<string> OnLoadingError;
        
        void Start()
        {
            InitializePointCloudSystem();
        }
        
        void InitializePointCloudSystem()
        {
            Debug.Log($"PowerlinePointCloudManager初始化，GameObject: {gameObject.name}, dataPath: '{dataPath}'");
            
            // 创建点云容器
            if (pointCloudContainer == null)
            {
                pointCloudContainer = new GameObject("PointCloudContainer");
                pointCloudContainer.transform.SetParent(transform);
                
                // 设置点云专用层级（31层）
                pointCloudContainer.layer = 31;
            }
            
            // 创建默认材质
            if (pointCloudMaterial == null)
            {
                CreateDefaultMaterial();
            }
            
            // 只有当启用自动加载且dataPath不为空且不是默认的无效路径时才自动加载点云
            if (autoLoadOnStart && !string.IsNullOrEmpty(dataPath) && dataPath != "pointcloud/sample")
            {
                Debug.Log($"自动加载点云，dataPath: '{dataPath}'");
                LoadPointCloudAsync();
            }
            else
            {
                Debug.Log($"跳过自动加载点云，autoLoadOnStart: {autoLoadOnStart}, dataPath: '{dataPath}'");
            }
        }
        
        void CreateDefaultMaterial()
        {
            // 尝试加载Unity-Point-Cloud-Free-Viewer的着色器
            Shader pointShader = Shader.Find("Custom/VertexColor");
            if (pointShader == null)
            {
                // 如果没有找到，使用标准着色器
                pointShader = Shader.Find("Standard");
            }
            
            pointCloudMaterial = new Material(pointShader);
            pointCloudMaterial.name = "PowerlinePointCloudMaterial";
            
            // 设置材质属性
            if (pointCloudMaterial.HasProperty("_Color"))
            {
                pointCloudMaterial.SetColor("_Color", Color.white);
            }
            
            if (pointCloudMaterial.HasProperty("_Metallic"))
            {
                pointCloudMaterial.SetFloat("_Metallic", 0f);
            }
            
            if (pointCloudMaterial.HasProperty("_Smoothness"))
            {
                pointCloudMaterial.SetFloat("_Smoothness", 0f);
            }
        }
        
        /// <summary>
        /// 异步加载点云数据
        /// </summary>
        public void LoadPointCloudAsync()
        {
            if (isLoading)
            {
                Debug.LogWarning("点云正在加载中，忽略重复加载请求");
                return;
            }
            
            // 清理之前的加载状态，允许重新加载
            if (isLoaded)
            {
                Debug.Log("清理之前的点云数据，准备重新加载");
                ClearPointCloudMeshes();
                isLoaded = false;
                totalPoints = 0;
                totalMeshGroups = 0;
            }
            
            // 强制刷新资源系统（用于新转换的文件）
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
            
            StartCoroutine(LoadPointCloudCoroutine());
        }
        
        IEnumerator LoadPointCloudCoroutine()
        {
            isLoading = true;
            loadingProgress = 0f;
            loadingStatus = "初始化点云加载...";
            
            // 检查是否已有缓存
            string cacheFolder = Application.dataPath + "/Resources/PointCloudMeshes/" + Path.GetFileName(dataPath);
            
            if (!Directory.Exists(cacheFolder) || forceReload)
            {
                // 需要重新加载和处理点云数据
                yield return StartCoroutine(ProcessPointCloudData());
            }
            else
            {
                // 加载缓存的点云
                yield return StartCoroutine(LoadCachedPointCloud());
            }
            
            // 应用电力线系统集成 - 已禁用自动适配
            // if (autoFitToPowerlines && sceneInitializer != null)
            // {
            //     FitPointCloudToPowerlines();
            // }
            
            isLoading = false;
            isLoaded = true;
            OnLoadingComplete?.Invoke();
            
            Debug.Log($"点云加载完成: {totalPoints} 个点, {totalMeshGroups} 个网格组");
        }
        
        IEnumerator ProcessPointCloudData()
        {
            loadingStatus = "读取点云文件...";
            Debug.Log($"PowerlinePointCloudManager正在处理数据，GameObject: {gameObject.name}, dataPath = '{dataPath}'");
            
            // 首先尝试从Resources文件夹读取
            string resourcePath = dataPath.TrimStart('/');
            Debug.Log($"尝试从Resources加载: {resourcePath}");
            
            TextAsset offFile = Resources.Load<TextAsset>(resourcePath);
            
            if (offFile != null)
            {
                Debug.Log($"从Resources成功加载文件: {resourcePath}");
                // 从Resources读取
                yield return StartCoroutine(ReadOFFFromTextAsset(offFile));
            }
            else
            {
                Debug.Log($"Resources中未找到文件: {resourcePath}，尝试文件系统");
                
                // 尝试从文件系统读取OFF文件
                string basePath = Application.dataPath + "/Resources/" + dataPath;
                
                // 确保文件路径包含.off扩展名
                string offFilePath = basePath.EndsWith(".off") ? basePath : basePath + ".off";
                
                Debug.Log($"查找OFF文件路径: {offFilePath}");
                
                if (!File.Exists(offFilePath))
                {
                    string errorMsg = $"点云文件不存在: Resources/{resourcePath}.off 或 {offFilePath}";
                    Debug.LogWarning(errorMsg); // 改为Warning而不是Error
                    Debug.LogWarning($"当前管理器dataPath: '{dataPath}', GameObject: {gameObject.name}");
                    OnLoadingError?.Invoke(errorMsg);
                    yield break;
                }
                
                Debug.Log($"从文件系统加载: {offFilePath}");
                // 读取OFF文件
                yield return StartCoroutine(ReadOFFFile(offFilePath));
            }
            
            // 检查是否成功读取了点云数据
            if (totalPoints <= 0)
            {
                string errorMsg = "点云文件读取失败或文件为空";
                Debug.LogError(errorMsg);
                OnLoadingError?.Invoke(errorMsg);
                yield break;
            }
            
            // 创建网格
            yield return StartCoroutine(CreatePointCloudMeshes());
            
            // 保存缓存
            SavePointCloudCache();
        }
        
        IEnumerator ReadOFFFromTextAsset(TextAsset textAsset)
        {
            loadingStatus = "解析点云数据...";
            
            // 使用更高效的字符串分割
            string[] lines = textAsset.text.Split('\n');
            int lineIndex = 0;
            
            // 读取文件头
            if (lineIndex >= lines.Length || lines[lineIndex].Trim() != "OFF")
            {
                Debug.LogError("不是有效的OFF文件格式");
                yield break;
            }
            lineIndex++;
            
            // 读取点数和面数
            if (lineIndex >= lines.Length)
            {
                Debug.LogError("OFF文件格式错误：缺少点数信息");
                yield break;
            }
            
            string[] counts = lines[lineIndex].Trim().Split(' ');
            totalPoints = int.Parse(counts[0]);
            lineIndex++;
            
            Debug.Log($"准备读取 {totalPoints} 个点云数据");
            
            // 初始化数组
            points = new Vector3[totalPoints];
            colors = new Color[totalPoints];
            boundsMin = Vector3.positiveInfinity;
            boundsMax = Vector3.negativeInfinity;
            
            // 简化批量处理 - 使用固定的大批量
            int batchSize = 20000; // 大幅增加批量大小，减少yield频率
            int processedPoints = 0;
            
            // 读取点数据 - 批量处理
            for (int i = 0; i < totalPoints && lineIndex < lines.Length; i++)
            {
                string line = lines[lineIndex].Trim();
                if (string.IsNullOrEmpty(line))
                {
                    i--; // 跳过空行
                    lineIndex++;
                    continue;
                }
                
                string[] data = line.Split(' ');
                
                // 解析位置
                float x = float.Parse(data[0]) * scale;
                float y = float.Parse(data[1]) * scale;
                float z = float.Parse(data[2]) * scale;
                
                if (invertYZ)
                {
                    points[i] = new Vector3(x, z, y);
                }
                else
                {
                    points[i] = new Vector3(x, y, z);
                }
                
                // 更新边界
                boundsMin = Vector3.Min(boundsMin, points[i]);
                boundsMax = Vector3.Max(boundsMax, points[i]);
                
                // 解析颜色 - 简化颜色处理
                if (data.Length >= 6)
                {
                    float r = int.Parse(data[3]) / 255f;
                    float g = int.Parse(data[4]) / 255f;
                    float b = int.Parse(data[5]) / 255f;
                    colors[i] = new Color(r, g, b, opacity);
                }
                else
                {
                    // 使用简单的默认颜色，避免后续重新计算
                    colors[i] = new Color(0.5f, 0.7f, 1f, opacity);
                }
                
                processedPoints++;
                
                // 批量更新进度 - 减少yield频率
                if (processedPoints % batchSize == 0)
                {
                    loadingProgress = (float)i / totalPoints * 0.5f; // 读取占50%进度
                    loadingStatus = $"读取点云数据: {i}/{totalPoints}";
                    OnLoadingProgress?.Invoke(loadingProgress);
                    yield return null;
                }
                
                lineIndex++;
            }
            
            // 计算边界中心
            boundsCenter = (boundsMin + boundsMax) * 0.5f;
            
            // 简化颜色处理 - 直接使用统一颜色，跳过复杂的颜色计算
            Color uniformColor = new Color(0.5f, 0.7f, 1f, opacity);
            for (int i = 0; i < totalPoints; i++)
            {
                if (colors[i].r == 0.5f && colors[i].g == 0.7f && colors[i].b == 1f)
                {
                    colors[i] = uniformColor;
                }
            }
            
            // 简化偏移处理 - 直接应用，不进行批量更新
            if (pointCloudOffset != Vector3.zero)
            {
                for (int i = 0; i < totalPoints; i++)
                {
                    points[i] += pointCloudOffset;
                }
            }
            
            Debug.Log($"点云数据读取完成: {totalPoints} 个点");
            Debug.Log($"点云边界: Min={boundsMin}, Max={boundsMax}, Center={boundsCenter}");
        }
        
        IEnumerator ReadOFFFile(string filePath)
        {
            loadingStatus = "解析点云数据...";
            
            using (StreamReader reader = new StreamReader(filePath))
            {
                // 读取文件头
                string header = reader.ReadLine(); // OFF
                if (header != "OFF")
                {
                    Debug.LogError("不是有效的OFF文件格式");
                    yield break;
                }
                
                // 读取点数和面数
                string[] counts = reader.ReadLine().Split(' ');
                totalPoints = int.Parse(counts[0]);
                
                // 初始化数组
                points = new Vector3[totalPoints];
                colors = new Color[totalPoints];
                boundsMin = Vector3.positiveInfinity;
                boundsMax = Vector3.negativeInfinity;
                
                // 读取点数据
                for (int i = 0; i < totalPoints; i++)
                {
                    string[] data = reader.ReadLine().Split(' ');
                    
                    // 解析位置
                    float x = float.Parse(data[0]) * scale;
                    float y = float.Parse(data[1]) * scale;
                    float z = float.Parse(data[2]) * scale;
                    
                    if (invertYZ)
                    {
                        points[i] = new Vector3(x, z, y);
                    }
                    else
                    {
                        points[i] = new Vector3(x, y, z);
                    }
                    
                    // 更新边界
                    boundsMin = Vector3.Min(boundsMin, points[i]);
                    boundsMax = Vector3.Max(boundsMax, points[i]);
                    
                    // 解析颜色
                    if (data.Length >= 6)
                    {
                        float r = int.Parse(data[3]) / 255f;
                        float g = int.Parse(data[4]) / 255f;
                        float b = int.Parse(data[5]) / 255f;
                        colors[i] = new Color(r, g, b, opacity);
                    }
                    else
                    {
                        colors[i] = new Color(0.5f, 0.7f, 1f, opacity); // 默认蓝色
                    }
                    
                    // 更新进度
                    if (i % 1000 == 0)
                    {
                        loadingProgress = (float)i / totalPoints * 0.5f; // 读取占50%进度
                        loadingStatus = $"读取点云数据: {i}/{totalPoints}";
                        OnLoadingProgress?.Invoke(loadingProgress);
                        yield return null;
                    }
                }
                
                // 计算边界中心
                boundsCenter = (boundsMin + boundsMax) * 0.5f;
                
                // 应用偏移
                if (pointCloudOffset != Vector3.zero)
                {
                    for (int i = 0; i < totalPoints; i++)
                    {
                        points[i] += pointCloudOffset;
                    }
                }
            }
            
            Debug.Log($"点云数据读取完成: {totalPoints} 个点");
            Debug.Log($"点云边界: Min={boundsMin}, Max={boundsMax}, Center={boundsCenter}");
        }
        
        IEnumerator CreatePointCloudMeshes()
        {
            loadingStatus = "创建点云网格...";
            
            // 计算需要的网格数量
            totalMeshGroups = Mathf.CeilToInt((float)totalPoints / maxPointsPerMesh);
            
            // 清理现有网格
            ClearPointCloudMeshes();
            
            // 简化网格创建 - 减少yield频率
            for (int groupIndex = 0; groupIndex < totalMeshGroups; groupIndex++)
            {
                int startIndex = groupIndex * maxPointsPerMesh;
                int pointCount = Mathf.Min(maxPointsPerMesh, totalPoints - startIndex);
                
                GameObject meshObject = CreateMeshGroup(groupIndex, startIndex, pointCount);
                pointCloudMeshes.Add(meshObject);
                
                // 更新进度
                loadingProgress = 0.7f + (float)groupIndex / totalMeshGroups * 0.3f;
                loadingStatus = $"创建网格组: {groupIndex + 1}/{totalMeshGroups}";
                OnLoadingProgress?.Invoke(loadingProgress);
                
                // 大幅减少yield频率 - 每10个网格才暂停一次
                if (groupIndex % 10 == 0)
                {
                    yield return null;
                }
            }
            
            Debug.Log($"点云网格创建完成: {totalMeshGroups} 个网格组");
        }
        
        GameObject CreateMeshGroup(int groupIndex, int startIndex, int pointCount)
        {
            // 创建网格对象
            GameObject meshObject = new GameObject($"PointCloudMesh_{groupIndex}");
            meshObject.transform.SetParent(pointCloudContainer.transform);
            
            // 设置点云专用层级（31层）- 确保查看器能够显示
            meshObject.layer = 31;
            
            // 添加组件
            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
            
            // 设置材质
            meshRenderer.material = pointCloudMaterial;
            
            // 创建网格
            Mesh mesh = new Mesh();
            mesh.name = $"PointCloudMesh_{groupIndex}";
            
            // 准备顶点数据 - 优化内存分配
            Vector3[] vertices = new Vector3[pointCount];
            Color[] vertexColors = new Color[pointCount];
            int[] indices = new int[pointCount];
            
            // 批量处理顶点数据
            for (int i = 0; i < pointCount; i++)
            {
                vertices[i] = points[startIndex + i] - boundsCenter; // 相对于中心的位置
                vertexColors[i] = colors[startIndex + i];
                indices[i] = i;
            }
            
            // 设置网格数据
            mesh.vertices = vertices;
            mesh.colors = vertexColors;
            mesh.SetIndices(indices, MeshTopology.Points, 0);
            
            // 设置边界
            mesh.RecalculateBounds();
            
            // 应用网格
            meshFilter.mesh = mesh;
            
            // 简化LOD支持 - 只在需要时添加
            if (enableLOD && pointCount > 1000) // 只对大型网格添加LOD
            {
                AddLODSupport(meshObject, groupIndex);
            }
            
            return meshObject;
        }
        
        void AddLODSupport(GameObject meshObject, int groupIndex)
        {
            // 为网格添加LOD组件
            LODGroup lodGroup = meshObject.AddComponent<LODGroup>();
            
            // 简化LOD级别 - 只使用2个级别提高性能
            LOD[] lods = new LOD[2];
            
            // 原始网格作为最高LOD
            Renderer[] renderers = { meshObject.GetComponent<Renderer>() };
            lods[0] = new LOD(0.5f, renderers); // 50%距离显示完整网格
            
            // 创建简化LOD级别 - 只创建一个简化版本
            GameObject simplifiedMesh = CreateSimplifiedMesh(meshObject, groupIndex);
            if (simplifiedMesh != null)
            {
                Renderer[] simplifiedRenderers = { simplifiedMesh.GetComponent<Renderer>() };
                lods[1] = new LOD(0.1f, simplifiedRenderers); // 10%距离显示简化网格
            }
            else
            {
                // 如果简化失败，使用原始网格
                lods[1] = new LOD(0.1f, renderers);
            }
            
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }
        
        // 删除复杂的性能模式方法，使用简化的固定参数
        
        /// <summary>
        /// 创建简化的网格用于LOD
        /// </summary>
        GameObject CreateSimplifiedMesh(GameObject originalMesh, int groupIndex)
        {
            try
            {
                MeshFilter originalFilter = originalMesh.GetComponent<MeshFilter>();
                if (originalFilter == null || originalFilter.mesh == null)
                    return null;
                
                Mesh originalMeshData = originalFilter.mesh;
                int originalPointCount = originalMeshData.vertexCount;
                
                // 如果点数太少，不需要简化
                if (originalPointCount < 1000)
                    return null;
                
                // 创建简化网格对象
                GameObject simplifiedMesh = new GameObject($"PointCloudMesh_{groupIndex}_LOD");
                simplifiedMesh.transform.SetParent(originalMesh.transform);
                simplifiedMesh.layer = 31;
                
                // 添加组件
                MeshFilter simplifiedFilter = simplifiedMesh.AddComponent<MeshFilter>();
                MeshRenderer simplifiedRenderer = simplifiedMesh.AddComponent<MeshRenderer>();
                
                // 设置材质
                simplifiedRenderer.material = pointCloudMaterial;
                
                // 创建简化网格 - 采样50%的点
                int simplifiedPointCount = originalPointCount / 2;
                Vector3[] originalVertices = originalMeshData.vertices;
                Color[] originalColors = originalMeshData.colors;
                
                Vector3[] simplifiedVertices = new Vector3[simplifiedPointCount];
                Color[] simplifiedColors = new Color[simplifiedPointCount];
                int[] simplifiedIndices = new int[simplifiedPointCount];
                
                for (int i = 0; i < simplifiedPointCount; i++)
                {
                    int originalIndex = i * 2; // 每隔一个点采样
                    simplifiedVertices[i] = originalVertices[originalIndex];
                    simplifiedColors[i] = originalColors[originalIndex];
                    simplifiedIndices[i] = i;
                }
                
                // 创建简化网格
                Mesh simplifiedMeshData = new Mesh();
                simplifiedMeshData.name = $"PointCloudMesh_{groupIndex}_LOD";
                simplifiedMeshData.vertices = simplifiedVertices;
                simplifiedMeshData.colors = simplifiedColors;
                simplifiedMeshData.SetIndices(simplifiedIndices, MeshTopology.Points, 0);
                simplifiedMeshData.RecalculateBounds();
                
                // 应用网格
                simplifiedFilter.mesh = simplifiedMeshData;
                
                return simplifiedMesh;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"创建简化网格失败: {ex.Message}");
                return null;
            }
        }
        
        IEnumerator LoadCachedPointCloud()
        {
            loadingStatus = "加载缓存点云...";
            
            string cacheFolder = Application.dataPath + "/Resources/PointCloudMeshes/" + Path.GetFileName(dataPath);
            string prefabPath = "PointCloudMeshes/" + Path.GetFileName(dataPath);
            
            // 加载预制体
            GameObject cachedPrefab = Resources.Load<GameObject>(prefabPath);
            if (cachedPrefab != null)
            {
                pointCloudContainer = Instantiate(cachedPrefab, transform);
                pointCloudContainer.name = "PointCloudContainer";
                
                // 设置容器的层级
                pointCloudContainer.layer = 31;
                
                // 获取所有网格
                MeshRenderer[] renderers = pointCloudContainer.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in renderers)
                {
                    pointCloudMeshes.Add(renderer.gameObject);
                    
                    // 设置点云专用层级（31层）- 确保查看器能够显示
                    renderer.gameObject.layer = 31;
                    
                    // 更新材质
                    if (pointCloudMaterial != null)
                    {
                        renderer.material = pointCloudMaterial;
                    }
                }
                
                totalMeshGroups = pointCloudMeshes.Count;
                Debug.Log($"从缓存加载点云: {totalMeshGroups} 个网格组");
            }
            else
            {
                Debug.LogWarning("无法加载缓存的点云，将重新处理");
                yield return StartCoroutine(ProcessPointCloudData());
            }
            
            yield return null;
        }
        
        void SavePointCloudCache()
        {
            if (pointCloudContainer == null) return;
            
            // 创建缓存目录
            string cacheFolder = "Assets/Resources/PointCloudMeshes";
            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }
            
            string fileName = Path.GetFileName(dataPath);
            string cachePath = cacheFolder + "/" + fileName;
            
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }
            
            // 保存网格资源
            for (int i = 0; i < pointCloudMeshes.Count; i++)
            {
                MeshFilter meshFilter = pointCloudMeshes[i].GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    string meshPath = $"{cachePath}/{fileName}_{i}.asset";
                    #if UNITY_EDITOR
                    UnityEditor.AssetDatabase.CreateAsset(meshFilter.sharedMesh, meshPath);
                    #endif
                }
            }
            
            // 保存预制体
            string prefabPath = $"{cachePath}.prefab";
            #if UNITY_EDITOR
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(pointCloudContainer, prefabPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            #endif
            
            Debug.Log($"点云缓存已保存到: {prefabPath}");
        }
        
        void ClearPointCloudMeshes()
        {
            foreach (var meshObject in pointCloudMeshes)
            {
                if (meshObject != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(meshObject);
                    }
                    else
                    {
                        DestroyImmediate(meshObject);
                    }
                }
            }
            pointCloudMeshes.Clear();
        }
        
        /// <summary>
        /// 将点云适配到电力线区域
        /// </summary>
        void FitPointCloudToPowerlines()
        {
            if (sceneInitializer == null || pointCloudContainer == null) return;
            
            // 获取电力线边界
            Bounds powerlineBounds = GetPowerlineBounds();
            
            // 计算缩放和偏移
            Vector3 targetCenter = powerlineBounds.center;
            Vector3 currentCenter = boundsCenter;
            
            // 应用变换
            Vector3 offset = targetCenter - currentCenter;
            pointCloudContainer.transform.position += offset;
            
            Debug.Log($"点云已适配到电力线区域: 偏移 {offset}");
        }
        
        Bounds GetPowerlineBounds()
        {
            Bounds bounds = new Bounds();
            bool boundsInitialized = false;
            
            if (sceneInitializer != null && sceneInitializer.powerlines != null)
            {
                foreach (var powerline in sceneInitializer.powerlines)
                {
                    foreach (var point in powerline.points)
                    {
                        if (!boundsInitialized)
                        {
                            bounds = new Bounds(point, Vector3.zero);
                            boundsInitialized = true;
                        }
                        else
                        {
                            bounds.Encapsulate(point);
                        }
                    }
                }
            }
            
            return bounds;
        }
        
        /// <summary>
        /// 设置点云可见性
        /// </summary>
        public void SetPointCloudVisible(bool visible)
        {
            if (pointCloudContainer != null)
            {
                pointCloudContainer.SetActive(visible);
            }
        }
        
        /// <summary>
        /// 设置点云透明度
        /// </summary>
        public void SetPointCloudOpacity(float newOpacity)
        {
            opacity = Mathf.Clamp01(newOpacity);
            
            if (pointCloudMaterial != null)
            {
                Color color = pointCloudMaterial.color;
                color.a = opacity;
                pointCloudMaterial.color = color;
            }
        }
        
        /// <summary>
        /// 获取点云统计信息
        /// </summary>
        public PointCloudStats GetPointCloudStats()
        {
            return new PointCloudStats
            {
                totalPoints = totalPoints,
                totalMeshGroups = totalMeshGroups,
                isLoaded = isLoaded,
                isLoading = isLoading,
                loadingProgress = loadingProgress,
                loadingStatus = loadingStatus,
                boundsMin = boundsMin,
                boundsMax = boundsMax,
                boundsCenter = boundsCenter,
                renderedPoints = renderedPoints
            };
        }
        
        void Update()
        {
            // 更新渲染统计
            if (Time.time - lastUpdateTime > 1f)
            {
                UpdateRenderStats();
                lastUpdateTime = Time.time;
            }
        }
        
        void UpdateRenderStats()
        {
            renderedPoints = 0;
            foreach (var meshObject in pointCloudMeshes)
            {
                if (meshObject != null && meshObject.activeInHierarchy)
                {
                    MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        renderedPoints += meshFilter.sharedMesh.vertexCount;
                    }
                }
            }
        }
        
        void OnGUI()
        {
            if (!showLoadingProgress || !isLoading) return;
            
            // 显示加载进度
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float barWidth = 400f;
            float barHeight = 30f;
            float x = (screenWidth - barWidth) * 0.5f;
            float y = screenHeight * 0.5f;
            
            GUI.Box(new Rect(x, y, barWidth, barHeight), loadingStatus);
            GUI.Box(new Rect(x, y, loadingProgress * barWidth, barHeight), "");
        }
        
        /// <summary>
        /// 上下文菜单：重新加载点云
        /// </summary>
        [ContextMenu("重新加载点云")]
        public void ReloadPointCloud()
        {
            if (isLoading) return;
            
            isLoaded = false;
            forceReload = true;
            ClearPointCloudMeshes();
            
            if (pointCloudContainer != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(pointCloudContainer);
                }
                else
                {
                    DestroyImmediate(pointCloudContainer);
                }
            }
            
            InitializePointCloudSystem();
        }
        
        /// <summary>
        /// 上下文菜单：清理点云缓存
        /// </summary>
        [ContextMenu("清理点云缓存")]
        public void ClearPointCloudCache()
        {
            string cacheFolder = Application.dataPath + "/Resources/PointCloudMeshes/" + Path.GetFileName(dataPath);
            if (Directory.Exists(cacheFolder))
            {
                Directory.Delete(cacheFolder, true);
                #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
                #endif
                Debug.Log("点云缓存已清理");
            }
        }
        
        void OnDestroy()
        {
            ClearPointCloudMeshes();
        }
    }
    
    /// <summary>
    /// 点云统计信息
    /// </summary>
    [System.Serializable]
    public struct PointCloudStats
    {
        public int totalPoints;
        public int totalMeshGroups;
        public bool isLoaded;
        public bool isLoading;
        public float loadingProgress;
        public string loadingStatus;
        public Vector3 boundsMin;
        public Vector3 boundsMax;
        public Vector3 boundsCenter;
        public int renderedPoints;
    }
} 