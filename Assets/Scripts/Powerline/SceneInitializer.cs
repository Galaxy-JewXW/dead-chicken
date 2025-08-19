using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneInitializer : MonoBehaviour
{
    [Header("基本配置")]
    public string csvFileName = "simple_towers"; // 简化输入模式CSV文件名
    public GameObject powerlineParent;
    public float lineWidth = 0.2f;
    
    /// <summary>
    /// 设置CSV文件名（用于动态加载提取的CSV文件）
    /// </summary>
    /// <param name="fileName">CSV文件名（不包含.csv扩展名）</param>
    public void SetCsvFileName(string fileName)
    {
        csvFileName = fileName;
        Debug.Log($"[SceneInitializer] 已设置CSV文件名: {csvFileName}");
    }
    
    [Header("数据格式配置")]
    [Tooltip("选择CSV数据格式")]
    public CSVFormat csvFormat = CSVFormat.SimpleTowers;
    
    public enum CSVFormat
    {
        [Tooltip("simple_towers.csv 格式：x,y,z,height")]
        SimpleTowers,
        [Tooltip("tower_centers.csv 格式：x,z,height (y默认为0)")]
        TowerCenters,
        [Tooltip("B.csv 格式：group_id,order,x,y,z,line_count (X,Y为水平坐标，Z为高度)")] // 新增B.csv格式
        B
    }
    
    [Header("地形适配")]
    public TerrainManager terrainManager;
    public float powerlineHeightOffset = 15f;
    public float towerHeightOffset = 0f;
    public bool adaptToTerrain = true;
    
    [Header("电力线参数")]
    public float sagFactor = 0.3f;
    public int segmentsPerSpan = 20;
    public bool enablePhysicalSag = true;
    
    [Header("导线材质")]
    public Material conductorMaterial;
    public Material groundWireMaterial;
    public Color conductorColor = new Color(0.8f, 0.7f, 0.5f);
    public Color groundWireColor = new Color(0.6f, 0.6f, 0.6f);
    
    [Header("引脚连接系统")]
    [Tooltip("启用精确引脚连接")]
    public bool usePrecisePinConnection = true;
    [Tooltip("引脚系统组件")]
    public TowerPinpointSystem pinpointSystem;
    
    [Header("电塔配置")]
    [Tooltip("Unity中电塔模型的原始高度（米）")]
    public float baseTowerHeight = 2f;
    public GameObject towerPrefab;
    public float towerScale = 0.1f;
    
    [Header("树木配置")]
    [Tooltip("是否在建立电塔和电线的同时建立树木")]
    public bool enableTreePlacement = true;
    [Tooltip("树木预制体")]
    public GameObject treePrefab;
    [Tooltip("树木CSV文件名")]
    public string treeCsvFileName = "tree/trees";
    [Tooltip("是否启用树木自动缩放")]
    public bool enableTreeAutoScaling = true;
    [Tooltip("树木目标高度范围")]
    public Vector2 treeHeightRange = new Vector2(3f, 8f);
    [Tooltip("每个电塔周围的树木数量范围")]
    public Vector2Int treesPerTowerRange = new Vector2Int(3, 7);
    [Tooltip("树木距离电塔的最小距离")]
    public float minTreeDistanceFromTower = 3f;
    [Tooltip("树木距离电塔的最大距离")]
    public float maxTreeDistanceFromTower = 15f;
    [Tooltip("树木基础缩放倍数")]
    public float treeBaseScale = 50f;
    
    [Header("点云集成")]
    [Tooltip("点云管理器（可选）")]
    public PowerlineSystem.PowerlinePointCloudManager pointCloudManager;

    [System.Serializable]
    public class PowerlineInfo
    {
        public List<Vector3> points = new List<Vector3>();
        public List<Vector3> smoothPoints = new List<Vector3>();
        public float length;
        public int index;
        public Vector3 start, end;
        public GameObject lineObj;
        public string wireType = "Conductor"; // "Conductor", "GroundWire"
        public int wireIndex = 0;
        
        // 扩展属性用于UI显示
        public float voltage = 220f; // 电压等级(kV)
        public int wireCount = 3; // 导线数量
        public Vector3[] towerPositions; // 杆塔位置数组
        
        // 构造函数
        public PowerlineInfo()
        {
            towerPositions = new Vector3[0];
        }
    }
    
    [System.Serializable]
    public class SimpleTowerData
    {
        public Vector3 position;
        public float height;
        public int groupId; // 新增：group ID
        public int order;   // 新增：在group中的顺序
        
        public SimpleTowerData(Vector3 pos, float h)
        {
            position = pos;
            height = h;
            groupId = 0;
            order = 0;
        }
        
        public SimpleTowerData(Vector3 pos, float h, int group, int ord)
        {
            position = pos;
            height = h;
            groupId = group;
            order = ord;
        }
    }
    
    [System.Serializable]
    public class SimpleTreeData
    {
        public int treeId;
        public Vector3 position;
        public float height;
        public int groupId;
        public int towerId;
        public string treeType;
        public float scale;
        
        public SimpleTreeData(int id, Vector3 pos, float h, int group, int tower, string type, float s = 1.0f)
        {
            treeId = id;
            position = pos;
            height = h;
            groupId = group;
            towerId = tower;
            treeType = type;
            scale = s;
        }
    }
    
    public List<PowerlineInfo> powerlines = new List<PowerlineInfo>();
    private Dictionary<Vector3, float> towerHeights = new Dictionary<Vector3, float>();
    
    // 树木管理
    private List<GameObject> placedTrees = new List<GameObject>();

    [Header("初始化控制")]
    [Tooltip("是否在Start时自动初始化场景")]
    public bool autoInitializeOnStart = false;
    
    void Start()
    {
        // 只有在启用自动初始化时才执行
        if (autoInitializeOnStart)
        {
            InitializeScene();
        }
        else
        {
            Debug.Log("SceneInitializer: 自动初始化已禁用，等待手动调用");
        }
    }
    
    /// <summary>
    /// 手动初始化场景
    /// </summary>
    public void InitializeScene()
    {
        Debug.Log($"开始初始化场景,文件: {csvFileName}, 格式: {csvFormat}");
        
        // 确认格式设置
        if (csvFormat == CSVFormat.B)
        {
            Debug.Log("[SceneInitializer] ✅ 确认使用B.csv格式，将启用group分组连线功能");
        }
        else
        {
            Debug.Log($"[SceneInitializer] ℹ️ 使用格式: {csvFormat}，将使用顺序连线模式");
        }
        
        // 不再添加随机丘陵
        if (terrainManager != null)
        {
            Debug.Log("使用平坦地形");
        }
        
        CreateWireMaterials();
        
        // 1. 先创建实际电塔
        List<GameObject> physicalTowers = PlaceTowersFromSimplifiedInput();
        
        // 2. 基于实际电塔生成导线
        if (physicalTowers.Count >= 2)
        {
            GenerateWiresFromTowers(physicalTowers);
        }
        else
        {
            Debug.LogError("电塔数据不足，至少需要2个电塔来生成导线");
        }
        
        // 3. 计算导线垂度（如果需要）
        if (enablePhysicalSag)
        {
            ComputeSagForAllPowerlines();
        }
        
        // 4. 绘制导线
        DrawAllPowerlines();
        
        // 5. 初始化点云系统（如果配置了）
        InitializePointCloudSystem();
        
        // 6. 创建树木（如果启用）
        if (enableTreePlacement)
        {
            Debug.Log("[SceneInitializer] 正在创建树木...");
            CreateTreesFromCsv();
        }
        
        Debug.Log("电力线场景初始化完成");
    }
        
    void CreateWireMaterials()
    {
        if (conductorMaterial == null)
        {
            conductorMaterial = new Material(Shader.Find("Standard"));
            conductorMaterial.name = "ConductorMaterial";
            conductorMaterial.color = conductorColor;
            conductorMaterial.SetFloat("_Metallic", 0.8f);
            conductorMaterial.SetFloat("_Smoothness", 0.6f);
        }
        
        if (groundWireMaterial == null)
        {
            groundWireMaterial = new Material(Shader.Find("Standard"));
            groundWireMaterial.name = "GroundWireMaterial";
            groundWireMaterial.color = groundWireColor;
            groundWireMaterial.SetFloat("_Metallic", 0.9f);
            groundWireMaterial.SetFloat("_Smoothness", 0.4f);
        }
    }
    
    /// <summary>
    /// 修改后的电塔放置方法，返回创建的电塔列表
    /// </summary>
    List<GameObject> PlaceTowersFromSimplifiedInput()
    {
        List<SimpleTowerData> towers = LoadSimpleTowerData();
        List<GameObject> createdTowers = new List<GameObject>();
        
        foreach (var towerData in towers)
        {
            GameObject tower = CreateTowerAtPosition(towerData);
            if (tower != null)
            {
                createdTowers.Add(tower);
            }
        }
        
        return createdTowers;
    }
    
    /// <summary>
    /// 修改后的电塔创建方法，返回创建的电塔对象
    /// </summary>
    public GameObject CreateTowerAtPosition(SimpleTowerData towerData)
    {
        if (towerPrefab == null) return null;
        
        Vector3 position = towerData.position;
        
        // 地形适配：调整电塔基座高度
        if (adaptToTerrain && terrainManager != null)
        {
            float terrainHeight = terrainManager.GetTerrainHeight(position);
            position.y = terrainHeight + towerHeightOffset;
        }
        
        GameObject tower = Instantiate(towerPrefab, position, Quaternion.identity);
        tower.name = $"Tower_{position.x:F1}_{position.z:F1}";
        
        // 不再设置标签，直接通过名称识别
        
        // 根据电塔高度进行缩放
        float scaleRatio = towerData.height / baseTowerHeight;
        tower.transform.localScale = Vector3.one * scaleRatio * towerScale;
        
        // 调整电塔位置，让底部贴在地面上
        AdjustTowerGroundPosition(tower, towerData);
        
        if (powerlineParent != null)
        {
            tower.transform.SetParent(powerlineParent.transform);
        }
        
        // 确保电塔有引脚系统组件
        TowerPinpointSystem towerPinSystem = tower.GetComponent<TowerPinpointSystem>();
        if (towerPinSystem == null && pinpointSystem != null)
        {
            towerPinSystem = tower.AddComponent<TowerPinpointSystem>();
            // 复制引脚系统的配置
            towerPinSystem.enablePrecisePinConnection = pinpointSystem.enablePrecisePinConnection;
            towerPinSystem.debugUpperArmHeight = pinpointSystem.debugUpperArmHeight;
            towerPinSystem.debugLowerArmHeight = pinpointSystem.debugLowerArmHeight;
            towerPinSystem.debugArmWidth = pinpointSystem.debugArmWidth;
            towerPinSystem.showPinMarkers = pinpointSystem.showPinMarkers;
            towerPinSystem.pinMarkerSize = pinpointSystem.pinMarkerSize;
            towerPinSystem.pinMarkerColor = pinpointSystem.pinMarkerColor;
        }
        
        return tower;
    }
    
    /// <summary>
    /// 调整电塔位置，让底部贴在地面上
    /// </summary>
    void AdjustTowerGroundPosition(GameObject tower, SimpleTowerData towerData)
    {
        // 获取电塔的实际包围盒
        Renderer towerRenderer = tower.GetComponentInChildren<Renderer>();
        if (towerRenderer == null)
        {
            Debug.LogWarning($"电塔 {tower.name} 没有找到 Renderer 组件，无法调整底部位置");
            return;
        }
        
        // 强制更新包围盒
        towerRenderer.bounds.Encapsulate(towerRenderer.bounds);
        
        // 获取电塔底部的世界坐标Y值
        float towerBottomY = towerRenderer.bounds.min.y;
        
        // 计算目标地面高度
        float targetGroundY = 0f;
        if (adaptToTerrain && terrainManager != null)
        {
            targetGroundY = terrainManager.GetTerrainHeight(tower.transform.position) + towerHeightOffset;
        }
        else
        {
            targetGroundY = towerData.position.y + towerHeightOffset;
        }
        
        // 计算需要向上偏移的距离
        float offsetY = targetGroundY - towerBottomY;
        
        // 应用偏移
        Vector3 newPosition = tower.transform.position;
        newPosition.y += offsetY;
                tower.transform.position = newPosition;
    }
    
    /// <summary>
    /// 修改后的导线生成方法：支持按group分组连线
    /// </summary>
    void GenerateWiresFromTowers(List<GameObject> physicalTowers)
    {
        if (!usePrecisePinConnection || pinpointSystem == null)
        {
            if (pinpointSystem == null)
            {
                pinpointSystem = FindObjectOfType<TowerPinpointSystem>();
            }
            
            if (pinpointSystem == null) return;
        }
        
        powerlines.Clear();
        
        // 检查是否使用B.csv格式（有group信息）
        bool useGroupConnection = (csvFormat == CSVFormat.B);
        
        if (useGroupConnection)
        {
            GenerateWiresByGroup(physicalTowers);
        }
        else
        {
            GenerateWiresSequentially(physicalTowers);
        }
    }
    
    /// <summary>
    /// 按group分组生成电力线
    /// </summary>
    void GenerateWiresByGroup(List<GameObject> physicalTowers)
    {
        Debug.Log("[SceneInitializer] 使用group分组模式生成电力线");
        
        // 获取电塔数据以获取group信息
        List<SimpleTowerData> towerData = LoadSimpleTowerData();
        
        // 按group分组电塔
        Dictionary<int, List<GameObject>> groupTowers = new Dictionary<int, List<GameObject>>();
        Dictionary<int, List<SimpleTowerData>> groupTowerData = new Dictionary<int, List<SimpleTowerData>>();
        
        for (int i = 0; i < physicalTowers.Count && i < towerData.Count; i++)
        {
            int groupId = towerData[i].groupId;
            
            if (!groupTowers.ContainsKey(groupId))
            {
                groupTowers[groupId] = new List<GameObject>();
                groupTowerData[groupId] = new List<SimpleTowerData>();
            }
            
            groupTowers[groupId].Add(physicalTowers[i]);
            groupTowerData[groupId].Add(towerData[i]);
        }
        
        int globalWireIndex = 0;
        
        // 为每个group内的电塔生成电力线
        foreach (var group in groupTowers)
        {
            int groupId = group.Key;
            List<GameObject> groupTowerList = group.Value;
            List<SimpleTowerData> groupDataList = groupTowerData[groupId];
            
            Debug.Log($"[SceneInitializer] 处理Group {groupId}，包含 {groupTowerList.Count} 座电塔");
            
            // 按order排序
            var sortedTowers = groupTowerList.Select((tower, index) => new { tower, data = groupDataList[index] })
                                            .OrderBy(x => x.data.order)
                                            .Select(x => x.tower)
                                            .ToList();
            
            // 为group内相邻电塔生成电力线
            for (int towerIndex = 0; towerIndex < sortedTowers.Count - 1; towerIndex++)
            {
                GenerateWiresBetweenTowers(sortedTowers[towerIndex], sortedTowers[towerIndex + 1], globalWireIndex);
                globalWireIndex += 8; // 每个塔段8根导线
            }
        }
        
        Debug.Log($"[SceneInitializer] 按group分组生成了 {powerlines.Count} 条电力线段");
    }
    
    /// <summary>
    /// 顺序生成电力线（原有逻辑）
    /// </summary>
    void GenerateWiresSequentially(List<GameObject> physicalTowers)
    {
        Debug.Log("[SceneInitializer] 使用顺序模式生成电力线");
        
        // 从实际电塔获取引脚位置
        List<List<Vector3>> allPinPositions = new List<List<Vector3>>();
        
        foreach (GameObject tower in physicalTowers)
        {
            List<Vector3> pinPositions = GetPinPositions(tower);
            allPinPositions.Add(pinPositions);
        }
        
        // 为每两个相邻电塔之间的每根导线创建独立的电力线段
        for (int towerIndex = 0; towerIndex < physicalTowers.Count - 1; towerIndex++)
        {
            GenerateWiresBetweenTowers(physicalTowers[towerIndex], physicalTowers[towerIndex + 1], towerIndex * 8);
        }
        
        Debug.Log($"[SceneInitializer] 顺序生成了 {powerlines.Count} 条电力线段，覆盖 {physicalTowers.Count} 个电塔");
    }
    
    /// <summary>
    /// 在两个电塔之间生成电力线
    /// </summary>
    void GenerateWiresBetweenTowers(GameObject tower1, GameObject tower2, int baseWireIndex)
    {
        List<Vector3> pinPositions1 = GetPinPositions(tower1);
        List<Vector3> pinPositions2 = GetPinPositions(tower2);
        
        // 生成8根导线（4根地线 + 4根主导线）
        for (int pinIndex = 0; pinIndex < 8; pinIndex++)
        {
            PowerlineInfo wire = new PowerlineInfo();
            
            // 设置导线信息
            if (pinIndex < 4)
            {
                wire.wireType = "GroundWire";
                wire.wireIndex = pinIndex;
            }
            else
            {
                wire.wireType = "Conductor";
                wire.wireIndex = pinIndex - 4;
            }
            
            // 设置唯一索引
            wire.index = baseWireIndex + pinIndex;
            
            // 获取当前塔和下一塔的引脚位置
            if (pinIndex < pinPositions1.Count && pinIndex < pinPositions2.Count)
            {
                Vector3 startPin = pinPositions1[pinIndex];
                Vector3 endPin = pinPositions2[pinIndex];
                
                // 添加起点和终点
                wire.points.Add(startPin);
                wire.points.Add(endPin);
                
                wire.start = startPin;
                wire.end = endPin;
                wire.length = Vector3.Distance(startPin, endPin);
                
                // 设置杆塔位置信息
                wire.towerPositions = new Vector3[2];
                wire.towerPositions[0] = tower1.transform.position;
                wire.towerPositions[1] = tower2.transform.position;
                
                powerlines.Add(wire);
            }
        }
    }
    
    /// <summary>
    /// 从实际电塔获取引脚位置（世界坐标）
    /// </summary>
    List<Vector3> GetPinPositions(GameObject tower)
    {
        List<Vector3> worldPositions = new List<Vector3>();
        
        TowerPinpointSystem pinSystem = tower.GetComponent<TowerPinpointSystem>();
        if (pinSystem == null) return worldPositions;
        
        var pins = pinSystem.GetTowerPins(tower);
        foreach (var pin in pins)
        {
            // 转换为世界坐标（电塔已经缩放，所以引脚位置自动包含缩放）
            Vector3 worldPos = tower.transform.TransformPoint(pin.localPosition);
            worldPositions.Add(worldPos);
        }
        
        return worldPositions;
    }
    
    void DrawAllPowerlines()
    {
        foreach (var powerline in powerlines)
        {
            DrawPowerline(powerline);
        }
    }
    
    void DrawPowerline(PowerlineInfo powerline)
    {
        if (powerline.lineObj != null)
        {
            DestroyImmediate(powerline.lineObj);
        }
        
        // 计算塔段索引和导线类型
        int towerSegmentIndex = powerline.index / 8;
        int wireIndexInSegment = powerline.index % 8;
        string wireTypeName = powerline.wireType == "GroundWire" ? "G" : "C";
        
        GameObject lineObj = new GameObject($"Powerline_Segment{towerSegmentIndex}_{wireTypeName}{wireIndexInSegment}");
        if (powerlineParent != null)
        {
            lineObj.transform.SetParent(powerlineParent.transform);
        }
        
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        
        Material material = powerline.wireType == "GroundWire" ? groundWireMaterial : conductorMaterial;
        lr.material = material;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        
        List<Vector3> pointsToUse = powerline.smoothPoints.Count > 0 ? powerline.smoothPoints : powerline.points;
        lr.positionCount = pointsToUse.Count;
        lr.SetPositions(pointsToUse.ToArray());
        
        // 添加高级交互组件
        PowerlineInteraction interaction = lineObj.AddComponent<PowerlineInteraction>();
        interaction.SetPowerlineInfo(powerline);
        interaction.enableInteraction = true;
        interaction.enableHighlight = true;
        interaction.enableClickInfo = true;
        interaction.enableHoverEffect = true;
        
        // 设置颜色
        if (powerline.wireType == "GroundWire")
        {
            interaction.normalColor = groundWireColor;
        }
        else
        {
            interaction.normalColor = conductorColor;
        }
        
                powerline.lineObj = lineObj;
    }
    
    float CalcLineLength(List<Vector3> pts)
    {
        float length = 0;
        for (int i = 1; i < pts.Count; i++)
        {
            length += Vector3.Distance(pts[i-1], pts[i]);
        }
        return length;
    }
    
    /// <summary>
    /// 修改后的地形适配方法：只计算垂度，不再修改端点高度
    /// </summary>
    void ComputeSagForAllPowerlines()
    {
        foreach (var powerline in powerlines)
        {
            if (enablePhysicalSag)
            {
                powerline.smoothPoints = CreateSmoothSagLine(powerline.points);
            }
            else
            {
                powerline.smoothPoints = new List<Vector3>(powerline.points);
            }
        }
    }
    
    List<Vector3> CreateSmoothSagLine(List<Vector3> points)
    {
        // 现在每条电力线只有两个点（起点和终点），直接计算垂度
        if (points.Count == 2)
        {
            return CreateSagLine(points[0], points[1], segmentsPerSpan);
        }
        
        // 兼容旧格式（多段点）
        List<Vector3> sagPoints = new List<Vector3>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            List<Vector3> segmentPoints = CreateSagLine(points[i], points[i + 1], segmentsPerSpan);
            sagPoints.AddRange(segmentPoints);
        }
        return sagPoints;
    }
    
    List<Vector3> CreateSagLine(Vector3 start, Vector3 end, int segments)
    {
        List<Vector3> sagLine = new List<Vector3>();
        float distance = Vector3.Distance(start, end);
        
        // 根据距离动态调整下垂程度，长距离时减少下垂
        float dynamicSagFactor = sagFactor;
        if (distance > 100f)
        {
            // 长距离时减少下垂程度，避免垂到地面
            dynamicSagFactor = sagFactor * Mathf.Lerp(1f, 0.3f, (distance - 100f) / 200f);
            dynamicSagFactor = Mathf.Max(dynamicSagFactor, 0.1f); // 最小下垂程度
        }
        
        float maxSag = distance * dynamicSagFactor * 0.1f;
        
        // 计算地面高度（取起点和终点的最低高度作为参考）
        float groundHeight = Mathf.Min(start.y, end.y) - powerlineHeightOffset;
        
        // 计算电力线的最低点高度
        float lowestPointHeight = Mathf.Min(start.y, end.y) - maxSag;
        
        // 如果最低点会低于地面，调整下垂程度
        if (lowestPointHeight < groundHeight)
        {
            float allowedSag = Mathf.Min(start.y, end.y) - groundHeight;
            maxSag = Mathf.Min(maxSag, allowedSag * 0.8f); // 留一些安全距离
        }
        
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 point = Vector3.Lerp(start, end, t);
            
            float sagAmount = maxSag * Mathf.Sin(t * Mathf.PI);
            point.y -= sagAmount;
            
            // 确保点不会低于地面
            point.y = Mathf.Max(point.y, groundHeight);
            
            sagLine.Add(point);
        }
        
        return sagLine;
    }
    
    public List<SimpleTowerData> LoadSimpleTowerData()
    {
        List<SimpleTowerData> towers = new List<SimpleTowerData>();
        
        // 详细的调试信息
        Debug.Log($"[SceneInitializer] 🔍 尝试加载CSV文件: '{csvFileName}'");
        Debug.Log($"[SceneInitializer] 🔍 当前CSV格式: {csvFormat}");
        
        // 尝试多种方式读取CSV文件内容
        string csvContent = null;
        
        // 方法1：通过Resources.Load读取（推荐用于打包后）
        Debug.Log($"[SceneInitializer] 🔄 方法1: 尝试通过Resources.Load加载: {csvFileName}");
        TextAsset data = Resources.Load<TextAsset>(csvFileName);
        if (data != null)
        {
            csvContent = data.text;
            Debug.Log($"[SceneInitializer] ✅ 通过Resources.Load成功加载CSV文件: {csvFileName}");
        }
        else
        {
            Debug.LogWarning($"[SceneInitializer] ⚠️ Resources.Load失败，尝试其他方法");
        }
        
        // 方法2：直接文件系统读取（编辑器模式）
        if (string.IsNullOrEmpty(csvContent))
        {
            string resourcesPath = System.IO.Path.Combine(Application.dataPath, "Resources", csvFileName + ".csv");
            Debug.Log($"[SceneInitializer] 🔄 方法2: 尝试直接读取文件: {resourcesPath}");
            
            if (System.IO.File.Exists(resourcesPath))
            {
                try
                {
                    csvContent = System.IO.File.ReadAllText(resourcesPath);
                    Debug.Log($"[SceneInitializer] ✅ 成功直接读取CSV文件内容，长度: {csvContent.Length} 字符");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SceneInitializer] ❌ 直接读取CSV文件失败: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[SceneInitializer] ⚠️ 文件不存在: {resourcesPath}");
            }
        }
        
        // 方法3：检查StreamingAssets目录（打包后）
        if (string.IsNullOrEmpty(csvContent))
        {
            string streamingAssetsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Resources", csvFileName + ".csv");
            Debug.Log($"[SceneInitializer] 🔄 方法3: 尝试StreamingAssets: {streamingAssetsPath}");
            
            if (System.IO.File.Exists(streamingAssetsPath))
            {
                try
                {
                    csvContent = System.IO.File.ReadAllText(streamingAssetsPath);
                    Debug.Log($"[SceneInitializer] ✅ 通过StreamingAssets成功读取CSV文件");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SceneInitializer] ❌ StreamingAssets读取失败: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[SceneInitializer] ⚠️ StreamingAssets文件不存在: {streamingAssetsPath}");
            }
        }
        
        // 方法4：检查应用程序数据目录
        if (string.IsNullOrEmpty(csvContent))
        {
            string appDataPath = System.IO.Path.Combine(Application.persistentDataPath, "Resources", csvFileName + ".csv");
            Debug.Log($"[SceneInitializer] 🔄 方法4: 尝试PersistentDataPath: {appDataPath}");
            
            if (System.IO.File.Exists(appDataPath))
            {
                try
                {
                    csvContent = System.IO.File.ReadAllText(appDataPath);
                    Debug.Log($"[SceneInitializer] ✅ 通过PersistentDataPath成功读取CSV文件");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SceneInitializer] ❌ PersistentDataPath读取失败: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[SceneInitializer] ⚠️ PersistentDataPath文件不存在: {appDataPath}");
            }
        }
        
        // 如果仍然没有内容，尝试列出可用的文件
        if (string.IsNullOrEmpty(csvContent))
        {
            Debug.LogError($"[SceneInitializer] ❌ 无法获取CSV文件内容，已尝试所有方法");
            
            // 列出所有可能的CSV文件
            try
            {
                // 列出Resources.Load可用的文件
                TextAsset[] allTextAssets = Resources.LoadAll<TextAsset>("");
                var csvAssets = allTextAssets.Where(asset => asset.name.EndsWith(".csv") || 
                                                           asset.name.Contains("tower_centers") || 
                                                           asset.name == "A" || asset.name == "B" || 
                                                           asset.name == "simple_towers").ToArray();
                
                if (csvAssets.Length > 0)
                {
                    Debug.LogError($"[SceneInitializer] 📁 Resources.Load可用的CSV文件: {string.Join(", ", csvAssets.Select(a => a.name).ToArray())}");
                }
                else
                {
                    Debug.LogError($"[SceneInitializer] 📁 Resources.Load中没有找到CSV文件");
                }
                
                // 列出文件系统中的CSV文件
                string resourcesDir = System.IO.Path.Combine(Application.dataPath, "Resources");
                if (System.IO.Directory.Exists(resourcesDir))
                {
                    string[] csvFiles = System.IO.Directory.GetFiles(resourcesDir, "*.csv");
                    Debug.LogError($"[SceneInitializer] 📁 文件系统中的CSV文件: {string.Join(", ", csvFiles.Select(f => System.IO.Path.GetFileName(f)).ToArray())}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneInitializer] 📁 列出文件失败: {ex.Message}");
            }
            
            return towers;
        }

        var lines = csvContent.Split('\n');

        if (csvFormat == CSVFormat.TowerCenters)
        {
            // 只处理tower_centers.csv
            List<(float x, float z, float height)> rawTowerCenters = new List<(float, float, float)>();
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.ToLower().Contains("x") && line.ToLower().Contains("z")) continue; // 跳过标题行

                var tokens = line.Split(',');
                if (tokens.Length == 3 &&
                    float.TryParse(tokens[0], out var x1) &&
                    float.TryParse(tokens[1], out var z1) &&
                    float.TryParse(tokens[2], out var height1))
                {
                    // xy缩放比例
                    float xMeter = x1 * 10f;
                    float zMeter = z1 * 10f;
                    rawTowerCenters.Add((xMeter, zMeter, height1));
                    minX = Mathf.Min(minX, xMeter);
                    maxX = Mathf.Max(maxX, xMeter);
                    minZ = Mathf.Min(minZ, zMeter);
                    maxZ = Mathf.Max(maxZ, zMeter);
                }
            }
            float centerX = (minX + maxX) / 2f;
            float centerZ = (minZ + maxZ) / 2f;
            foreach (var (x, z, height) in rawTowerCenters)
            {
                towers.Add(new SimpleTowerData(new Vector3(x - centerX, 0f, z - centerZ), height));
            }
            Debug.Log($"tower_centers.csv已自动缩放(千米转米)并居中，中心点({centerX:F2}, {centerZ:F2})");
            Debug.Log($"成功加载 {towers.Count} 座电塔数据，使用格式: {csvFormat}");
            return towers;
        }
        else if (csvFormat == CSVFormat.B) // 新增B.csv格式
        {
            // 像tower_centers格式一样，先收集所有数据，然后进行缩放和居中
            List<(float x, float y, float z, int groupId, int order)> rawBData = new List<(float, float, float, int, int)>();
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.ToLower().Contains("group_id") && line.ToLower().Contains("order") && line.ToLower().Contains("x") && line.ToLower().Contains("y") && line.ToLower().Contains("z") && line.ToLower().Contains("line_count")) continue; // 跳过标题行

                var tokens = line.Split(',');
                if (tokens.Length >= 6 &&
                    int.TryParse(tokens[0], out var groupId) &&
                    int.TryParse(tokens[1], out var order) &&
                    float.TryParse(tokens[2], out var x) &&
                    float.TryParse(tokens[3], out var y) &&
                    float.TryParse(tokens[4], out var z) &&
                    float.TryParse(tokens[5], out var lineCount))
                {
                    // B.csv格式：group_id,order,x,y,z,line_count
                    // 像tower_centers一样进行缩放（千米转米）
                    float xMeter = x * 10f;
                    float yMeter = y * 10f;
                    rawBData.Add((xMeter, yMeter, z, groupId, order));
                    minX = Mathf.Min(minX, xMeter);
                    maxX = Mathf.Max(maxX, xMeter);
                    minY = Mathf.Min(minY, yMeter);
                    maxY = Mathf.Max(maxY, yMeter);
                }
                else
                {
                    Debug.LogWarning($"B.csv数据格式错误，已跳过: {line}");
                }
            }
            
            // 计算中心点并居中
            float centerX = (minX + maxX) / 2f;
            float centerY = (minY + maxY) / 2f;
            
            foreach (var (x, y, z, groupId, order) in rawBData)
            {
                float height = z > 0 ? z : baseTowerHeight;
                Vector3 position = new Vector3(x - centerX, 0f, y - centerY);
                towers.Add(new SimpleTowerData(position, height, groupId, order));
                
                // 调试日志：显示前几个电塔的坐标转换
                if (towers.Count <= 3)
                {
                    Debug.Log($"[B.csv] Group {groupId}, Order {order}: 原始坐标({x/10f},{y/10f},{z}) -> 缩放坐标({x},{y},{z}) -> Unity坐标({position.x},{position.y},{position.z}), 高度={height}");
                }
            }
            
            Debug.Log($"B.csv已自动缩放(千米转米)并居中，中心点({centerX:F2}, {centerY:F2})");
            Debug.Log($"成功加载 {towers.Count} 座电塔数据，使用B.csv格式（支持group分组连线）");
            return towers;
        }
        else // SimpleTowers
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.ToLower().Contains("x") && line.ToLower().Contains("z")) continue; // 跳过标题行

                var tokens = line.Split(',');
                bool parsedSuccessfully = false;
                if (tokens.Length >= 4 &&
                    float.TryParse(tokens[0], out var x2) &&
                    float.TryParse(tokens[1], out var y2) &&
                    float.TryParse(tokens[2], out var z2) &&
                    float.TryParse(tokens[3], out var height2))
                {
                    towers.Add(new SimpleTowerData(new Vector3(x2, y2, z2), height2));
                    parsedSuccessfully = true;
                }
                else if (tokens.Length == 3 &&
                    float.TryParse(tokens[0], out var x3) &&
                    float.TryParse(tokens[1], out var y3) &&
                    float.TryParse(tokens[2], out var z3))
                {
                    towers.Add(new SimpleTowerData(new Vector3(x3, y3, z3), baseTowerHeight));
                    parsedSuccessfully = true;
                }
                if (!parsedSuccessfully)
                {
                    Debug.LogWarning($"数据格式错误，已跳过: {line}");
                }
            }
            Debug.Log($"成功加载 {towers.Count} 座电塔数据，使用格式: {csvFormat}");
            return towers;
        }
    }
    
    [ContextMenu("重新生成所有")]
    public void RegenerateAll()
    {
        ClearAllWires();
        Start();
    }
    
    [ContextMenu("切换到 tower_centers.csv 格式")]
    public void SwitchToTowerCentersFormat()
    {
        csvFileName = "tower_centers";
        csvFormat = CSVFormat.TowerCenters;
        Debug.Log("已切换到 tower_centers.csv 格式");
    }
    
    [ContextMenu("切换到 simple_towers.csv 格式")]
    public void SwitchToSimpleTowersFormat()
    {
        csvFileName = "simple_towers";
        csvFormat = CSVFormat.SimpleTowers;
        Debug.Log("已切换到 simple_towers.csv 格式");
    }
    
    [ContextMenu("切换到 B.csv 格式")]
    public void SwitchToBFormat()
    {
        csvFileName = "B";
        csvFormat = CSVFormat.B;
        Debug.Log("已切换到 B.csv 格式（支持group分组连线）");
    }
    
    [ContextMenu("验证当前CSV文件")]
    public void ValidateCurrentCSVFile()
    {
        TextAsset data = Resources.Load<TextAsset>(csvFileName);
        if (data == null)
        {
            Debug.LogError($"❌ 无法找到CSV文件: {csvFileName}");
            return;
        }
        
        Debug.Log($"✅ 找到CSV文件: {csvFileName}");
        Debug.Log($"📊 当前格式: {csvFormat}");
        
        // 预览前几行数据
        string[] lines = data.text.Split('\n');
        int previewLines = Mathf.Min(5, lines.Length);
        
        Debug.Log("📋 文件预览:");
        for (int i = 0; i < previewLines; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                Debug.Log($"第{i+1}行: {lines[i].Trim()}");
            }
        }
        
        if (lines.Length > previewLines)
        {
            Debug.Log($"... 还有 {lines.Length - previewLines} 行数据");
        }
    }
    
    [ContextMenu("清理所有导线")]
    public void ClearAllWires()
    {
        foreach (var powerline in powerlines)
        {
            if (powerline.lineObj != null)
            {
                DestroyImmediate(powerline.lineObj);
            }
        }
        powerlines.Clear();
    }
    
    [ContextMenu("显示引脚标记")]
    public void ShowPinMarkers()
    {
        if (pinpointSystem == null) return;
        
        GameObject[] towers = FindObjectsOfType<GameObject>().Where(go => 
            go.name.Contains("Tower") || go.name.Contains("GoodTower")).ToArray();
        
        foreach (GameObject tower in towers)
        {
            pinpointSystem.AddPinMarkers(tower);
        }
        

    }
    
    /// <summary>
    /// 初始化点云系统
    /// </summary>
    void InitializePointCloudSystem()
    {
        // 查找点云管理器
        if (pointCloudManager == null)
        {
            pointCloudManager = FindObjectOfType<PowerlineSystem.PowerlinePointCloudManager>();
        }
        
        if (pointCloudManager != null)
        {
            // 设置场景初始化器引用
            pointCloudManager.sceneInitializer = this;
            
            // 为主相机添加点大小启用器
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                PowerlineSystem.PowerlinePointSizeEnabler.SetupPointCloudCamera(mainCamera);
            }
            
                    Debug.Log("点云系统已初始化并与电力线系统集成");
    }
    else
    {
        Debug.Log("未找到点云管理器，跳过点云系统初始化");
    }
}

#region 树木管理

/// <summary>
/// 从CSV文件创建树木
/// </summary>
private void CreateTreesFromCsv()
{
    Debug.Log("[SceneInitializer] 开始执行树木放置...");
    
    List<SimpleTreeData> trees = LoadSimpleTreeData();
    List<GameObject> createdTrees = new List<GameObject>();
    
    if (trees.Count == 0) 
    {
        Debug.LogWarning("[SceneInitializer] 没有树木数据可供放置");
        return;
    }
    
    Debug.Log($"[SceneInitializer] 准备放置 {trees.Count} 棵树");
    
    // 清理已放置的树木
    ClearPlacedTrees();
    
    // 如果没有指定树木预制件，尝试从Resources加载
    if (treePrefab == null)
    {
        Debug.Log("[SceneInitializer] 树木预制件未指定，尝试从Resources加载...");
        treePrefab = Resources.Load<GameObject>("Prefabs/Tree");
        if (treePrefab == null)
        {
            Debug.LogError("[SceneInitializer] 无法找到Tree预制件，跳过树木放置");
            Debug.LogError("[SceneInitializer] 请确保Tree.prefab位于Resources/Prefabs/文件夹中");
            return;
        }
        Debug.Log("[SceneInitializer] 成功加载Tree预制件");
    }
    else
    {
        Debug.Log("[SceneInitializer] 使用已指定的树木预制件");
    }
    
    int successCount = 0;
    int failCount = 0;
    
    foreach (var treeData in trees)
    {
        GameObject tree = CreateTreeAtPosition(treeData);
        if (tree != null)
        {
            createdTrees.Add(tree);
            placedTrees.Add(tree);
            successCount++;
            
            // 每10棵树输出一次进度
            if (successCount % 10 == 0)
            {
                Debug.Log($"[SceneInitializer] 已成功放置 {successCount} 棵树");
            }
        }
        else
        {
            failCount++;
            Debug.LogWarning($"[SceneInitializer] 第 {treeData.treeId} 棵树创建失败");
        }
    }
    
    Debug.Log($"[SceneInitializer] 树木放置完成！成功: {successCount}, 失败: {failCount}");
    Debug.Log($"[SceneInitializer] 总共放置了 {placedTrees.Count} 棵树");
    
    // 通知树木危险监测系统更新
    NotifyTreeDangerMonitorUpdate();
}

/// <summary>
/// 加载简化树木数据
/// </summary>
private List<SimpleTreeData> LoadSimpleTreeData()
{
    List<SimpleTreeData> trees = new List<SimpleTreeData>();
    
    if (!enableTreePlacement) 
    {
        Debug.LogWarning("[SceneInitializer] 树木放置功能未启用！");
        return trees;
    }
    
    Debug.Log($"[SceneInitializer] 开始加载树木数据，CSV文件名: {treeCsvFileName}");
    
    // 首先尝试从CSV文件加载树木数据
    List<SimpleTreeData> csvTrees = LoadTreesFromCsvFile();
    
    // 如果CSV文件中有树木数据，使用它
    if (csvTrees.Count > 0)
    {
        trees.AddRange(csvTrees);
        Debug.Log($"[SceneInitializer] 从CSV文件加载了 {csvTrees.Count} 棵树");
    }
    else
    {
        // 如果CSV文件中没有树木数据，基于电塔位置自动生成树木
        Debug.Log("[SceneInitializer] CSV文件中没有树木数据，将基于电塔位置自动生成树木");
        trees = GenerateTreesNearTowers();
    }
    
    Debug.Log($"[SceneInitializer] 成功加载 {trees.Count} 棵简化树木数据");
    return trees;
}

/// <summary>
/// 从CSV文件加载树木数据
/// </summary>
private List<SimpleTreeData> LoadTreesFromCsvFile()
{
    List<SimpleTreeData> trees = new List<SimpleTreeData>();
    
    // 加载CSV文件
    TextAsset csvFile = Resources.Load<TextAsset>(treeCsvFileName);
    if (csvFile == null)
    {
        Debug.LogWarning($"[SceneInitializer] 无法找到树木CSV文件 {treeCsvFileName}");
        return trees;
    }
    
    Debug.Log($"[SceneInitializer] 成功加载CSV文件，文件大小: {csvFile.text.Length} 字符");
    
    // 解析CSV数据
    string[] lines = csvFile.text.Split('\n');
    Debug.Log($"[SceneInitializer] CSV文件包含 {lines.Length} 行数据");
    
    // 先收集所有数据，然后进行缩放和居中（类似B.csv的处理方式）
    List<(float x, float y, float z, int treeId, int groupId, int towerId, string treeType)> rawTreeData = new List<(float, float, float, int, int, int, string)>();
    float minX = float.MaxValue, maxX = float.MinValue;
    float minY = float.MaxValue, maxY = float.MinValue;
    
    // 跳过标题行
    for (int i = 1; i < lines.Length; i++)
    {
        string line = lines[i].Trim();
        if (string.IsNullOrEmpty(line)) continue;
        
        string[] values = line.Split(',');
        if (values.Length >= 6)
        {
            if (int.TryParse(values[0], out int treeId) &&
                int.TryParse(values[1], out int groupId) &&
                int.TryParse(values[2], out int towerId) &&
                float.TryParse(values[3], out float x) &&
                float.TryParse(values[4], out float y) &&
                float.TryParse(values[5], out float z))
            {
                // 使用和B.csv相同的缩放比例：千米转米（乘以10）
                float xMeter = x * 10f;
                float yMeter = y * 10f;
                float zMeter = z;
                
                string treeType = values.Length > 6 ? values[6] : "Tree";
                
                // 收集原始数据用于居中计算
                rawTreeData.Add((xMeter, yMeter, zMeter, treeId, groupId, towerId, treeType));
                minX = Mathf.Min(minX, xMeter);
                maxX = Mathf.Max(maxX, xMeter);
                minY = Mathf.Min(minY, yMeter);
                maxY = Mathf.Max(maxY, yMeter);
            }
            else
            {
                Debug.LogWarning($"[SceneInitializer] 第 {i} 行数据解析失败: {line}");
            }
        }
        else
        {
            Debug.LogWarning($"[SceneInitializer] 第 {i} 行数据列数不足: {line}");
        }
    }
    
    // 计算中心点并居中（类似B.csv的处理方式）
    float centerX = (minX + maxX) / 2f;
    float centerY = (minY + maxY) / 2f;
    
    Debug.Log($"[SceneInitializer] 树木数据已缩放(千米转米)并居中，中心点({centerX:F2}, {centerY:F2})");
    
    // 创建最终的树木数据
    foreach (var (x, y, z, treeId, groupId, towerId, treeType) in rawTreeData)
    {
        // 坐标转换：X,Y→Unity的X,Z，Z→Y（高度），并居中
        Vector3 position = new Vector3(x - centerX, z, y - centerY);
        
        // 计算树木高度（基于目标高度范围）
        float treeHeight = UnityEngine.Random.Range(treeHeightRange.x, treeHeightRange.y);
        
        // 计算缩放比例（增加变化范围）
        float scale = UnityEngine.Random.Range(0.6f, 1.8f);
        
        SimpleTreeData treeData = new SimpleTreeData(treeId, position, treeHeight, groupId, towerId, treeType, scale);
        trees.Add(treeData);
        
        // 每10棵树输出一次调试信息
        if (trees.Count % 10 == 0)
        {
            Debug.Log($"[SceneInitializer] 已加载 {trees.Count} 棵树，最新: ID={treeId}, 原始位置=({x/10f},{y/10f},{z}) -> Unity位置=({position.x:F1},{position.y:F1},{position.z:F1}), 组={groupId}, 塔={towerId}");
        }
    }
    
    Debug.Log($"[SceneInitializer] 成功加载 {trees.Count} 棵树，使用和B.csv相同的缩放和居中处理");
    return trees;
}

/// <summary>
/// 基于电塔位置自动生成树木（在电力线路附近）
/// </summary>
private List<SimpleTreeData> GenerateTreesNearTowers()
{
    List<SimpleTreeData> trees = new List<SimpleTreeData>();
    
    // 获取电塔数据
    List<SimpleTowerData> towerData = LoadSimpleTowerData();
    if (towerData.Count == 0)
    {
        Debug.LogWarning("[SceneInitializer] 没有电塔数据，无法生成树木");
        return trees;
    }
    
    Debug.Log($"[SceneInitializer] 基于 {towerData.Count} 座电塔生成树木");
    
    // 获取实际场景中的电塔GameObject位置（更准确）
    GameObject[] actualTowers = FindObjectsOfType<GameObject>().Where(go => 
        go.name.Contains("Tower") || go.name.Contains("GoodTower")).ToArray();
    
    Debug.Log($"[SceneInitializer] 找到 {actualTowers.Length} 座实际电塔");
    
    int treeId = 1;
    
    // 为每个电塔生成多棵树
    foreach (var tower in towerData)
    {
        // 找到对应的实际电塔位置
        Vector3 actualTowerPosition = tower.position;
        
        // 尝试从实际电塔GameObject获取更准确的位置
        if (actualTowers.Length > 0)
        {
            // 找到最近的已放置电塔
            GameObject nearestTower = null;
            float minDistance = float.MaxValue;
            
            foreach (var actualTower in actualTowers)
            {
                float distance = Vector3.Distance(actualTower.transform.position, tower.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestTower = actualTower;
                }
            }
            
            if (nearestTower != null)
            {
                actualTowerPosition = nearestTower.transform.position;
                Debug.Log($"[SceneInitializer] 电塔 {tower.groupId}-{towerData.IndexOf(tower)} 使用实际位置: {actualTowerPosition}");
            }
        }
        
        // 每个电塔生成树木（使用配置参数）
        int treesPerTower = UnityEngine.Random.Range(treesPerTowerRange.x, treesPerTowerRange.y + 1);
        
        for (int i = 0; i < treesPerTower; i++)
        {
            // 在电塔周围随机位置生成树木（使用配置参数）
            float distanceFromTower = UnityEngine.Random.Range(minTreeDistanceFromTower, maxTreeDistanceFromTower);
            float angle = UnityEngine.Random.Range(0f, 360f); // 随机角度
            
            // 计算树木位置（相对于电塔）
            float offsetX = Mathf.Cos(angle * Mathf.Deg2Rad) * distanceFromTower;
            float offsetZ = Mathf.Sin(angle * Mathf.Deg2Rad) * distanceFromTower;
            
            Vector3 treePosition = actualTowerPosition + new Vector3(offsetX, 0, offsetZ);
            
                         // 计算树木高度和缩放
             float treeHeight = UnityEngine.Random.Range(treeHeightRange.x, treeHeightRange.y);
             float scale = UnityEngine.Random.Range(0.6f, 1.8f);
            
            // 创建树木数据
            SimpleTreeData treeData = new SimpleTreeData(
                treeId, 
                treePosition, 
                treeHeight, 
                tower.groupId, 
                towerData.IndexOf(tower), 
                "AutoTree", 
                scale
            );
            
            trees.Add(treeData);
            treeId++;
            
            // 每10棵树输出一次调试信息
            if (trees.Count % 10 == 0)
            {
                Debug.Log($"[SceneInitializer] 已生成 {trees.Count} 棵树，最新: ID={treeData.treeId}, 位置=({treePosition.x:F1},{treePosition.y:F1},{treePosition.z:F1}), 距离电塔={distanceFromTower:F1}m");
            }
        }
    }
    
    Debug.Log($"[SceneInitializer] 自动生成了 {trees.Count} 棵树，分布在 {towerData.Count} 座电塔周围");
    return trees;
}

/// <summary>
/// 在指定位置创建树木
/// </summary>
private GameObject CreateTreeAtPosition(SimpleTreeData treeData)
{
    if (treePrefab == null) 
    {
        Debug.LogError("[SceneInitializer] 树木预制件为空，无法创建树木");
        return null;
    }
    
    Vector3 position = treeData.position;
    Debug.Log($"[SceneInitializer] 创建树木 ID={treeData.treeId}, 原始位置=({position.x:F2}, {position.y:F2}, {position.z:F2})");
    
    // 地形适配：调整树木基座高度
    if (terrainManager != null)
    {
        float terrainHeight = terrainManager.GetTerrainHeight(position);
        position.y = Mathf.Max(position.y, terrainHeight);
        Debug.Log($"[SceneInitializer] 地形高度: {terrainHeight:F2}, 调整后Y坐标: {position.y:F2}");
    }
    else
    {
        Debug.LogWarning("[SceneInitializer] 地形管理器未找到，跳过地形适配");
    }
    
    // 添加随机偏移，避免树木完全重叠
    float randomOffsetX = UnityEngine.Random.Range(-2f, 2f);
    float randomOffsetZ = UnityEngine.Random.Range(-2f, 2f);
    position += new Vector3(randomOffsetX, 0, randomOffsetZ);
    Debug.Log($"[SceneInitializer] 随机偏移: ({randomOffsetX:F2}, 0, {randomOffsetZ:F2}), 最终位置: ({position.x:F2}, {position.y:F2}, {position.z:F2})");
    
    GameObject tree = Instantiate(treePrefab, position, Quaternion.identity);
    if (tree == null)
    {
        Debug.LogError("[SceneInitializer] 树木实例化失败");
        return null;
    }
    
    tree.name = $"Tree_{treeData.treeId}_Group{treeData.groupId}_Tower{treeData.towerId}";
    
    // 随机旋转
    float randomRotation = UnityEngine.Random.Range(0f, 360f);
    tree.transform.rotation = Quaternion.Euler(0, randomRotation, 0);
    
    // 根据树木高度进行缩放（参考电塔的缩放方式）
    float scaleRatio = treeData.height / 3f; // 假设标准树木高度为3米
    // 增加基础缩放，让树木更明显
    tree.transform.localScale = Vector3.one * scaleRatio * treeData.scale * treeBaseScale;
    
    Debug.Log($"[SceneInitializer] 树木 {tree.name} 创建成功，高度缩放: {scaleRatio:F2}, 基础缩放: {treeData.scale:F2}, 旋转: {randomRotation:F1}°");
    
    // 调整树木位置，让底部贴在地面上（参考电塔的AdjustTowerGroundPosition方法）
    AdjustTreeGroundPosition(tree, treeData);
    
    // 不再设置标签，直接通过名称识别
    
    // 自动缩放（如果需要）
    if (enableTreeAutoScaling)
    {
        ApplyTreeAutoScaling(tree);
        Debug.Log("[SceneInitializer] 已应用自动缩放");
    }
    
    // 设置父对象（如果有电塔父对象）
    if (powerlineParent != null)
    {
        tree.transform.SetParent(powerlineParent.transform);
        Debug.Log($"[SceneInitializer] 树木已设置父对象: {powerlineParent.name}");
    }
    else
    {
        Debug.LogWarning("[SceneInitializer] 未设置树木父对象");
    }
    
    return tree;
}

/// <summary>
/// 调整树木位置，让底部贴在地面上（参考电塔的AdjustTowerGroundPosition方法）
/// </summary>
void AdjustTreeGroundPosition(GameObject tree, SimpleTreeData treeData)
{
    // 获取树木的实际包围盒
    Renderer treeRenderer = tree.GetComponentInChildren<Renderer>();
    if (treeRenderer == null)
    {
        Debug.LogWarning($"树木 {tree.name} 没有找到 Renderer 组件，无法调整底部位置");
        return;
    }
    
    // 强制更新包围盒
    treeRenderer.bounds.Encapsulate(treeRenderer.bounds);
    
    // 获取树木底部的世界坐标Y值
    float treeBottomY = treeRenderer.bounds.min.y;
    
    // 计算目标地面高度
    float targetGroundY = 0f;
    if (adaptToTerrain && terrainManager != null)
    {
        targetGroundY = terrainManager.GetTerrainHeight(tree.transform.position);
    }
    else
    {
        targetGroundY = treeData.position.y;
    }
    
    // 计算需要向上偏移的距离
    float offsetY = targetGroundY - treeBottomY;
    
    // 应用偏移
    Vector3 newPosition = tree.transform.position;
    newPosition.y += offsetY;
    tree.transform.position = newPosition;
    
    Debug.Log($"[SceneInitializer] 树木 {tree.name} 地面适配: 底部Y={treeBottomY:F2}, 目标地面Y={targetGroundY:F2}, 偏移Y={offsetY:F2}");
}

/// <summary>
/// 应用树木自动缩放
/// </summary>
private void ApplyTreeAutoScaling(GameObject tree)
{
    if (tree == null) return;
    
    // 获取树木的边界
    Renderer renderer = tree.GetComponent<Renderer>();
    if (renderer != null)
    {
        Bounds bounds = renderer.bounds;
        float currentHeight = bounds.size.y;
        float targetHeight = UnityEngine.Random.Range(treeHeightRange.x, treeHeightRange.y);
        
        if (currentHeight > 0)
        {
            float scaleFactor = targetHeight / currentHeight;
            tree.transform.localScale *= scaleFactor;
            Debug.Log($"[SceneInitializer] 树木自动缩放: 当前高度={currentHeight:F2}, 目标高度={targetHeight:F2}, 缩放因子={scaleFactor:F2}");
        }
    }
}

/// <summary>
/// 清理已放置的树木
/// </summary>
private void ClearPlacedTrees()
{
    foreach (var tree in placedTrees)
    {
        if (tree != null)
        {
            DestroyImmediate(tree);
        }
    }
    placedTrees.Clear();
    Debug.Log("[SceneInitializer] 已清理所有已放置的树木");
}

/// <summary>
/// 手动构建树木（用于调试）
/// </summary>
[ContextMenu("手动构建树木")]
public void BuildTreesFromCsv()
{
    Debug.Log("[SceneInitializer] 手动触发树木构建...");
    
    if (!enableTreePlacement)
    {
        Debug.LogWarning("[SceneInitializer] 树木放置功能未启用，请先启用enableTreePlacement");
        return;
    }
    
    CreateTreesFromCsv();
    Debug.Log("[SceneInitializer] 手动构建完成");
}

/// <summary>
/// 检查树木系统状态（用于调试）
/// </summary>
[ContextMenu("检查树木系统状态")]
public void CheckTreeSystemStatus()
{
    Debug.Log("=== SceneInitializer 树木系统状态检查 ===");
    Debug.Log($"enableTreePlacement: {enableTreePlacement}");
    Debug.Log($"treePrefab: {(treePrefab != null ? treePrefab.name : "null")}");
    Debug.Log($"treeCsvFileName: {treeCsvFileName}");
    Debug.Log($"enableTreeAutoScaling: {enableTreeAutoScaling}");
    Debug.Log($"treeHeightRange: {treeHeightRange.x}-{treeHeightRange.y}");
    Debug.Log($"treesPerTowerRange: {treesPerTowerRange.x}-{treesPerTowerRange.y}");
    Debug.Log($"treeDistanceFromTower: {minTreeDistanceFromTower}-{maxTreeDistanceFromTower}m");
    Debug.Log($"treeBaseScale: {treeBaseScale}");
    Debug.Log($"terrainManager: {(terrainManager != null ? terrainManager.name : "null")}");
    Debug.Log($"powerlineParent: {(powerlineParent != null ? powerlineParent.name : "null")}");
    Debug.Log($"已放置树木数量: {placedTrees.Count}");
    
    TextAsset csvFile = Resources.Load<TextAsset>(treeCsvFileName);
    if (csvFile != null)
    {
        Debug.Log($"CSV文件存在，大小: {csvFile.text.Length} 字符");
        string[] lines = csvFile.text.Split('\n');
        Debug.Log($"CSV文件行数: {lines.Length}");
    }
    else
    {
        Debug.LogError($"CSV文件不存在: {treeCsvFileName}");
    }
    
    if (treePrefab == null)
    {
        GameObject loadedPrefab = Resources.Load<GameObject>("Prefabs/Tree");
        Debug.Log($"从Resources加载的预制件: {(loadedPrefab != null ? loadedPrefab.name : "null")}");
    }
    
    // 显示电塔和树木的位置信息
    Debug.Log("=== 位置信息 ===");
    GameObject[] towers = FindObjectsOfType<GameObject>().Where(go => 
        go.name.Contains("Tower") || go.name.Contains("GoodTower")).ToArray();
    
    Debug.Log($"找到 {towers.Length} 座电塔:");
    foreach (var tower in towers)
    {
        Debug.Log($"电塔: {tower.name}, 位置: {tower.transform.position}");
    }
    
    Debug.Log($"已放置 {placedTrees.Count} 棵树:");
    for (int i = 0; i < Mathf.Min(placedTrees.Count, 10); i++) // 只显示前10棵
    {
        var tree = placedTrees[i];
        if (tree != null)
        {
            Debug.Log($"树木 {i}: {tree.name}, 位置: {tree.transform.position}");
        }
    }
    
    if (placedTrees.Count > 10)
    {
        Debug.Log($"... 还有 {placedTrees.Count - 10} 棵树");
    }
    
    Debug.Log("=== 状态检查完成 ===");
}

/// <summary>
/// 重新生成树木（用于调试位置问题）
/// </summary>
[ContextMenu("重新生成树木")]
public void RegenerateTrees()
{
    Debug.Log("[SceneInitializer] 重新生成树木...");
    ClearPlacedTrees();
    CreateTreesFromCsv();
    Debug.Log("[SceneInitializer] 树木重新生成完成");
}

/// <summary>
/// 通知树木危险监测系统更新
/// </summary>
private void NotifyTreeDangerMonitorUpdate()
{
    var treeDangerMonitor = FindObjectOfType<TreeDangerMonitor>();
    if (treeDangerMonitor != null)
    {
        Debug.Log("[SceneInitializer] 通知TreeDangerMonitor更新树木列表");
        treeDangerMonitor.RefreshTreeList();
        treeDangerMonitor.ManualMonitoring();
    }
    else
    {
        Debug.LogWarning("[SceneInitializer] 未找到TreeDangerMonitor，无法通知更新");
    }
}



#endregion

}  