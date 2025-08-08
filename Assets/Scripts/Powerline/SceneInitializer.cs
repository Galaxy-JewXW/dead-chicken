using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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
    
    public List<PowerlineInfo> powerlines = new List<PowerlineInfo>();
    private Dictionary<Vector3, float> towerHeights = new Dictionary<Vector3, float>();

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
        
        // 尝试设置标签（如果标签存在的话）
        try
        {
            tower.tag = "Tower";
        }
        catch (UnityException)
        {
            Debug.LogWarning("Tower标签未定义，请在Unity的Tags & Layers中添加Tower标签");
        }
        
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
        
        GameObject[] towers = null;
        try
        {
            towers = GameObject.FindGameObjectsWithTag("Tower");
        }
        catch (UnityException)
        {
            Debug.LogWarning("Tower标签未定义，将通过名称查找电塔");
            towers = new GameObject[0];
        }
        
        if (towers.Length == 0)
        {
            towers = FindObjectsOfType<GameObject>().Where(go => 
                go.name.Contains("Tower") || go.name.Contains("GoodTower")).ToArray();
        }
        
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
    
} 